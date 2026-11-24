using System.Collections.Generic;
using GDF.Composition;
using GDF.DocHints;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/attribute_index.svg")]
public partial class AttributeIndex : Node, IAttributeSet<StringName>
{
    [UsedOnlyAtInitialization([CommonAccessTimes.SceneInstantiation, CommonAccessTimes.EnterTree])]
    [Export] public Array<AttributeBlueprint> AttributeBlueprints = new();
    
    public readonly AttributeSet<StringName> AttributeSet = new();

    private bool _initialized = false;

    public override void _Process(double delta)
    {
        base._Process(delta);
        CheckIntegrity(new IntegrityCheckParams()
        {
            RemoveFailures = true
        });
    }

    // AttributeSet Delegates
    
    public void RegisterAttribute(StringName id, IAttribute attribute)
    {
        AttributeSet.RegisterAttribute(id, attribute);
    }

    public bool UnregisterAttribute(StringName id)
    {
        return AttributeSet.UnregisterAttribute(id);
    }

    public bool HasAttribute(StringName id)
    {
        return AttributeSet.HasAttribute(id);
    }

    public void CheckIntegrity(IntegrityCheckParams param)
    {
        AttributeSet.CheckIntegrity(param);
    }

    public AttributeSetModifier<StringName> CreateSetModifier(string description, float order = 0)
    {
        return AttributeSet.CreateSetModifier(description, order);
    }

    public IAttribute GetAttribute(StringName id)
    {
        return AttributeSet.GetAttribute(id);
    }

    public bool IsAttribute<T>(StringName id)
    {
        return AttributeSet.IsAttribute<T>(id);
    }

    public bool IsAttributeComputable<T>(StringName id)
    {
        return AttributeSet.IsAttributeComputable<T>(id);
    }

    public bool IsAttributeModifiable<T>(StringName id)
    {
        return AttributeSet.IsAttributeModifiable<T>(id);
    }

    public T GetValue<T>(StringName id)
    {
        return AttributeSet.GetValue<T>(id);
    }
    
    // Scanning

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        var dict = new System.Collections.Generic.Dictionary<StringName, IAttribute>();
        
        foreach(var attributeSet in this.GetComponents<IIndexableAttributeSet>())
        {
            attributeSet.CollectAttributes(dict);
            foreach (var (id, attribute) in dict)
            {
                RegisterAttribute(id, attribute);
            }
            dict.Clear();
        }

        foreach (var blueprint in AttributeBlueprints)
        {
            blueprint.CreateAttributes(dict);
            foreach (var (id, attribute) in dict)
            {
                RegisterAttribute(id, attribute);
            }
            dict.Clear();
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case GdfConstants.NotificationDeepSceneInstantiated:
            case NotificationEnterTree:
                EnsureInitialized();
                break;
        }
    }
}

public interface IIndexableAttributeSet : IAttributeSet<StringName>
{
    public void CollectAttributes(System.Collections.Generic.Dictionary<StringName, IAttribute> output);
    
    IAttribute IAttributeSet<StringName>.GetAttribute(StringName id)
    {
        var dict = new System.Collections.Generic.Dictionary<StringName, IAttribute>();
        CollectAttributes(dict);
        return dict.GetValueOrDefault(id);
    }
}