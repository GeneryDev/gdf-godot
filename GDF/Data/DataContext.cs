using System;
using System.Collections.Generic;
using System.Text;
using GDF.Util;
using Godot;
using Array = Godot.Collections.Array;

namespace GDF.Data;

public static class DataContext
{
    private static ref ParsedDataQuery ParseQuery(string query, DataQueryType queryType, ref ParsedDataQuery cached)
    {
        if (!cached.IsEmpty && cached.Query == query && cached.QueryType == queryType) return ref cached;

        var parsed = new ParsedDataQuery(query, queryType);
        if (!string.IsNullOrEmpty(query))
        {
            var currentIndex = 0;
            var anySeparatorCharacters = false;
            while (Step(query, currentIndex, out int interpStartIndex, out int interpEndIndex, out string interpolationStr))
            {
                parsed.SeparatorIndices.Add((currentIndex, interpStartIndex));
                if (!anySeparatorCharacters && !IsSubstringWhitespace(query, currentIndex, interpStartIndex))
                    anySeparatorCharacters = true;
                parsed.InterpolationEntries.Add(ParseInterpolationString(interpolationStr));
                currentIndex = interpEndIndex;
            }
            parsed.SeparatorIndices.Add((currentIndex, query.Length));
            if (!anySeparatorCharacters && !IsSubstringWhitespace(query, currentIndex, query.Length))
                anySeparatorCharacters = true;
            
            if (queryType == DataQueryType.Expression && (anySeparatorCharacters || parsed.InterpolationEntries.Count != 1))
            {
                AssembleExpression(ref parsed);
            }
        }
        cached = parsed;
        return ref cached;
    }

    private static void AssembleExpression(ref ParsedDataQuery parsed)
    {
        parsed.Expression = new Expression();
        parsed.ExpressionInputs = new Array();
        int varCount = parsed.InterpolationEntries.Count;
        parsed.ExpressionInputs.Resize(varCount);
        var inputNames = new string[varCount];
        var sb = new StringBuilder();
        for (var i = 0; i < parsed.SeparatorIndices.Count; i++)
        {
            (int partStart, int partEnd) = parsed.SeparatorIndices[i];
            sb.Append(parsed.Query, partStart, partEnd - partStart);
            if (i < varCount)
            {
                var varName = $"var{i}";
                inputNames[i] = varName;
                sb.Append(' ').Append(varName).Append(' ');
            }
        }

        var rawExpression = sb.ToString();
        if (parsed.Expression.Parse(rawExpression, inputNames) != Error.Ok)
        {
            GD.PrintErr($"Error parsing expression in data query: {rawExpression}\nSource query: {parsed.Query}");
            return;
        }
    }

    private static bool IsSubstringWhitespace(string str, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            if (!char.IsWhiteSpace(str[i])) return false;
        }

