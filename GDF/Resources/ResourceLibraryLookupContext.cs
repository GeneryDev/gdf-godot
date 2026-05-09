using System.Collections.Generic;
using GDF.Data;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Resources;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_context.png")]
public partial class ResourceLibraryLookupContext : Node, IDataContext
{
    [Signal]
    public delegate void UpdatedEventHandler();

    [Export(PropertyHint.EnumSuggestion)]
    public string LibraryTypeString
    {
        get => _libraryTypeString;
        set
        {
            _libraryTypeString = value;
            Update();
        }
    }

    [Export(PropertyHint.EnumSuggestion)]
    public StringName ResourceId
    {
        get => _resourceId;
        set
        {
            _resourceId = value;
            Update();
        }
    }

    private string _libraryTypeString = "";
    private StringName _resourceId = "";
    
    private GodotObject _resourceObj;

    private bool _connectedToResourceLibrarySystem = false;

    private void Update()
    {
        if (!_connectedToResourceLibrarySystem)
        {
            ResourceLibrarySystem.LibrariesUpdated.Connect(new Callable(this, MethodName.Update));
            _connectedToResourceLibrarySystem = true;
        }
        _resourceObj = FetchResource();
        EmitSignalUpdated();
    }

    private GodotObject FetchResource()
    {
        var library = ResourceLibrarySystem.GetLibraryByTypeString(LibraryTypeString);
        if (library?.IsIdValid(ResourceId) ?? false)
        {
            if (!library.IsIdValid(ResourceId)) return null;
            return library.GetObject(ResourceId);
        }

        return null;
    }

    public IDataContext ParentContext => _resourceObj as IDataContext;
    public StringName UpdatedSignalName => SignalName.Updated;

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "resource":
            {
                output = _resourceObj;
                return true;
            }
        }

        return false;
    }

    public bool GetSubContext(string key, string input, ref IDataContext output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "context":
            case "resource":
            case "object":
            case "reference":
            case "resource_context":
            case "object_context":
            case "reference_context":
            {
                output = _resourceObj as IDataContext;
                return true;
            }
        }

        return false;
    }

    public bool GetCollection(string key, string input, List<IDataContext> output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "all":
            {
                var library = ResourceLibrarySystem.GetLibraryByTypeString(LibraryTypeString);
                if (library == null) return false;
                foreach (var id in library.GetAllIds())
                {
                    var obj = library.GetObject(id);
                    if(obj is IDataContext ctx) output.Add(ctx);
                }
                return true;
            }
            case "all_in_library":
            {
                var library = ResourceLibrarySystem.GetLibraryByTypeString(input);
                if (library == null) return false;
                foreach (var id in library.GetAllIds())
                {
                    var obj = library.GetObject(id);
                    if(obj is IDataContext ctx) output.Add(ctx);
                }
                return true;
            }
        }

        return false;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.LibraryTypeString)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
            property["hint_string"] = ResourceLibrarySystem.GetAllTypeStringsCommaSeparated();
        }

        if (propName == PropertyName.ResourceId && Engine.IsEditorHint())
        {
            var library = ResourceLibrarySystem.GetLibraryByTypeString(LibraryTypeString);
            property["hint_string"] = library?.GetAllIdsCommaSeparated() ?? "";
        }

        property["usage"] = Variant.From(usage);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationPredelete)
        {
            if (_connectedToResourceLibrarySystem)
            {
                ResourceLibrarySystem.LibrariesUpdated.Disconnect(new Callable(this, MethodName.Update));
            }
        }
    }
}