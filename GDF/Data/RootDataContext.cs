using System;
using System.Collections.Generic;
using System.Linq;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Data;

public class RootDataContext : IDataContext, ITextPreprocessor
{
    public static readonly RootDataContext Instance = new();

    private readonly List<ITextPreprocessor> _bbcodePreprocessors = new();

    public RootDataContext()
    {
    }

    bool IDataContext.GetContextString(string key, string input, ref string replacement,
        IDataQueryOptions options)
    {
        switch (key)
        {
            case "tr" when input.Length != 0:
            {
                replacement = TranslationServer.Translate(input) ?? input;
                return true;
            }
            case "trr" when input.Length != 0:
            {
                ParsedDataQuery cachedQuery = default;
                replacement = this.Format(TranslationServer.Translate(input) ?? input, ref cachedQuery, options);
                return true;
            }
            case "br":
            {
                replacement = "\n";
                return true;
            }
            case "nbsp":
            {
                replacement = "\u00A0";
                return true;
            }
        }

        return false;
    }

    bool IDataContext.GetContextVariable(string key, string input, ref Variant output,
        IDataQueryOptions options)
    {
        switch (key)
        {
        }

        return false;
    }

    public bool GetCollection(string key, string input, List<IDataContext> output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "_collection":
            {
                int colonIndex = input.IndexOf(':');
                string subKey;
                string subInput;
                if (colonIndex < 0)
                {
                    subKey = input;
                    subInput = "";
                }
                else
                {
                    subKey = input[..colonIndex];
                    subInput = input[(colonIndex+1)..];
                }

                var fromIndex = 0;
                for (int i = output.Count - 1; i >= 0; i--)
                {
                    if (output[i] is CollectionStackBoundaryContext boundary)
                    {
                        fromIndex = i + 1;
                        break;
                    }
                }

                return RunCollectionFunction(subKey, subInput, output, options, fromIndex, output.Count - fromIndex);
            }
            case "_collection_stack":
            {
                int colonIndex = input.IndexOf(':');
                string subKey;
                string subInput;
                if (colonIndex < 0)
                {
                    subKey = input;
                    subInput = "";
                }
                else
                {
                    subKey = input[..colonIndex];
                    subInput = input[(colonIndex+1)..];
                }

                var fromIndex = 0;
                for (int i = output.Count - 1; i >= 0; i--)
                {
                    if (output[i] is CollectionStackBoundaryContext boundary)
                    {
                        fromIndex = i + 1;
                        break;
                    }
                }

                return RunCollectionStackFunction(subKey, subInput, output, options, fromIndex, output.Count - fromIndex);
            }
        }

        return false;
    }

    private bool RunCollectionStackFunction(string key, string input, List<IDataContext> output, IDataQueryOptions options, int fromIndex, int count)
    {
        switch (key)
        {
            case "push":
            {
                output.Add(new CollectionStackBoundaryContext().Boxed());
                return true;
            }
            case "pop":
            {
                for (int i = output.Count - 1; i >= 0; i--)
                {
                    if (output[i] is CollectionStackBoundaryContext boundary)
                    {
                        output.RemoveAt(i);
                        break;
                    }
                }

                return true;
            }
        }

        return false;
    }

    private bool RunCollectionFunction(string key, string input, List<IDataContext> output, IDataQueryOptions options, int fromIndex, int count)
    {
        switch (key)
        {
            case "sort":
            case "sort_unstable":
            {
                string filterQuery = input;
                ParsedDataQuery filterQueryCache = default;
                Comparison<IDataContext> comparisonFunction = (a, b) =>
                    a.Evaluate(filterQuery, ref filterQueryCache, options).AsInt32() -
                    b.Evaluate(filterQuery, ref filterQueryCache, options).AsInt32();
                
                output.Sort(fromIndex, count, Comparer<IDataContext>.Create(comparisonFunction));

                return true;
            }
            case "sort_stable":
            {
                string filterQuery = input;
                ParsedDataQuery filterQueryCache = default;
                Func<IDataContext, int> orderFunction = a => a.Evaluate(filterQuery, ref filterQueryCache, options).AsInt32();
                var sorted = output.GetRange(fromIndex, count).OrderBy(orderFunction).ToArray();
                output.RemoveRange(fromIndex, count);
                output.InsertRange(fromIndex, sorted);

                return true;
            }
            case "filter":
            {
                var filterQuery = input;
                ParsedDataQuery filterQueryCache = default;
                for (int i = fromIndex; i < fromIndex + count; i++)
                {
                    var ctx = output[i];
                    var value = ctx.Evaluate(filterQuery, ref filterQueryCache, options);
                    if (!value.AsBool())
                    {
                        output.RemoveAt(i);
                        i--;
                        count--;
                    }
                }

                return true;
            }
            case "shuffle":
            {
                var rand = new RandomNumberGenerator();
                for (int i = fromIndex; i < fromIndex + count - 1; i++)
                {
                    int pickedIndex = rand.RandiRange(i, fromIndex + count - 1);
                    (output[i], output[pickedIndex]) = (output[pickedIndex], output[i]);
                }

                return true;
            }
            case "reverse":
            {
                for (int i = fromIndex; i < (fromIndex + count) / 2; i++)
                {
                    (output[i], output[fromIndex + count - i - 1]) = (output[fromIndex + count - i - 1], output[i]);
                }

                return true;
            }
            case "insert_separators":
            {
                for (var i = fromIndex + 1; i < fromIndex + count; i += 2)
                {
                    if (i >= output.Count) break;
                    output.Insert(i, new SeparatorDataContext(i).Boxed());
                    count++;
                }

                return true;
            }
            case "insert_separators_by_group":
            {
                Variant prevValue = default;
                var separatorQuery = input;
                ParsedDataQuery separatorQueryCache = default;
                for (var i = fromIndex; i < fromIndex + count; i++)
                {
                    var ctx = output[i];
                    var value = ctx.Evaluate(separatorQuery, ref separatorQueryCache, options);
                    if (!value.VariantEquals(prevValue))
                    {
                        // insert separator
                        prevValue = value;
                        output.Insert(i, new SeparatorDataContext(i, ctx).Boxed());
                        i++;
                        count++;
                    }
                }

                return true;
            }
            case "remove_initial_separator":
            {
                if (count > 0 && output[fromIndex] is SeparatorDataContext)
                {
                    output.RemoveAt(fromIndex);
                    count--;
                }
                return true;
            }
            default:
                return false;
        }
    }

    public bool RequiresProcessing(string input, IDataQueryOptions options)
    {
        foreach (var processor in _bbcodePreprocessors)
        {
            if (processor.RequiresProcessing(input, options)) return true;
        }
        return false;
    }

    public string Process(string input, IDataQueryOptions options)
    {
        string str = input;
        foreach (var processor in _bbcodePreprocessors)
        {
            if (processor.RequiresProcessing(str, options))
                str = processor.Process(str, options);
        }
        return str;
    }
}

