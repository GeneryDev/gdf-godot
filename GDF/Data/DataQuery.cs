using System.Collections.Generic;
using GDF.Logical.Values;
using Godot;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_query.png")]
public partial class DataQuery : ValueSource, IDataQueryOptions
{
    [Export] public NodePath DataContext;

    [Export(PropertyHint.MultilineText)] public string Query;

    [Export] public DataQueryType QueryType = DataQueryType.Expression;

    [Export] public ValueSource DefaultValue;

    [ExportGroup("Query Options")]
    [Export] public bool SupportsNullOperands { get; set; } = false;
    [Export] public int FontSize { get; set; } = 16;
    [Export] public bool BbcodeEnabled { get; set; } = false;

    public IDataContext OverrideDataContext;

    private ParsedDataQuery _queryCache;

    public override Variant GetValue(Node source)
    {
        var context = OverrideDataContext ?? source.GetNodeOrNull(DataContext) as IDataContext;
        return GetValue(source, context);
    }

    public Variant GetValue(Node source, IDataContext context)
    {
        if (context == null) return GetDefaultValue(source);
        Variant value;
        switch (QueryType)
        {
            case DataQueryType.Expression:
                value = context.Evaluate(Query, ref _queryCache, options: this);
                break;
            case DataQueryType.String:
                value = context.Format(Query, ref _queryCache, options: this);
                break;
            case DataQueryType.Collection:
            {
                var collection = new List<IDataContext>();
                context.EvaluateCollection(Query, collection, ref _queryCache, options: this);
                value = collection.Count;
                break;
            }
            default:
                value = default;
                break;
        }

        if (value.VariantType == Variant.Type.Nil) return GetDefaultValue(source);
        return value;
    }

    public IDataContext GetValueAsContext(Node source)
    {
        return GetValueAsContext(source, OverrideDataContext ?? source.GetNodeOrNull(DataContext) as IDataContext);
    }

    public IDataContext GetValueAsContext(Node source, IDataContext context)
    {
        if (context == null) return default;
        return QueryType switch
        {
            DataQueryType.SubContext => !string.IsNullOrEmpty(Query)
                ? context.EvaluateSubContext(Query, ref _queryCache, options: this)
                : context,
            _ => GetValue(source).AsGodotObject() as IDataContext
        };
    }

    public void EvaluateCollection(Node source, List<IDataContext> output)
    {
        EvaluateCollection(output, OverrideDataContext ?? source.GetNodeOrNull(DataContext) as IDataContext);
    }

    public void EvaluateCollection(List<IDataContext> output, IDataContext context)
    {
        context?.EvaluateCollection(Query, output, ref _queryCache, options: this);
    }

    public Variant GetDefaultValue(Node source)
    {
        return DefaultValue?.GetValue(source) ?? default;
    }

    public override string ToString()
    {
        return Query ?? "{}";
    }
    
    int? IDataQueryOptions.FontSize => FontSize;
    bool? IDataQueryOptions.BbcodeEnabled => BbcodeEnabled;
}