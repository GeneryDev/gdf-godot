using System;
using System.Collections.Generic;
using System.Text;
using GDF.Util;
using Godot;

// ReSharper disable StaticMemberInGenericType

namespace GDF.Resources;

public abstract partial class ResourceLibrary<TRes, TSub> : Node, IResourceLibrary where TRes : Resource
{
    private static readonly Dictionary<StringName, TRes> CachedResourcesById = new();
    private static readonly Dictionary<StringName, string> ResourcePathsById = new();
    private static readonly Dictionary<string, StringName> IdsByResourcePath = new();

    private static ResourceLibrary<TRes, TSub> _instance;

    public static LibraryConfig Config { get; private set; }
    public static StringName FallbackId => Config.FallbackId;
    public static Descriptor Fallback => FromId(FallbackId);
    
    private static ulong? _editorLastScanTimestamp;

    public virtual LibraryConfig GetLibraryConfig()
    {
        return default;
    }
    protected virtual void OnResourceRegistered(StringName id, string path) {}

    protected virtual bool AcceptsFilePath(string filePath)
    {
        return filePath.EndsWith(".tres");
    }

    public ResourceLibrary()
    {
        _instance = this;
        // ReSharper disable once VirtualMemberCallInConstructor
        Config = GetLibraryConfig();
    }


    public override void _EnterTree()
    {
        base._EnterTree();
        ScanRoots();
        ResourceLibrarySystem.RegisterLibrary(this);
    }

    public override void _Ready()
    {
        base._Ready();
        if(Config.PreloadAll && !Engine.IsEditorHint())
            LoadAllResources();
    }

    void IResourceLibrary.ScanRoots()
    {
        ScanRoots();
    }

    private static void ScanRoots()
    {
#if TOOLS
        if (Engine.IsEditorHint())
        {
            // Skip scanning roots if either roots were scanned in the past second.
            // Additionally, forbid any scanning during the first second after the first request.
            // This is necessary so that we don't accidentally attempt to load resources
            // while hot reloading hasn't finished, which can cause resources to lose all stored data.
            ulong now = Time.GetTicksMsec();
            if (!_editorLastScanTimestamp.HasValue)
            {
                _editorLastScanTimestamp = now;
                return;
            }

            if (now < _editorLastScanTimestamp.Value + 1_000)
            {
                return;
            }
            else
            {
                _editorLastScanTimestamp = now;
                GD.Print($"Scanning roots for library {GetLibraryTypeString()}");
            }
        }
#endif
        if (Config.Roots != null)
        {
            foreach (var root in Config.Roots)
            {
                ScanResourceDirectory(root.Path, root.Recursive, root.KeepRelativeDirectories, root.IdPrefix);
            }
        }
    }

    public static void ScanResourceDirectory(string dirPath, bool recursive = true, bool keepRelativeDirs = false,
        string namePrefix = null)
    {
        if (dirPath.EndsWith('/')) dirPath = dirPath[..^1];
        if (!DirAccess.DirExistsAbsolute(dirPath)) return;

        // Subfolders
        if (recursive && DirAccess.GetDirectoriesAt(dirPath) is { Length: > 0 } subDirs)
            foreach (string dirName in subDirs)
            {
                string subDirPath = dirPath + '/' + dirName;
                string subPrefix = keepRelativeDirs ? (namePrefix ?? "") + (dirName + '/') : namePrefix;

                ScanResourceDirectory(subDirPath, true, keepRelativeDirs, subPrefix);
            }

        if (DirAccess.GetFilesAt(dirPath) is { Length: > 0 } subFiles)
            foreach (string rawFilename in subFiles)
            {
                string filename = rawFilename;
                // Workaround for https://github.com/godotengine/godot/issues/66014
                if (filename.EndsWith(".remap")) filename = filename[..^".remap".Length];
                string filePath = dirPath + '/' + filename;

                if (!_instance.AcceptsFilePath(filePath)) continue;
                string filenameNoExtension = filename[..filename.LastIndexOf('.')];

                string subName = (namePrefix ?? "") + filenameNoExtension;
                RegisterResourceById(subName, filePath);
            }

        return;
    }

