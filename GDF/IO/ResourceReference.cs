using Godot;
using Godot.Collections;

namespace GDF.IO;

[Tool]
[GlobalClass]
public partial class ResourceReference : Resource
{
    [Export]
    public Resource SetResource
    {
        get => null;
        set
        {
            if (value == null) return;
            _cachedResource = null;
            StoredResourcePath = value.ResourcePath;
            StoredResourceName = value.ResourceName;
            StoredResourceType = value.GetType().FullName;
            StoredResourceId = ResourceUid.IdToText(ResourceLoader.GetResourceUid(StoredResourcePath));
            GD.Print($"Set resource reference to: {StoredResourcePath} ({StoredResourceId})");
            ResourceName = StoredResourcePath.Substring(StoredResourcePath.LastIndexOf('/')+1);
        }
    }

    [Export] public string StoredResourceId { get; set; }
    [Export] public string StoredResourcePath { get; set; }
    [Export] public string StoredResourceName { get; set; }
    [Export] public string StoredResourceType { get; set; }

    public string StoredResourceIdOrPath => !string.IsNullOrEmpty(StoredResourceId) ? StoredResourceId : StoredResourcePath;

    [Export]
    public bool Repair
    {
        get => false;
        set
        {
            if (value)
            {
                ValidateReference();
            }
        }
    }

    private Resource _cachedResource;
    
    private string _activeLoadTokenPath;

    public ResourceReference()
    {
        
    }

    public ResourceReference(string path)
    {
        StoredResourcePath = path;
    }

    private string GetTypeHint()
    {
        // For some reason, passing in Godot.PackedScene as a type hint causes issues, so exclude that one
        if (StoredResourceType is null or "Godot.PackedScene") return "";
        return StoredResourceType;
    }

    public void RequestLoad(bool useSubThreads = false, ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Reuse)
    {
        if (_activeLoadTokenPath == StoredResourceIdOrPath)
        {
            // already requested
            return;
        }
        _cachedResource = null;
        ReleaseLoadToken();
        _activeLoadTokenPath = StoredResourceIdOrPath;
        // GD.Print($"Requesting to load {StoredResourceId} [{StoredResourcePath}]");
        ResourceLoader.LoadThreadedRequest(
            StoredResourceIdOrPath,
            typeHint: GetTypeHint(),
            useSubThreads: useSubThreads,
            cacheMode: cacheMode
            );
    }

    public bool CacheIfLoaded()
    {
        if (_cachedResource != null) return true;
        if (GetStatus() == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            GetResource();
            return true;
        }

        return false;
    }

    public bool HasCached()
    {
        return _cachedResource != null || ResourceLoader.HasCached(StoredResourceIdOrPath);
    }

    public ResourceLoader.ThreadLoadStatus GetStatus()
    {
        return _cachedResource != null
            ? ResourceLoader.ThreadLoadStatus.Loaded
            : ResourceLoader.LoadThreadedGetStatus(StoredResourceIdOrPath);
    }

    public Resource GetResource(bool cacheReference = true)
    {
        if (string.IsNullOrEmpty(StoredResourceIdOrPath)) return null;
        if (ResourceLoader.LoadThreadedGetStatus(StoredResourceIdOrPath) is ResourceLoader.ThreadLoadStatus.Loaded or ResourceLoader.ThreadLoadStatus.InProgress)
        {
            // collect result, free load token
            // GD.Print($"Collecting loaded {StoredResourceId} [{StoredResourcePath}]");
            _activeLoadTokenPath = null;
            var res = ResourceLoader.LoadThreadedGet(StoredResourceIdOrPath);
            if (cacheReference) _cachedResource = res;
            return res;
        }
        
        if (!HasCached())
        {
            if (GetStatus() == ResourceLoader.ThreadLoadStatus.InvalidResource) // Not loaded
            {
                var res = ResourceLoader.Load(StoredResourceIdOrPath, GetTypeHint());
                if (cacheReference) _cachedResource = res;
                return res;
            }
        }

        if ((_cachedResource ?? ResourceLoader.GetCachedRef(StoredResourceIdOrPath)) is { } cached)
        {
            if (cacheReference) _cachedResource = cached;
            return cached;
        }
        return null;
    }

    public T GetResource<T>(bool cacheReference = true) where T : Resource
    {
        return (T)GetResource(cacheReference);
    }

    public void ValidateReference()
    {
        bool uidExists = ResourceLoader.Exists(StoredResourceId);
        bool pathExists = ResourceLoader.Exists(StoredResourcePath);

        if (uidExists && pathExists)
        {
            string idForPath = ResourceUid.IdToText(ResourceLoader.GetResourceUid(StoredResourcePath));
            if (idForPath == StoredResourceId)
            {
                // All good!
                GD.Print($"Resource reference {StoredResourcePath} validated correctly, no issues found");
                return;
            }
            else
            {
                GD.Print("Old path exists but UID points to a different path! Using the new path.");
                RepairFromId();
            }
        } else if (uidExists)
        {
            RepairFromId();
        }
        else if (pathExists)
        {
            RepairFromPath();
        }
        else
        {
            GD.PrintErr("Failed to find resource, both by UID and path");
        }
    }

    private void RepairFromId()
    {
        var resource = ResourceLoader.Load(StoredResourceId, cacheMode: ResourceLoader.CacheMode.Ignore);
        if (resource == null)
        {
            GD.PrintErr("Could not infer resource path from " + StoredResourceId + "; resource returned null!");
            return;
        }
        GD.Print("Repaired resource reference by using the stored UID: " + StoredResourceId);
        SetResource = resource;
    }

    private void RepairFromPath()
    {
        var resource = ResourceLoader.Load(StoredResourcePath, cacheMode: ResourceLoader.CacheMode.Ignore);
        if (resource == null)
        {
            GD.PrintErr("Could not infer resource ID from " + StoredResourcePath + "; resource returned null!");
            return;
        }
        GD.Print("Repaired resource reference by using the stored Path: " + StoredResourcePath);
        SetResource = resource;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propertyStringName = property["name"].AsStringName();
        if (propertyStringName == PropertyName.SetResource)
        {
            var usage = property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.DeferredSetResource | PropertyUsageFlags.NoInstanceState;
            property["usage"] = (int)usage;
        }
        if (propertyStringName == PropertyName.StoredResourceId ||
            propertyStringName == PropertyName.StoredResourcePath ||
            propertyStringName == PropertyName.StoredResourceName ||
            propertyStringName == PropertyName.StoredResourceType)
        {
            var usage = property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly;
            property["usage"] = (int)usage;
        }
        base._ValidateProperty(property);
    }

    private void ReleaseLoadToken()
    {
        if (_activeLoadTokenPath != null)
        {
            // GD.Print($"Releasing load token {StoredResourceId} [{StoredResourcePath}]");
            ResourceLoader.LoadThreadedGet(StoredResourceIdOrPath);
            _activeLoadTokenPath = null;
        }
    }
    public void ClearCachedReference()
    {
        _cachedResource = null;
    }

    // I tried using NotificationPredelete, but it does not appear to work on resources. Using finalizer instead
    ~ResourceReference()
    {
        if (!Engine.IsEditorHint())
        {
            ReleaseLoadToken();
        }
    }
}