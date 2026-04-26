using GDF.Util;
using Godot;

namespace GDF.Data;

[Tool]
[GlobalClass]
public partial class NodeFactoryConditionalTemplate : Resource
{
    [Export]
    public NodePath TemplatePath;
    [Export]
    public string Query = "";

    private ParsedDataQuery _queryCache;

    // ReSharper disable once InconsistentNaming
    public string resource_name => GetSuggestedName();

    public bool EvaluateConditionQuery(IDataContext item)
    {
        bool matched = item.Evaluate(Query, ref _queryCache).AsBool();
        return matched;
    }

    private string GetSuggestedName()
    {
        return TemplatePath.IsNullOrEmpty() ? "" : $"{TemplatePath.GetName(TemplatePath.GetNameCount() - 1)}: {Query}";
    }
}