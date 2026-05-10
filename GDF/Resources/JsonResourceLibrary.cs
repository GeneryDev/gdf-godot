using System.Collections.Generic;
using Godot;

namespace GDF.Resources;

public abstract partial class JsonResourceLibrary<T> : ResourceLibrary<Json, T> where T : IJsonResource, new()
{
    private static readonly Dictionary<StringName, T> IdsToReferences = new();

    public override GodotObject GetObject(StringName id)
    {
        return FromId(id).Reference as GodotObject;
    }

    protected override bool AcceptsFilePath(string filePath)
    {
        return filePath.EndsWith(".json");
    }

    public static void RegisterRuntimeResource(StringName id, string path, T reference)
    {
        RegisterRuntimeResource(id, path);
        IdsToReferences[id] = reference;
    }

    public new static Descriptor FromId(StringName id)
    {
        return ResourceLibrary<Json, T>.FromId(id);
    }

    public new static Descriptor FromPath(string path)
    {
        return ResourceLibrary<Json, T>.FromPath(path);
    }

    public new static Descriptor From(Json json)
    {
        return ResourceLibrary<Json, T>.From(json);
    }

    public static Descriptor From(T instance)
    {
        if (instance == null)
        {
            return FromId(null);
        }
        return FromPath(instance.JsonFilePath);
    }

    public new static Descriptor FromIdOrPath(Variant variant)
    {
        return ResourceLibrary<Json, T>.FromIdOrPath(variant);
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

    public new readonly struct Descriptor
    {
        private readonly ResourceLibrary<Json, T>.Descriptor _innerDescriptor;

        public StringName Id => _innerDescriptor.Id;

        public bool IsEmpty => _innerDescriptor.IsEmpty;

        public Json Json => _innerDescriptor.Resource;

        public T Reference
        {
            get
            {
                if (IsEmpty) return default;
                if (IdsToReferences.TryGetValue(Id, out var existing)) return existing;
                var instance = new T();
                instance.JsonFilePath = Json.ResourcePath;
                instance.Parse(Json);
                return IdsToReferences[Id] = instance;
            }
        }

        public Descriptor(ResourceLibrary<Json, T>.Descriptor innerDescriptor)
        {
            _innerDescriptor = innerDescriptor;
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

        public static implicit operator Descriptor(ResourceLibrary<Json, T>.Descriptor descriptor)
        {
            return new Descriptor(descriptor);
        }

        public static implicit operator Descriptor(T instance)
        {
            return From(instance);
        }

        public static implicit operator Descriptor(Json json)
        {
            return From(json);
        }
    }
}