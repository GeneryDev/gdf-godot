using Godot;
using Godot.Collections;

namespace GDF.Resources;

[Tool]
[GlobalClass]
public partial class SceneSummary : Resource
{
    private const string SummaryPathSuffix = ".summary.tres";

    [Export] public StringName RootNodeName = new();
    [Export] public Script RootNodeScript;
    [Export] public Dictionary RootNodeProperties = new();

    public static string SummaryPathFromScenePath(string scenePath)
    {
        return scenePath + SummaryPathSuffix;
    }
    public static string ScenePathFromSummaryPath(string summaryPath)
    {
        if (!IsSummaryPath(SummaryPathSuffix)) return null;
        return summaryPath[..^SummaryPathSuffix.Length];
    }
    public static bool IsSummaryPath(string path)
    {
        return path.StartsWith(SummaryPathSuffix);
    }

    public void Clear()
    {
        RootNodeName = new();
        RootNodeScript = null;
        RootNodeProperties.Clear();
    }

    public Node ConstructRootInstance()
    {
        var instance = ((CSharpScript)RootNodeScript).New().As<SummarizableScene>();
        if (instance != null)
        {
            instance.Name = RootNodeName;
            instance.Summary = this; // Setter copies properties
        }

        return instance;
    }

    public static SceneSummary From(string scenePath)
    {
        string summaryPath = SummaryPathFromScenePath(scenePath);
        if (summaryPath == null) return null;
        var summary = GD.Load<SceneSummary>(summaryPath);
        return summary;
    }
}