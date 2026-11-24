using System.Collections.Generic;
using GDF.Attributes.Impl;
using GDF.Util;
using Godot;

namespace GDF.Attributes;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class EngineAttributes : SingletonNode<EngineAttributes>, IIndexableAttributeSet
{
    public BoolAttribute Paused = new BoolAttribute(false)
    {
        DefaultFunction = BoolModifierChain.Functions.Or
    };
    public FloatAttribute TimeScale = new FloatAttribute(1.0f)
    {
        DefaultFunction = FloatModifierChain.Functions.Multiply
    };

    public VariantAttribute<Godot.Input.MouseModeEnum> MouseMode = new(Godot.Input.MouseModeEnum.Visible);

    private ValueChangeTracker<bool> _trackerPaused = new();
    private ValueChangeTracker<float> _trackerTimeScale = new();
    private ValueChangeTracker<int> _trackerMouseMode = new();
    
    public void CollectAttributes(Dictionary<StringName, IAttribute> output)
    {
        output["paused"] = Paused;
        output["time_scale"] = TimeScale;
        output["mouse_mode"] = MouseMode;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateEngineAttributes();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateEngineAttributes();
        UpdateWindowAttributes();
    }

    private void UpdateEngineAttributes()
    {
        if (_trackerPaused.HasChanged(Paused)) GetTree().Paused = Paused;
        if (_trackerTimeScale.HasChanged(TimeScale)) Engine.TimeScale = TimeScale;
    }

    private void UpdateWindowAttributes()
    {
        if (_trackerMouseMode.HasChanged((int)MouseMode.CurrentValue)) Godot.Input.MouseMode = MouseMode.CurrentValue;
    }
}