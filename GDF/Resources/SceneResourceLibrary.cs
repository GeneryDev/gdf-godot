using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.Resources;

public abstract partial class SceneResourceLibrary<T> : ResourceLibrary<PackedScene, T>
{
    private static readonly Dictionary<StringName, T> IdsToReferences = new();

    public override GodotObject GetObject(StringName id)
    {
        return FromId(id).Reference as GodotObject;
    }

    protected override bool AcceptsFilePath(string filePath)
    {
        return filePath.EndsWith(".tscn");
    }

    public new static Descriptor FromId(StringName id)
    {
        return ResourceLibrary<PackedScene, T>.FromId(id);
    }

    public new static Descriptor FromPath(string path)
    {
        return ResourceLibrary<PackedScene, T>.FromPath(path);
    }

    public new static Descriptor From(PackedScene scene)
    {
        return ResourceLibrary<PackedScene, T>.From(scene);
    }

    public static Descriptor From(T instance)
    {
        if (instance is Node node)
        {
            return FromPath(node.SceneFilePath);
        }
        else if (instance == null)
        {
            return FromId(null);
        }
        else
        {
            GD.PushWarning($"Attempted to get a {typeof(T).Name} descriptor from non-node instance {instance}");
            return default;
        }
    }

    public new static Descriptor FromIdOrPath(Variant variant)
    {
        return ResourceLibrary<PackedScene, T>.FromIdOrPath(variant);
    }

    public static List<Descriptor> CollectAll(List<Descriptor> output)
    {
        output ??= new();
        foreach (var (id, _) in GetResourcePathsById())
        {
            output.Add(FromId(id));
        }

        return output;
    }

    private static void FreeReferences()
    {
        if (Engine.IsEditorHint()) return;
        foreach (var (_, reference) in IdsToReferences)
        {
            (reference as Node)?.Free();
        }
        IdsToReferences.Clear();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationPredelete)
        {
            FreeReferences();
        }
    }

    public new readonly struct Descriptor
    {
        private readonly ResourceLibrary<PackedScene, T>.Descriptor _innerDescriptor;

        public StringName Id => _innerDescriptor.Id;

        public bool IsEmpty => _innerDescriptor.IsEmpty;

        public PackedScene Scene => _innerDescriptor.Resource;

        public SceneSummary Summary
        {
            get
            {
                if (IsEmpty) return default;
                return SceneSummary.From(Path);
            }
        }

        public T Reference
        {
            get
            {
                if (IsEmpty) return default;
                if (IdsToReferences.TryGetValue(Id, out var existing)) return existing;
                return IdsToReferences[Id] = CreateReference();
            }
        }

        public string Path => GetPathForId(Id);

        public Descriptor(ResourceLibrary<PackedScene, T>.Descriptor innerDescriptor)
        {
            _innerDescriptor = innerDescriptor;
        }

        public T New()
        {
            if (IsEmpty) return default;
            return Scene.GdfInstantiate<T>() ?? default;
        }

        // Factory methods

        private T CreateReference()
        {
            if (Summary == null)
            {
                PrintErr($"Failed to create reference instance for '{Id}': No summary exists.");
                return default;
            }
            var constructed = Summary.ConstructRootInstance();
            if (constructed == null)
            {
                PrintErr($"Failed to create reference instance for '{Id}': Instantiation failed.");
                return default;
            }
            if (constructed is T t) return t;
            PrintErr($"Failed to create reference instance for '{Id}': Scene's root node is not of type {typeof(T).Namespace}.");
            return default;
        }

        // Operators

        public bool Equals(Descriptor other)
        {
            return _innerDescriptor == other._innerDescriptor;
        }

        public override bool Equals(object obj)
        {
            return obj is Descriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _innerDescriptor.GetHashCode();
        }

        public static bool operator ==(Descriptor left, Descriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Descriptor left, Descriptor right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Descriptor(ResourceLibrary<PackedScene, T>.Descriptor descriptor)
        {
            return new Descriptor(descriptor);
        }

        public static implicit operator Descriptor(T instance)
        {
            return From(instance);
        }

        public static implicit operator Descriptor(PackedScene scene)
        {
            return From(scene);
        }
    }
}