public struct SeparatorDataContext : IDataContext, ICacheableDataContext<SeparatorDataContext>
{
    public int IndexInCollection;
    public IDataContext Parent;

    public IDataContext ParentContext => Parent;

    public SeparatorDataContext(int indexInCollection)
    {
        IndexInCollection = indexInCollection;
    }

    public SeparatorDataContext(int indexInCollection, IDataContext parent)
    {
        IndexInCollection = indexInCollection;
        Parent = parent;
    }

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "is_separator":
            {
                return this.OutputBooleanVariable(true, ref output, input);
            }
            case "separator_index_in_collection":
            {
                return this.OutputIntVariable(IndexInCollection, ref output, input);
            }
        }

        return false;
    }

    public bool EqualsContext(IDataContext other)
    {
        return other is SeparatorDataContext otherCtx && EqualsContext(otherCtx);
    }

    public bool EqualsContext(SeparatorDataContext otherCtx)
    {
        return IndexInCollection == otherCtx.IndexInCollection && (Parent == otherCtx.Parent || (Parent != null && Parent.EqualsContext(otherCtx.Parent)));
    }

    public bool CanCache() => true;
}

public struct PlaceholderDataContext : IDataContext, ICacheableDataContext<PlaceholderDataContext>
{
    public string Key;
    public IDataContext Parent;

    public IDataContext ParentContext => Parent;

    public PlaceholderDataContext(string key)
    {
        Key = key;
    }

    public PlaceholderDataContext(string key, IDataContext parent)
    {
        Key = key;
        Parent = parent;
    }

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        if (key == Key)
        {
            return this.OutputBooleanVariable(true, ref output, input);
        }
        
        switch (key)
        {
            case "key":
            {
                output = Key;
                return true;
            }
            case "is_placeholder":
            {
                return this.OutputBooleanVariable(true, ref output, input);
            }
        }

        return false;
    }

    public bool EqualsContext(IDataContext other)
    {
        return other is PlaceholderDataContext otherCtx && EqualsContext(otherCtx);
    }

    public bool EqualsContext(PlaceholderDataContext otherCtx)
    {
        return Key == otherCtx.Key && (Parent == otherCtx.Parent || (Parent != null && Parent.EqualsContext(otherCtx.Parent)));
    }

    public bool CanCache() => true;
}

public struct CollectionStackBoundaryContext : IDataContext, ICacheableDataContext<CollectionStackBoundaryContext>
{
    public CollectionStackBoundaryContext()
    {
    }

    public bool EqualsContext(IDataContext other)
    {
        return other is CollectionStackBoundaryContext otherCtx && EqualsContext(otherCtx);
    }

    public bool EqualsContext(CollectionStackBoundaryContext otherCtx)
    {
        return true;
    }

    public bool CanCache() => true;
}