using System.Collections.Generic;
using Godot;

namespace GDF.Components;

[GlobalClass]
public partial class BatchProcessor : Node
{
    [Export] public double TimeScale = 1.0f;

    [Export]
    public bool ProcessOwnChildren = true;
    [Export]
    public Node[] DirectNodes
    {
        get => _directNodes;
        set
        {
            _directNodes = value;
            UpdateProcessModes();
        }
    }
    
    private Node[] _directNodes;
    private readonly List<Node> _ownChildren = new();
    private bool _ownChildrenListOutdated = true;

    public override void _Process(double delta)
    {
        delta *= TimeScale;
        base._Process(delta);
        InvokeProcess(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        delta *= TimeScale;
        base._PhysicsProcess(delta);
        InvokePhysicsProcess(delta);
    }

    private void UpdateProcessModes()
    {
        ReadyForProcessing();
        if (DirectNodes != null)
        {
            foreach (var node in DirectNodes)
            {
                node?.SetProcess(false);
                node?.SetPhysicsProcess(false);
            }
        }

        if (ProcessOwnChildren && _ownChildren.Count > 0)
        {
            foreach (var node in _ownChildren)
            {
                node?.SetProcess(false);
                node?.SetPhysicsProcess(false);
            }
        }
    }

    private void InvokeProcess(double delta)
    {
        ReadyForProcessing();
        if (DirectNodes != null)
        {
            foreach (var node in DirectNodes)
            {
                node?._Process(delta);
            }
        }

        if (ProcessOwnChildren && _ownChildren.Count > 0)
        {
            foreach (var node in _ownChildren)
            {
                node?._Process(delta);
            }
        }
    }

    private void InvokePhysicsProcess(double delta)
    {
        ReadyForProcessing();
        if (DirectNodes != null)
        {
            foreach (var node in DirectNodes)
            {
                node?._PhysicsProcess(delta);
            }
        }

        if (ProcessOwnChildren && _ownChildren.Count > 0)
        {
            foreach (var node in _ownChildren)
            {
                node?._PhysicsProcess(delta);
            }
        }
    }

    private void ReadyForProcessing()
    {
        if (_ownChildrenListOutdated)
        {
            _ownChildrenListOutdated = false;
            _ownChildren.Clear();
            _ownChildren.AddRange(this.GetChildren());
            UpdateProcessModes();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationChildOrderChanged)
        {
            _ownChildrenListOutdated = true;
        }
        if (what == NotificationReady)
        {
            UpdateProcessModes();
            _ownChildrenListOutdated = true;
        }
    }
}