using System.Reflection;
using Godot;
using Godot.Collections;

namespace GDF.Resources;

[Tool]
[GlobalClass]
public abstract partial class SummarizableScene : Node
{
    [Export]
    public SceneSummary Summary
    {
        get => _summary;
        set
        {
            _summary = value;
            LoadFromSummary();
        }
    }
    public bool IsReconstructedFromSummary = false;

    private bool _generatingSummary = false;
    private SceneSummary _summary;

    public bool IsGeneratingSummary => _generatingSummary;

    private void StartGeneratingSummary()
    {
        _generatingSummary = true;
        Summary ??= new();
        Summary.ResourcePath = SceneSummary.SummaryPathFromScenePath(this.SceneFilePath);
        Summary.Clear();

        Summary.RootNodeScript = this.GetScript().As<Script>();
        Summary.RootNodeName = Name;

        foreach (var property in this.GetPropertyList())
        {
            if ((property["usage"].As<PropertyUsageFlags>() &
                (PropertyUsageFlags.Group | PropertyUsageFlags.Subgroup | PropertyUsageFlags.Category)) != 0) continue;
            
            var propNameSn = property["name"].AsStringName();
            string propNameStr = property["name"].AsString();
            
            if (GetIncludeInSummaryAttributeForProperty(propNameStr) == null) continue;
            if (IsNodeType(property))
            {
                GD.PrintErr($"Cannot include property '{propNameStr}' in scene summary: Node types cannot be stored in summaries!");
                continue;
            }

            var value = this.Get(propNameSn);
            Summary.RootNodeProperties[propNameSn] = value;
        }

        ResourceSaver.Save(Summary);
    }

    private static bool IsNodeType(Dictionary property)
    {
        var hint = property["hint"].As<PropertyHint>();
        if (hint == PropertyHint.NodeType) return true;
        return false;
    }

    private StoreInSummaryAttribute GetIncludeInSummaryAttributeForProperty(string name)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                           BindingFlags.FlattenHierarchy;
        var type = GetType();
        var memberInfo = (MemberInfo)type.GetField(name, bindingFlags) ?? type.GetProperty(name, bindingFlags);
        return memberInfo?.GetCustomAttribute<StoreInSummaryAttribute>();
    }

    private void FinishGeneratingSummary()
    {
        _generatingSummary = false;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case NotificationEditorPreSave:
            {
                if (EditorInterface.Singleton.GetEditedSceneRoot() == this && !string.IsNullOrEmpty(this.SceneFilePath))
                {
                    StartGeneratingSummary();
                }
                break;
            }
            case NotificationEditorPostSave:
            {
                if(_generatingSummary) FinishGeneratingSummary();
                break;
            }
            case NotificationSceneInstantiated:
            {
                if (_summary != null && Engine.IsEditorHint())
                {
                    string expectedSummaryPath = SceneSummary.SummaryPathFromScenePath(SceneFilePath);
                    if (expectedSummaryPath != _summary.ResourcePath)
                    {
                        GD.Print("This summarizable scene was moved! Making a copy of the SceneSummary file.");
                        _summary = (SceneSummary)_summary.Duplicate();
                        _summary.RootNodeProperties = _summary.RootNodeProperties.Duplicate();
                        _summary.ResourcePath = SceneSummary.SummaryPathFromScenePath(this.SceneFilePath);
                    }
                }

                break;
            }
        }
    }

    private void LoadFromSummary()
    {
        if (Summary != null)
        {
            Name = Summary.RootNodeName;
            foreach (var (propName, value) in Summary.RootNodeProperties)
            {
                Set(propName.AsStringName(), value);
            }
        }
    }
    
    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.Summary)
        {
            usage &= ~(PropertyUsageFlags.Editor);
        }

        if (_generatingSummary && Engine.IsEditorHint() && EditorInterface.Singleton.GetEditedSceneRoot() == this &&
            !string.IsNullOrEmpty(this.SceneFilePath) && Summary != null)
        {
            if (Summary.RootNodeProperties.ContainsKey(propName) && GetIncludeInSummaryAttributeForProperty(propName) is { AlsoStoreInScene: false })
            {
                usage &= ~(PropertyUsageFlags.Storage);
            }
        }
        
        property["usage"] = Variant.From(usage);
    }
}