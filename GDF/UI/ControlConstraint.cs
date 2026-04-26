using System;
using System.Collections.Generic;
using System.Linq;
using GDF.Util;
using Godot;

namespace GDF.UI;

[Tool]
[GlobalClass]
public partial class ControlConstraint : Control
{
    private static readonly Dictionary<StringName, ConstraintGroupResolution> ConstraintGroupResolutions = new();

    [Export] public bool Enabled = true;
    [Export] public Control Target;
    [Export] public Control OverrideOffsetTarget;
    [Export] public float ScreenMargin = 0;

    [Export] public bool TryKeepOriginalPosition = true;
    [Export] public bool TryKeepMinimumSize = true;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public Vector2 TargetAnchor;

    [Export] public float FollowLerpStrength = 10;

    [Export]
    public StringName SelfObstructingGroup
    {
        get => _selfObstructingGroup;
        set
        {
            if (!_selfObstructingGroup.IsNullOrEmpty()) RemoveFromGroup(_selfObstructingGroup);
            _selfObstructingGroup = value;
            if (!value.IsNullOrEmpty()) AddToGroup(value);
        }
    }

    [Export] public DisplacementAxisEnum PreferredDisplacementAxis = DisplacementAxisEnum.Both;
    [Export] public bool RunInEditor = false;
    [Export] public bool Debug = false;

    private StringName _selfObstructingGroup;

    public override void _EnterTree()
    {
        if (!_selfObstructingGroup.IsNullOrEmpty()) AddToGroup(_selfObstructingGroup);
    }

    private ConstraintGroupResolution GetObstructionResolution()
    {
        if (_selfObstructingGroup.IsNullOrEmpty()) return null;
        if (!IsInsideTree()) return null;
        if (ConstraintGroupResolutions.TryGetValue(_selfObstructingGroup, out var existing)) return existing;
        return ConstraintGroupResolutions[_selfObstructingGroup] = new ConstraintGroupResolution()
        {
            GroupName = _selfObstructingGroup
        }.Initialize(GetTree());
    }

    public override void _Process(double delta)
    {
        Update(delta);
    }

    private Rect2 _debugRectGlobal;

    public override void _Draw()
    {
        if (!Debug) return;
        var globalTransformInv = GetGlobalTransform().AffineInverse();
        var vtx0 = globalTransformInv * _debugRectGlobal.Position;
        var vtx1 = globalTransformInv * (_debugRectGlobal.Position + _debugRectGlobal.Size * Vector2.Right);
        var vtx2 = globalTransformInv * (_debugRectGlobal.Position + _debugRectGlobal.Size);
        var vtx3 = globalTransformInv * (_debugRectGlobal.Position + _debugRectGlobal.Size * Vector2.Down);
        DrawLine(vtx0, vtx1, Colors.Yellow, 4);
        DrawLine(vtx1, vtx2, Colors.Yellow, 4);
        DrawLine(vtx2, vtx3, Colors.Yellow, 4);
        DrawLine(vtx3, vtx0, Colors.Yellow, 4);
    }

    public Rect2 GetControlTargetRect()
    {
        var rect = Target.GetGlobalRect();
        if (TryKeepOriginalPosition)
            rect.Position = GlobalPosition - rect.Size * TargetAnchor;
        return rect;
    }

    private void Update(double delta)
    {
        if (!RunInEditor && Engine.IsEditorHint()) return;
        if (Target == null || !Enabled) return;

        var windowRect = GetWindow().GetVisibleRect();
        var rect = Target.GetGlobalRect();

        Rect2 targetRect;
        if (!SelfObstructingGroup.IsNullOrEmpty())
        {
            var resolution = GetObstructionResolution().Resolve(GetTree());
            targetRect = resolution.GetResolvedRect(this);
            _debugRectGlobal = targetRect;
        }
        else
        {
            targetRect = GetControlTargetRect();
        }

        if (FollowLerpStrength > 0)
            rect.Position =
                ExpDecay.LerpOverTime(rect.Position, targetRect.Position, (float)(delta * FollowLerpStrength));
        else
            rect.Position = targetRect.Position;
        // GD.Print($"Rect: {_debugRectGlobal}");
        if (Debug) QueueRedraw();

        ConstraintGroupResolution.ResolveOffScreenClipping(windowRect, this, ref rect);

        if (OverrideOffsetTarget != null)
        {
            var movementDelta = rect.Position - Target.GlobalPosition;
            OverrideOffsetTarget.GlobalPosition += movementDelta;
            if (TryKeepMinimumSize) OverrideOffsetTarget.Size = OverrideOffsetTarget.GetCombinedMinimumSize();
        }
        else
        {
            Target.GlobalPosition = rect.Position;
            if (TryKeepMinimumSize) Target.Size = Target.GetCombinedMinimumSize();
        }
    }

