using Godot;
using Godot.Collections;

namespace GDF.Physics;

public partial class PhysicsQuerySystem : Node
{
    public static PhysicsQuerySystem Instance { get; private set; }

    [Export] public ImplementationMode Mode = default;

    private RayCast3D _rayCast3D;
    private RayQuery3D? _lastRayQuery;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        base._Ready();
        _rayCast3D = new RayCast3D()
        {
            Enabled = false,
            ExcludeParent = false
        };
        AddChild(_rayCast3D);
    }

    public static RayResult3D Perform(RayQuery3D query)
    {
        if (Engine.IsEditorHint())
        {
            GD.PrintErr("Physics query cannot be fulfilled in the editor");
            return default;
        }

        var rayCast = Instance?._rayCast3D;
        if (rayCast == null)
        {
            GD.PushError(
                $"Could not perform physics query: There is no autoloaded instance of {nameof(PhysicsQuerySystem)} yet loaded");
            return default;
        }

        if (Instance.Mode == ImplementationMode.Off) return default;

        if (Instance.Mode is ImplementationMode.CastNodes or ImplementationMode.CastNodesLazy)
        {
            if (Instance.Mode != ImplementationMode.CastNodesLazy) Instance._lastRayQuery = null;

            if (Instance._lastRayQuery?.CollideWithAreas != query.CollideWithAreas)
                rayCast.CollideWithAreas = query.CollideWithAreas;
            if (Instance._lastRayQuery?.CollideWithBodies != query.CollideWithBodies)
                rayCast.CollideWithBodies = query.CollideWithBodies;
            if (Instance._lastRayQuery?.CollisionMask != query.CollisionMask)
                rayCast.CollisionMask = query.CollisionMask;
            if (Instance._lastRayQuery?.Exclude != query.Exclude)
            {
                rayCast.ClearExceptions();
                if (query.Exclude != null)
                    foreach (var rid in query.Exclude)
                        rayCast.AddExceptionRid(rid);
            }

            if (Instance._lastRayQuery?.From != query.From)
            {
                rayCast.Position = query.From;
                rayCast.TargetPosition = query.To - query.From;
            }
            else if (Instance._lastRayQuery?.To != query.To)
            {
                rayCast.TargetPosition = query.To - query.From;
            }

            if (Instance._lastRayQuery?.HitBackFaces != query.HitBackFaces)
                rayCast.HitBackFaces = query.HitBackFaces;
            if (Instance._lastRayQuery?.HitFromInside != query.HitFromInside)
                rayCast.HitFromInside = query.HitFromInside;
            rayCast.ForceRaycastUpdate();

            Instance._lastRayQuery = query;

            return new RayResult3D()
            {
                Collided = rayCast.IsColliding(),
                Collider = (Node)rayCast.GetCollider(),
                ColliderId = rayCast.GetCollider()?.GetInstanceId() ?? 0,
                Normal = rayCast.GetCollisionNormal(),
                Position = rayCast.GetCollisionPoint(),
                FaceIndex = rayCast.GetCollisionFaceIndex(),
                Rid = rayCast.GetColliderRid(),
                ShapeIndex = rayCast.GetColliderShape()
            };
        }
        else if (Instance.Mode == ImplementationMode.DirectSpaceState)
        {
            var rawResult = rayCast.GetWorld3D().DirectSpaceState.IntersectRay(new PhysicsRayQueryParameters3D()
            {
                CollideWithAreas = query.CollideWithAreas,
                CollideWithBodies = query.CollideWithBodies,
                CollisionMask = query.CollisionMask,
                Exclude = query.Exclude,
                From = query.From,
                To = query.To,
                HitBackFaces = query.HitBackFaces,
                HitFromInside = query.HitFromInside
            });
            if (rawResult.Count <= 0) return default;

            return new RayResult3D()
            {
                Collided = true,
                Collider = (Node)rawResult["collider"].AsGodotObject(),
                ColliderId = rawResult["collider_id"].AsUInt64(),
                Normal = rawResult["normal"].AsVector3(),
                Position = rawResult["position"].AsVector3(),
                FaceIndex = rawResult["face_index"].AsInt32(),
                Rid = rawResult["rid"].AsRid(),
                ShapeIndex = rawResult["shape"].AsInt32()
            };
        }

        return default;
    }

    public enum ImplementationMode
    {
        Off,
        DirectSpaceState,
        CastNodes,
        CastNodesLazy
    }
}

public struct RayQuery3D
{
    public bool CollideWithAreas = false;
    public bool CollideWithBodies = true;
    public uint CollisionMask = ~(uint)0;
    public Array<Rid> Exclude = null;
    public Vector3 From = default;
    public Vector3 To = default;
    public bool HitBackFaces = true;
    public bool HitFromInside = false;

    public RayQuery3D()
    {
    }
}

public struct RayResult3D
{
    public bool Collided = false;

    public Node Collider = null;
    public ulong ColliderId = default;
    public Vector3 Normal = default;
    public Vector3 Position = default;
    public int FaceIndex = -1;
    public Rid Rid = default;
    public int ShapeIndex = 0;

    public RayResult3D()
    {
    }
}