        return true;
    }
    
    private static bool Step(string str, int fromIndex, out int interpStartIndex, out int interpEndIndex, out string interpStr)
    {
        var escaped = false;
        interpStartIndex = interpEndIndex = -1;
        interpStr = null;
        var braceLevel = 0;
        var sb = new StringBuilder();
        for (int index = fromIndex; index < str.Length; index++)
        {
            char c = str[index];
            if (!escaped)
            {
                if (c == '{')
                {
                    if (braceLevel == 0)
                    {
                        sb.Clear();
                        interpStartIndex = index;
                    }
                    if(braceLevel > 0)
                        sb.Append(c);
                    braceLevel++;
                }
                else if (c == '}')
                {
                    braceLevel--;
                    if(braceLevel > 0)
                        sb.Append(c);
                    if (braceLevel == 0)
                    {
                        interpEndIndex = index + 1;
                        interpStr = sb.ToString();
                        return true;
                    }
                }
                else if (c == '\\')
                {
                    escaped = true;
                    continue;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if(braceLevel > 0)
                    sb.Append(c);
                escaped = false;
            }
        }

        return false;
    }

    // Expressions
    [Obsolete("Please use the overload with a ParsedDataQuery reference for caching")]
    public static Variant Evaluate(this IDataContext context, string query, IDataQueryOptions options = null)
    {
        ParsedDataQuery cached = default;
        return Evaluate(context, query, ref cached, options);
    }
    
    public static Variant Evaluate(this IDataContext context, string query, ref ParsedDataQuery cachedQuery,
        IDataQueryOptions options = null)
    {
        if (string.IsNullOrEmpty(query)) return default;
        ref var parsed = ref ParseQuery(query, DataQueryType.Expression, ref cachedQuery);
        if (parsed.Expression != null)
        {
            // Complex expression

            for (var i = 0; i < parsed.InterpolationEntries.Count; i++)
            {
                var varValue = GetVariable(parsed.InterpolationEntries[i], context, options);
                if (varValue.VariantType == Variant.Type.Nil && !(options?.SupportsNullOperands ?? false))
                {
                    return default;
                }
                parsed.ExpressionInputs[i] = varValue;
            }
            var output = parsed.Expression.Execute(parsed.ExpressionInputs);
            parsed.ExpressionInputs.Fill(default);
            if (parsed.Expression.HasExecuteFailed())
            {
                GD.PrintErr($"Error executing expression in data query: {parsed.Query}\n{parsed.Expression.GetErrorText()}");
                return default;
            }

            return output;
        }
        else
        {
            // Single variable
            
            return parsed.InterpolationEntries.Count > 0
                ? GetVariable(parsed.InterpolationEntries[^1], context, options)
                : default;
        }
    }

    private static Variant GetVariable(ParsedDataQueryInterpolation interpolation, IDataContext context,
        IDataQueryOptions options)
    {
        if (!interpolation.CalcExpression.IsEmpty)
            return Evaluate(context, interpolation.CalcExpression.Query, ref interpolation.CalcExpression, options);
        (string key, string input) = GetKeyAndInput(interpolation, ref context);
        Variant output = default;
        if (key == null) return output;

        foreach (var ctx in EnumerateContextHierarchy(context))
        {
            if (ctx.GetContextVariable(key, input, ref output, options))
                return output;
            if (ctx.UseStringsAsVariables)
            {
                string str = null;
                if (ctx.GetContextString(key, input, ref str, options))
                    return str;
            }
        }

        return default;
    }

    // Strings
    [Obsolete("Please use the overload with a ParsedDataQuery reference for caching")]
    public static string Format(this IDataContext context, string input, IDataQueryOptions options = null)
    {
        ParsedDataQuery cached = default;
        return Format(context, input, ref cached, options);
    }
    public static string Format(this IDataContext context, string input, ref ParsedDataQuery cachedQuery, IDataQueryOptions options = null)
    {
        if (string.IsNullOrEmpty(input)) return input;
        input = PreprocessBbcode(context, input, options);
        ref var parsed = ref ParseQuery(input, DataQueryType.String, ref cachedQuery);

        string firstPassResult = FormatOnce(context, input, ref parsed, options);

        StringName translationInput;
        if (parsed.LastFormatOutput == firstPassResult)
        {
            translationInput = parsed.LastFormatOutputStringName;
        }
        else
        {
            parsed.LastFormatOutput = firstPassResult;
            translationInput = parsed.LastFormatOutputStringName = parsed.LastFormatOutput;
        }
        
        // Translate here
        var translationResult = TranslationServer.Singleton.Translate(translationInput);
        if (translationResult == translationInput && !StringRequiresFormatting(firstPassResult, options))
        {
            // no change
            return firstPassResult;
        }


        parsed.OtherPasses ??= new();
        if(parsed.OtherPasses.Count < 1) parsed.OtherPasses.Add(default);

        var secondPassInput = (string)translationResult;
        secondPassInput = PreprocessBbcode(context, secondPassInput, options);
        
        var secondPassParsed = parsed.OtherPasses[0];
        string secondPassResult = FormatOnce(context, secondPassInput, ref secondPassParsed, options);
        parsed.OtherPasses[0] = secondPassParsed;
        return secondPassResult;
    }

    private static string PreprocessBbcode(this IDataContext context, string input, IDataQueryOptions options = null)
    {
        if (options?.BbcodeEnabled ?? false)
        {
            return ((ITextPreprocessor)RootDataContext.Instance).Process(input, options);
        }

        return input;
    }

    private static string FormatOnce(this IDataContext context, string input, ref ParsedDataQuery cachedQuery,
        IDataQueryOptions options = null)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (!input.Contains('{')) return input;
        var sb = new StringBuilder();
        
        ref var parsed = ref ParseQuery(input, DataQueryType.String, ref cachedQuery);
        for (int i = 0; i < parsed.SeparatorIndices.Count; i++)
        {
            (int partStart, int partEnd) = parsed.SeparatorIndices[i];
            sb.Append(parsed.Query, partStart, partEnd - partStart);

            if (i < parsed.InterpolationEntries.Count)
            {
                string entryResult = GetString(parsed.InterpolationEntries[i], context, options);
                entryResult = TranslationServer.Singleton.Tr(entryResult);
                sb.Append(entryResult);
            }
        }
        return sb.ToString();
    }

    private static bool StringRequiresFormatting(string input, IDataQueryOptions options)
    {
        if (string.IsNullOrEmpty(input)) return false;
        if (input.Contains('{')) return true;
        if (RootDataContext.Instance.RequiresProcessing(input, options)) return true;
        return false;
    }

    private static string GetString(ParsedDataQueryInterpolation interpolation, IDataContext context,
        IDataQueryOptions options)
    {
        string replacement = Engine.IsEditorHint() ? $"{{{interpolation.RawString}}}" : "{error}";
        if (!interpolation.CalcExpression.IsEmpty &&
            Evaluate(context, interpolation.CalcExpression.Query, ref interpolation.CalcExpression, options) is
                { } exprResult)
        {
            if (exprResult.VariantType == Variant.Type.Nil) return replacement;
            return exprResult.AsString();
        }
        
        (string key, string input) = GetKeyAndInput(interpolation, ref context);
        if (key == null) return replacement;

        foreach (var ctx in EnumerateContextHierarchy(context))
        {
            if (ctx.GetContextString(key, input, ref replacement, options)) return replacement;
            if (ctx.UseVariablesAsStrings)
            {
                Variant variant = default;
                if (ctx.GetContextVariable(key, input, ref variant, options) &&
                    variant.VariantType is Variant.Type.String or Variant.Type.StringName or Variant.Type.Int
                        or Variant.Type.Float or Variant.Type.Bool)
                    return variant.AsString();
            }
        }

        return replacement;
    }


    // Sub-Contexts
    [Obsolete("Please use the overload with a ParsedDataQuery reference for caching")]
    public static IDataContext EvaluateSubContext(this IDataContext context, string query,
        IDataQueryOptions options = null)
    {
        ParsedDataQuery cached = default;
        return EvaluateSubContext(context, query, ref cached, options);
    }
    public static IDataContext EvaluateSubContext(this IDataContext context, string query, ref ParsedDataQuery cachedQuery,
        IDataQueryOptions options = null)
    {
        if (string.IsNullOrEmpty(query)) return default;
        ref var parsed = ref ParseQuery(query, DataQueryType.SubContext, ref cachedQuery);
        IDataContext value = default;
        if (parsed.InterpolationEntries.Count > 0)
        {
            GetSubContext(parsed.InterpolationEntries[^1], context, ref value, options);
        }
        return value;
    }

    private static bool GetSubContext(ParsedDataQueryInterpolation interpolation, IDataContext context, ref IDataContext output,
        IDataQueryOptions options)
    {
        (string key, string input) = GetKeyAndInput(interpolation, ref context);
        if (key == null) return false;
        return GetSubContextKeyInput(context, key, input, ref output, options);
    }

    private static bool GetSubContextKeyInput(IDataContext context, string key, string input, ref IDataContext output, IDataQueryOptions options)
    {
        if (key == null) return false;
        
        if (key == "_self")
        {
            output = context;
            return true;
        }

        foreach (var ctx in EnumerateContextHierarchy(context))
            if (ctx.GetSubContext(key, input, ref output, options))
                return true;
        return false;
    }


    // Collections
    [Obsolete("Please use the overload with a ParsedDataQuery reference for caching")]
    public static bool EvaluateCollection(this IDataContext context, string query, List<IDataContext> output,
        IDataQueryOptions options = null)
    {
        ParsedDataQuery cached = default;
        return EvaluateCollection(context, query, output, ref cached, options);
    }
    public static bool EvaluateCollection(this IDataContext context, string query, List<IDataContext> output, ref ParsedDataQuery cachedQuery,
        IDataQueryOptions options = null)
    {
        if (string.IsNullOrEmpty(query)) return false;
        ref var parsed = ref ParseQuery(query, DataQueryType.Collection, ref cachedQuery);
        var value = false;
        for (var i = 0; i < parsed.InterpolationEntries.Count; i++)
        {
            if (GetCollection(parsed.InterpolationEntries[i], context, output, options))
            {
                value = true;
            }
        }
        return value;
    }

    private static bool GetCollection(ParsedDataQueryInterpolation interpolation, IDataContext context, List<IDataContext> output,
        IDataQueryOptions options)
    {
        (string key, string input) = GetKeyAndInput(interpolation, ref context);
        if (key == null) return false;
        
        if (key == "_self")
        {
            output.Add(context);
            return true;
        }

        foreach (var ctx in EnumerateContextHierarchy(context))
        {
            if (ctx.GetContextCollection(key, input, output, options))
                return true;
            
            {
                // using sub contexts as single collection entries
                IDataContext subContext = null;
                if (ctx.GetSubContext(key, input, ref subContext, options))
                {
                    output.Add(subContext);
                    return true;
                }
            }
        }
        return false;
    }

    private static readonly List<string> TempSubContextKeys = new();
    private static ParsedDataQueryInterpolation ParseInterpolationString(string interpolationStr)
    {
        // Accept a string in the format: `key:input` OR `key`
        // NOT wrapped in braces

        int keyEndIndex = interpolationStr.IndexOf(':');
        int inputStartIndex;
        if (keyEndIndex == -1)
        {
            keyEndIndex = interpolationStr.Length;
            inputStartIndex = interpolationStr.Length;
        }
        else
        {
            inputStartIndex = keyEndIndex + 1;
        }

        TempSubContextKeys.Clear();
        string fullKey = interpolationStr[..keyEndIndex].ToLowerInvariant();
        string input = interpolationStr[inputStartIndex..];

        int lastKeyStartIndex = 0;
        int dotIndex = -1;
        while ((dotIndex = fullKey.IndexOf('.', lastKeyStartIndex)) != -1)
        {
            string subContextName = fullKey[lastKeyStartIndex..dotIndex];
            TempSubContextKeys.Add(subContextName);
            lastKeyStartIndex = dotIndex + 1;
        }

        var parsed = new ParsedDataQueryInterpolation()
        {
            Key = fullKey[lastKeyStartIndex..],
            Input = input,
            SubContextKeys = TempSubContextKeys.Count > 0 ? TempSubContextKeys.ToArray() : null
        };
        TempSubContextKeys.Clear();
        if (fullKey == "calc")
            ParseQuery(input, DataQueryType.Expression, ref parsed.CalcExpression);
        return parsed;
    }

    private static (string, string) GetKeyAndInput(ParsedDataQueryInterpolation interpolation, ref IDataContext context)
    {
        string key = interpolation.Key;
        string input = interpolation.Input;
        if (interpolation.SubContextKeys is { Length: > 0 })
        {
            for (var i = 0; i < interpolation.SubContextKeys.Length; i++)
            {
                string subContextName = interpolation.SubContextKeys[i];
                IDataContext subContext = null;
                if (!GetSubContextKeyInput(context, subContextName, "", ref subContext, default)) return (null, null);
                context = subContext;
            }
        }
        
        return (key, input);
    }

    private static DataContextEnumerator EnumerateContextHierarchy(IDataContext context)
    {
        return new DataContextEnumerator(context);
    }

    private struct DataContextEnumerator
    {
        private readonly IDataContext _startingContext;
        private IDataContext _next;
        public IDataContext Current { get; private set; }
        private bool _reachedRoot = false;

        public DataContextEnumerator(IDataContext startingContext)
        {
            _startingContext = startingContext;
            Reset();
        }

        public void Reset()
        {
            _next = _startingContext;
            _reachedRoot = false;
        }

        public bool MoveNext()
        {
            Current = _next;
            if (_next == null) return false;

            _next = _next.ParentContext;
            if (_next == null && !_reachedRoot)
            {
                _next = RootDataContext.Instance;
                _reachedRoot = true;
            }

            return true;
        }

        public DataContextEnumerator GetEnumerator()
        {
            return this;
        }
    }

    public static bool OutputBooleanVariable<T>(this T context, bool value, ref Variant output, string input) where T : IDataContext
    {
        if (input.Length != 0)
            output = bool.TryParse(input, out bool b) && b == value;
        else
            output = value;
        return true;
    }

    public static bool OutputStringVariable<T>(this T context, string value, ref Variant output,
        string input) where T : IDataContext
    {
        if (input.Length != 0)
            output = input.Equals(value);
        else
            output = value;
        return true;
    }

    public static bool OutputIntVariable<T>(this T context, int value, ref Variant output, string input) where T : IDataContext
    {
        if (input.Length != 0 && int.TryParse(input, out int comparison))
            output = comparison == value;
        else
            output = value;
        return true;
    }

    public static bool OutputFloatVariable<T>(this T context, float value, ref Variant output, string input) where T : IDataContext
    {
        if (input.Length != 0 && float.TryParse(input, out float comparison))
            output = Math.Abs(comparison - value) < Mathf.Epsilon;
        else
            output = value;
        return true;
    }

    public static void InjectContext(this Node node, IDataContext itemContext)
    {
        node.InjectContext(null, itemContext);
    }
    public static void InjectContext<T>(this Node node, T itemContext) where T : struct, ICacheableDataContext<T>
    {
        node.InjectContext(null, itemContext.Boxed());
    }

    public static void InjectContext<T>(this Node node, StringName injectingSlotId, T itemContext) where T : struct, ICacheableDataContext<T>
    {
        node.InjectContext(injectingSlotId, itemContext.Boxed());
    }

    public static void InjectContext(this Node node, StringName injectingSlotId, IDataContext itemContext)
    {
        if (node == null) return;
        if (node is IDataContextInjectable rootInjectableNode)
        {
            if (rootInjectableNode.CanInject(injectingSlotId))
                rootInjectableNode.SetContexts(itemContext);
        }
        else
        {
            foreach (var injectableNode in node.IterateChildrenOfType<IDataContextInjectable>())
                if (injectableNode.CanInject(injectingSlotId))
                {
                    injectableNode.SetContexts(itemContext);
                    break;
                }
        }
    }
    
    public static bool CanInject(this IDataContextInjectable injectable, StringName injectingSlotId)
    {
        var injectableSlot = injectable.GetInjectableSlotId();
        if (injectableSlot.IsNullOrEmpty() && injectingSlotId.IsNullOrEmpty()) return true;
        return injectingSlotId == injectableSlot;
    }
}

public struct ParsedDataQuery
{
    public readonly string Query;
    public readonly DataQueryType QueryType;
    public bool IsEmpty => Query == null;

    public readonly List<ParsedDataQueryInterpolation> InterpolationEntries;
    public readonly List<(int, int)> SeparatorIndices;
    public Expression Expression;
    public Godot.Collections.Array ExpressionInputs;

    public List<ParsedDataQuery> OtherPasses;

    public string LastFormatOutput;
    public StringName LastFormatOutputStringName;

    public ParsedDataQuery(string query, DataQueryType queryType)
    {
        Query = query;
        QueryType = queryType;
        InterpolationEntries = new();
        SeparatorIndices = new();
    }
}

public struct ParsedDataQueryInterpolation
{
    public string[] SubContextKeys;
    public string Key;
    public string Input;
    public ParsedDataQuery CalcExpression;
    public bool IsEmpty => SubContextKeys == null;
    public string RawString => !CalcExpression.IsEmpty ? $"calc:{CalcExpression.Query}" : (string.IsNullOrEmpty(Input) ? Key : $"{Key}:{Input}");
}