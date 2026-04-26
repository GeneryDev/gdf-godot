#if TOOLS
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace GDF.Editor;

[Tool]
public partial class ToolInspectorPlugin : EditorInspectorPlugin
{
    private readonly List<ControlToAdd> _controlQueue = new();
    private readonly List<ControlToAdd> _immediateNextControlQueue = new();
    
    public override bool _CanHandle(GodotObject @object)
    {
        return @object?.GetType().GetCustomAttribute<ToolAttribute>() != null;
    }

    public override void _ParseBegin(GodotObject @object)
    {
        _controlQueue.Clear();
        var objectType = @object.GetType();
        foreach (var method in objectType.GetMethods())
        {
            var declaringType = method.DeclaringType;

            var controlAttr = method.GetCustomAttribute<InspectorCustomControlAttribute>();
            if (controlAttr != null)
            {
                if (method.ContainsGenericParameters || method.GetParameters().Length != 0)
                {
                    GD.PushWarning($"Failed to create inspector control for method {method.Name}: method is either generic or requires parameters!");
                    continue;
                }
                _controlQueue.Add(new ControlToAdd()
                {
                    Control = (Control)method.Invoke(@object, System.Array.Empty<object>()),
                    AnchorCategoryOrProperty = controlAttr.AnchorProperty,
                    AnchorMode = controlAttr.AnchorMode,
                    SkipIfAnchorNotFound = false
                });
            }
        }
    }

    public override void _ParseEnd(GodotObject @object)
    {
        foreach (var btnInfo in _controlQueue)
        {
            if (btnInfo.SkipIfAnchorNotFound)
            {
                btnInfo.Control.Free();
                continue;
            }
            AddCustomControl(btnInfo.Control);
        }
        _controlQueue.Clear();
        base._ParseEnd(@object);
    }

    public override void _ParseCategory(GodotObject @object, string category)
    {
        for (var i = 0; i < _controlQueue.Count; i++)
        {
            var btnInfo = _controlQueue[i];
            if (btnInfo.AnchorCategoryOrProperty == category)
            {
                AddCustomControl(btnInfo.Control);
                _controlQueue.RemoveAt(i);
                i--;
            }
        }
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString,
        PropertyUsageFlags usageFlags, bool wide)
    {
        bool returnValue = false;

        foreach(var entry in _immediateNextControlQueue)
        {
            AddCustomControl(entry.Control);
        }
        _immediateNextControlQueue.Clear();
        
        for (var i = 0; i < _controlQueue.Count; i++)
        {
            var entry = _controlQueue[i];
            if (entry.AnchorCategoryOrProperty == name)
            {
                switch (entry.AnchorMode)
                {
                    case InspectorPropertyAnchorMode.Before:
                        AddCustomControl(entry.Control);
                        break;
                    case InspectorPropertyAnchorMode.After:
                        _immediateNextControlQueue.Add(entry);
                        break;
                    case InspectorPropertyAnchorMode.Replace:
                        returnValue = true;
                        if(entry.Control is EditorProperty)
                            AddPropertyEditor(name, entry.Control);
                        else
                            AddCustomControl(entry.Control);
                        break;
                }
                _controlQueue.RemoveAt(i);
                i--;
            }
        }
        
        return returnValue;
    }

    private struct ControlToAdd
    {
        public Control Control;
        public string AnchorCategoryOrProperty;
        public InspectorPropertyAnchorMode AnchorMode;
        public bool SkipIfAnchorNotFound;
    }
}
#endif