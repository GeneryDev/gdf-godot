using System.Linq;
using GDF.Editor;
using Godot;
using Godot.Collections;
using Projects.Sample;

namespace GDF.Data.Parameterized;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_context.png")]
public partial class ParameterizedDataContextNode : Node, IDataContext
{
    [Signal]
    public delegate void UpdatedEventHandler();
    
    [Export(PropertyHint.Enum)]
    public string TypeId
    {
        get => _typeId;
        set
        {
            if (_typeId == value) return;
            _typeId = value;
#if TOOLS
            if (!string.IsNullOrEmpty(_typeId) && EditorUtils.IsSettingPropertyThroughInspector(this))
            {
                this.Name = _typeId.Capitalize();
                Params.Clear();
            }
#endif
            DiscardCache();
            NotifyPropertyListChanged();
            OnParametersUpdated();
        }
    }

    [Export]
    public Dictionary Params
    {
        get => _params;
        set
        {
            if (_params == value) return;
            _params = value;
            EnsureParametersUnique();
            OnParametersUpdated();
        }
    }

    private string _typeId = "";
    
    private Array<Dictionary> _paramPropertyList = null;
    private Dictionary _params = new();
    private IDataContext _context;

    private bool _paramsUnique = false;

    private void DiscardCache()
    {
        _paramPropertyList = null;
    }

    private void EnsureParametersReady()
    {
        EnsurePropertyListReady();

        foreach (string key in Params.Keys.ToArray())
        {
            var keyValid = false;
            foreach(var rawParam in _paramPropertyList)
            {
                if (rawParam["original_name"].AsString() == key)
                {
                    keyValid = true;
                    break;
                }
            }

            if (!keyValid)
            {
                Params.Remove(key);
            }
        }
    }
    
    private void EnsurePropertyListReady()
    {
        if (_paramPropertyList != null) return;
        
        UpdatePropertyList();
    }

    public override bool _Set(StringName property, Variant value)
    {
        EnsureParametersReady();
        foreach (var propertyInfo in _paramPropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                EnsureParametersUnique();
                if (value.VariantType != Variant.Type.Nil)
                    Params[propertyInfo["original_name"].AsString()] = value;
                else
                    Params.Remove(propertyInfo["original_name"].AsString());
                OnParametersUpdated();
            }
        }
        return base._Set(property, value);
    }

    private void EnsureParametersUnique()
    {
        if (_paramsUnique) return;
        _params = _params?.Duplicate();
        _paramsUnique = true;
    }

    public override Variant _Get(StringName property)
    {
        EnsureParametersReady();
        foreach (var propertyInfo in _paramPropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return System.Collections.Generic.CollectionExtensions.GetValueOrDefault(Params, propertyInfo["original_name"].AsString());
            }
        }
        return base._Get(property);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        EnsurePropertyListReady();
        foreach (var propertyInfo in _paramPropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return true;
            }
        }
        return base._PropertyCanRevert(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        EnsurePropertyListReady();
        foreach (var propertyInfo in _paramPropertyList)
        {
            if (propertyInfo["name"].AsStringName() == property)
            {
                return propertyInfo["default"];
            }
        }
        return base._PropertyGetRevert(property);
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        UpdatePropertyList();
        var arr = new Array<Dictionary>();
        arr.AddRange(_paramPropertyList);
        return arr;
    }

    private void UpdatePropertyList()
    {
        _paramPropertyList ??= new();
        _paramPropertyList.Clear();
        if (!string.IsNullOrEmpty(_typeId) && ParameterizedDataContexts.IsValidId(_typeId))
        {
            foreach (var rawProperty in ParameterizedDataContexts.GetPropertyList(_typeId))
            {
                string originalName = rawProperty["name"].AsString();
                var newName = $"params/{originalName}";
                var newProp = new Godot.Collections.Dictionary()
                {
                    {"name", newName},
                    {"type", rawProperty["type"]},
                    {"usage", Variant.From(PropertyUsageFlags.Editor)},
                    {"hint", Variant.From(PropertyHint.None)},
                    {"hint_string", ""},
                    {"original_name", originalName},
                    {"default", (Variant)default}
                };
                _paramPropertyList.Add(newProp);
            }
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();
        if (propertyName == PropertyName.TypeId)
        {
#if TOOLS
            property["hint_string"] = ParameterizedDataContexts.GetAllIds()
                .OrderBy((id) => ParameterizedDataContexts.GetPropertyList(id).Count).ToArray().Join(",");
#endif
        }
        if (propertyName == PropertyName.Params)
        {
            usage &= ~(PropertyUsageFlags.Editor);
        }

        property["usage"] = Variant.From(usage);
        base._ValidateProperty(property);
    }

    private void OnParametersUpdated()
    {
        _context?.DisconnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
        _context = ParameterizedDataContexts.Create(TypeId, Params);
        _context?.ConnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
        EmitSignalUpdated();
    }

    private void OnContextUpdated()
    {
        EmitSignalUpdated();
    }

    public StringName UpdatedSignalName => SignalName.Updated;

    public IDataContext ParentContext => _context;

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationPredelete)
        {
            _context?.DisconnectUpdateSignal(new Callable(this, MethodName.OnContextUpdated));
        }
    }
}