    protected static void RegisterResourceById(StringName id, string path)
    {
        if (ResourcePathsById.TryGetValue(id, out string existingPath) && !Engine.IsEditorHint())
        {
            PrintErr(
                $"Resource library contains duplicate definitions for '{id}';\n  First path: {existingPath}\n  Second path: {path}");
            return;
        }

        // GD.Print($"Registered [{id}] ({path})");
        ResourcePathsById[id] = path;
        IdsByResourcePath[path] = id;
        _instance?.OnResourceRegistered(id, path);
    }

    public static void RegisterRuntimeResource(StringName id, string path)
    {
        if (path == null)
        {
            PrintErr($"Resource library was asked to register a null path under id '{id}'");
            return;
        }
        ResourcePathsById[id] = path;
        IdsByResourcePath[path] = id;
    }

    public static void RegisterRuntimeResource(StringName id, TRes resource)
    {
        if (resource == null)
        {
            PrintErr($"Resource library was asked to register a null resource under id '{id}'");
            return;
        }
        string path = resource.ResourcePath;
        ResourcePathsById[id] = path;
        IdsByResourcePath[path] = id;
        CachedResourcesById[id] = resource;
    }

    int IResourceLibrary.ResourceCount => ResourcePathsById.Count;

    public static bool IsIdValid(StringName id)
    {
        if (Engine.IsEditorHint()) ScanRoots();
        return id.IsNullOrEmpty() || ResourcePathsById.ContainsKey(id);
    }

    public static bool IsPathValid(string path)
    {
        if (Engine.IsEditorHint()) ScanRoots();
        return string.IsNullOrEmpty(path) || IdsByResourcePath.ContainsKey(path);
    }

    public static string GetPathForId(StringName id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (Engine.IsEditorHint()) ScanRoots();

        if (ResourcePathsById.TryGetValue(id, out string path)) return path;
        else PrintErr($"No such resource with ID {id}");
        return null;
    }