    public enum DisplacementAxisEnum
    {
        Both = Horizontal | Vertical,
        Horizontal = 1,
        Vertical = 2
    }

    public partial class ConstraintGroupResolution : GodotObject
    {
        public StringName GroupName;
        public ulong FrameIndex;
        private List<ValueTuple<Control, Rect2>> _resolvedControlRects = new();

        private List<Node> _cachedObstructingNodes;

        public ConstraintGroupResolution Initialize(SceneTree tree)
        {
            // TODO optimize to avoid expensive engine interactions (tree node signals, fetching list of nodes in groups)
            // Idea: Replace all uses of node groups (the Godot kind) with custom scripts that have a Key or Group property. Those scripts should be handling listing logic.
            // For controls that aren't meant to be constrained, but that should obstruct other constrained nodes, add a new "ControlConstraintObstruction" script.
            // The parent of that script's node should be the control constraints have to avoid.
            tree.TryConnect(SceneTree.SignalName.NodeAdded, new Callable(this, MethodName.OnNodeChangedInTree));
            tree.TryConnect(SceneTree.SignalName.NodeRemoved, new Callable(this, MethodName.OnNodeChangedInTree));
            return this;
        }

        public bool IsStillValid()
        {
            return Engine.GetProcessFrames() == FrameIndex;
        }

