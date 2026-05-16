using System.Collections.Generic;
using Godot;

namespace GDF.Animations;

[GlobalClass]
public partial class GdfAnimationTree : AnimationTree
{
    private readonly Dictionary<StringName, (AnimationNode Node, GdfAnimationNodeMetadata Meta)> _animNodePathsToMetadataNodes = new();
    private readonly Dictionary<StringName, AnimationNodeStateMachinePlayback> _stateMachinePlaybacks = new();

    public override void _Ready()
    {
        base._Ready();
        ScanTree();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        this.SetProcessInternal(false);
        this.SetPhysicsProcessInternal(false);
    }

    public new void Advance(double delta)
    {
        UpdateExpressions(delta);
        base.Advance(delta);
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Active && CallbackModeProcess == AnimationCallbackModeProcess.Idle)
            Advance(delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Active && CallbackModeProcess == AnimationCallbackModeProcess.Physics)
            Advance(delta);
    }
}