    public static StringName GetIdForPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Engine.IsEditorHint()) ScanRoots();

        if (IdsByResourcePath.TryGetValue(path, out var id)) return id;
        if (path.Contains("::")) return null; // no errors for sub resources
        PrintErr($"No such resource with path {path}");
        return null;
    }

    bool IResourceLibrary.IsIdValid(StringName id)
    {
        return IsIdValid(id);
    }

    bool IResourceLibrary.IsPathValid(string path)
    {
        return IsPathValid(path);
    }

    string IResourceLibrary.GetPathForId(StringName id)
    {
        return GetPathForId(id);
    }

    StringName IResourceLibrary.GetIdForPath(string path)
    {
        return GetIdForPath(path);
    }

    protected static void PrintErr(string message)
    {
        GD.PrintErr($"[ResourceLibrary of {GetLibraryTypeString()}] {message}");
    }

    private static string GetLibraryTypeString()
    {
        return typeof(TSub) == typeof(TRes)
                ? typeof(TRes).Name
                : $"{typeof(TSub).Name}"
            ;
    }

    string IResourceLibrary.GetLibraryTypeString()
    {
        return GetLibraryTypeString();
    }

    public void LoadAllResources()
    {
        foreach (var entry in ResourcePathsById) GetCachedResource(entry.Key, entry.Value);
    }

    private static TRes GetCachedResource(StringName id, StringName path)
    {
        if (id.IsNullOrEmpty()) return null;
        if (CachedResourcesById.TryGetValue(id, out var existing)) return existing;
        var loaded = GD.Load<TRes>(path);
        if (Config.CacheResources)
            CachedResourcesById[id] = loaded;
        return loaded;
    }

    public static Dictionary<StringName, string>.KeyCollection GetAllIds()
    {
        if (Engine.IsEditorHint()) ScanRoots();
        return ResourcePathsById.Keys;
    }

    IEnumerable<StringName> IResourceLibrary.GetAllIds()
    {
        return GetAllIds();
    }

    public virtual GodotObject GetObject(StringName id)
    {
        return GetCachedResource(id, GetPathForId(id));
    }

    public static void CollectWithTag<T, TTag>(TTag tag, IList<T> output) where T : TRes, ITagged<TTag>
    {
        foreach (var packId in GetAllIds())
        {
            var descriptor = FromId(packId);
            var res = descriptor.Resource;
            var typedRes = (T)res;
            if (!typedRes.HasTag(tag)) continue;

            output.Add(typedRes);
        }
    }
    
    public static Descriptor FromId(StringName id)
    {
        if (IsIdValid(id)) return new Descriptor(id);
        PrintErr($"No such resource with ID {id}");
        return new Descriptor(FallbackId);
    }

    public static Descriptor FromPath(string path)
    {
        return FromId(GetIdForPath(path));
    }

    public static Descriptor From(TRes resource)
    {
        return FromPath(resource?.ResourcePath);
    }

    public static Descriptor FromIdOrPath(Variant variant)
    {
        if (variant.VariantType == Variant.Type.Nil) return Fallback;
        if (variant.VariantType is not (Variant.Type.String or Variant.Type.StringName))
            return Fallback; // invalid type

        // Check correct types first
        if (variant.VariantType == Variant.Type.StringName && IsIdValid(variant.AsStringName()))
            return FromId(variant.AsStringName());
        if (variant.VariantType == Variant.Type.String && IsPathValid(variant.AsString()))
            return FromPath(variant.AsString());

        // Convert to opposite string type for fallback
        if (variant.VariantType == Variant.Type.String && variant.AsStringName() is { } id && IsIdValid(id))
            return FromId(id);
        if (variant.VariantType == Variant.Type.StringName && variant.AsString() is { } path && IsPathValid(path))
            return FromPath(path);

        // Invalid ID or path
        PrintErr($"No such resource with ID or path {variant.AsString()}");

        return Fallback;
    }

    public readonly struct Descriptor : IEquatable<Descriptor>
    {
        public readonly StringName Id;

        public string Path => GetPathForId(Id);

        public bool IsEmpty => Id.IsNullOrEmpty();

        public TRes Resource => GetCachedResource(Id, Path);

        public Descriptor(StringName id)
        {
            Id = id;
        }

        // Operators

        public bool Equals(Descriptor other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is Descriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id != null ? Id.GetHashCode() : 0;
        }

        public static bool operator ==(Descriptor left, Descriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Descriptor left, Descriptor right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Descriptor(TRes resource)
        {
            return From(resource);
        }
    }
}

public struct LibraryConfig
{
    public LibraryRoot[] Roots;
    public StringName FallbackId;
    public bool PreloadAll;
    public bool CacheResources;

    public LibraryConfig()
    {
        Roots = new LibraryRoot[] { };
        FallbackId = null;
        PreloadAll = false;
        CacheResources = false;
    }

    public struct LibraryRoot
    {
        public string Path;
        public bool Recursive;
        public bool KeepRelativeDirectories;
        public string IdPrefix;

        public LibraryRoot(string path, bool recursive = true, bool keepRelativeDirs = false, string idPrefix = null)
        {
            Path = path;
            Recursive = recursive;
            KeepRelativeDirectories = keepRelativeDirs;
            IdPrefix = idPrefix;
        }
    }
}

public interface IResourceLibrary
{
    int ResourceCount { get; }

    public bool IsIdValid(StringName id);
    public bool IsPathValid(string path);

    string GetPathForId(StringName id);
    StringName GetIdForPath(string path);

    IEnumerable<StringName> GetAllIds();
    string GetLibraryTypeString();
    public void ScanRoots();

    public GodotObject GetObject(StringName id);
}

public static class ResourceLibraryExtensions
{
    public static string GetAllIdsCommaSeparated(this IResourceLibrary library)
    {
        var sb = new StringBuilder();
        foreach (var id in library.GetAllIds())
        {
            if(sb.Length > 0)
                sb.Append(',');
            sb.Append(id);
        }

        return sb.ToString();
    }
}