        public ConstraintGroupResolution Resolve(SceneTree tree)
        {
            // GD.Print("Resolving. Valid: " + IsStillValid(tree));
            if (IsStillValid()) return this;

            FrameIndex = Engine.GetProcessFrames();
            var fullWindowRect = tree.Root.GetVisibleRect();

            var controlsInGroup = _cachedObstructingNodes;
            if (controlsInGroup == null)
                controlsInGroup ??= tree.GetNodesInGroup(GroupName).OrderBy((c) => c is ControlConstraint ? 1 : 0)
                    .ToList();

            // Get all control rectangles
            _resolvedControlRects.Clear();
            foreach (var node in controlsInGroup)
            {
                if (!IsInstanceValid(node))
                {
                    _cachedObstructingNodes = null;
                    continue;
                }

                if (node is not Control control) continue;
                if (control is ControlConstraint constraint)
                {
                    if (constraint.Target == null || !constraint.Enabled) continue;
                    _resolvedControlRects.Add((constraint, constraint.GetControlTargetRect()));
                }
                else
                {
                    _resolvedControlRects.Add((control, control.GetGlobalRect()));
                }
            }

            // Start resolving intersections

            var remainingIterations = 8;
            while (remainingIterations-- > 0)
            {
                var anyIntersections = false;

                // Resolve clipping outside the window frame
                ResolveOffScreenClipping(fullWindowRect);

                // Resolve intersections between controls

                for (var i = 0; i < _resolvedControlRects.Count; i++)
                {
                    var (controlA, rectA) = _resolvedControlRects[i];
                    for (var j = 0; j < _resolvedControlRects.Count; j++)
                    {
                        if (i == j) continue;
                        var (controlB, rectB) = _resolvedControlRects[j];
                        if (controlA is not ControlConstraint && controlB is not ControlConstraint)
                            // neither can be moved
                            continue;

                        if (!rectA.Intersects(rectB)) continue;

                        anyIntersections = true;
                        var intersection = rectA.Intersection(rectB);

                        var displacement = new Vector2(0, 0);
                        var diff = rectB.GetCenter() - rectA.GetCenter();
                        if (diff.IsZeroApprox())
                            // nudge
                            diff = new Vector2(0.1f, -0.1f);
                        var displaceAxes =
                            ((controlA as ControlConstraint)?.PreferredDisplacementAxis ?? DisplacementAxisEnum.Both) &
                            ((controlB as ControlConstraint)?.PreferredDisplacementAxis ?? DisplacementAxisEnum.Both);
                        bool displaceX = (displaceAxes & DisplacementAxisEnum.Horizontal) != 0;
                        bool displaceY = (displaceAxes & DisplacementAxisEnum.Vertical) != 0;

                        bool mainDisplacementAxisIsX =
                            displaceX && (intersection.Size.X <= intersection.Size.Y || !displaceY);
                        bool mainDisplacementAxisIsY =
                            displaceY && (intersection.Size.X > intersection.Size.Y || !displaceX);

                        if (mainDisplacementAxisIsX)
                        {
                            // minimum axis is horizontal
                            if (displaceX)
                                displacement.X = intersection.Size.X;
                            if (!diff.IsZeroApprox() && displaceY)
                                displacement.Y = diff.Normalized().Dot(Vector2.Down) * intersection.Size.Y *
                                                 Math.Sign(diff.Y) * 0.5f;
                        }
                        else if (mainDisplacementAxisIsY)
                        {
                            // minimum axis is vertical
                            if (displaceY)
                                displacement.Y = intersection.Size.Y;
                            if (!diff.IsZeroApprox() && displaceX)
                                displacement.X = diff.Normalized().Dot(Vector2.Right) * intersection.Size.X *
                                                 Math.Sign(diff.X) * 0.5f;
                        }

                        float displacementFactor =
                            controlA is ControlConstraint && controlB is ControlConstraint ? 0.5f : 1;
                        displacement *= displacementFactor;

                        if (controlA is ControlConstraint)
                            rectA.Position -= displacement * new Vector2(Math.Sign(diff.X), Math.Sign(diff.Y));
                        if (controlB is ControlConstraint)
                            rectB.Position += displacement * new Vector2(Math.Sign(diff.X), Math.Sign(diff.Y));

                        ResolveOffScreenClipping(fullWindowRect, controlA, ref rectA);
                        ResolveOffScreenClipping(fullWindowRect, controlB, ref rectB);

                        _resolvedControlRects[i] = (controlA, rectA);
                        _resolvedControlRects[j] = (controlB, rectB);
                    }
                }

                if (!anyIntersections) break;
            }

            return this;
        }

        private void ResolveOffScreenClipping(Rect2 fullWindowRect)
        {
            for (var i = 0; i < _resolvedControlRects.Count; i++)
            {
                var (control, rect) = _resolvedControlRects[i];
                ResolveOffScreenClipping(fullWindowRect, control, ref rect);
                _resolvedControlRects[i] = (control, rect);
            }
        }

        public static void ResolveOffScreenClipping(Rect2 fullWindowRect, Control control, ref Rect2 controlRect)
        {
            if (control is not ControlConstraint constraint)
                // can't be moved
                return;
            var windowRect = fullWindowRect.Grow(-constraint.ScreenMargin);
            if (windowRect.Encloses(controlRect)) return;
            if (controlRect.Position.X < windowRect.Position.X)
                controlRect.Position = controlRect.Position with { X = windowRect.Position.X };
            if (controlRect.Position.Y < windowRect.Position.Y)
                controlRect.Position = controlRect.Position with { Y = windowRect.Position.Y };

            if (controlRect.End.X > windowRect.End.X)
                controlRect.Position = controlRect.End with { X = windowRect.End.X } - controlRect.Size;
            if (controlRect.End.Y > windowRect.End.Y)
                controlRect.Position = controlRect.End with { Y = windowRect.End.Y } - controlRect.Size;
        }

        private void OnNodeChangedInTree(Node node)
        {
            if (node.IsInGroup(GroupName)) _cachedObstructingNodes = null;
        }

        public Rect2 GetResolvedRect(Control control)
        {
            foreach (var (resolvedControl, rect) in _resolvedControlRects)
                if (resolvedControl == control)
                    return rect;

            return default;
        }
    }
}