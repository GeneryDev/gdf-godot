using System.Collections.Generic;
using GDF.PropertyStacks.Internal;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.PropertyStacks;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/property_stack.png")]
public partial class PropertyStack : Node
{
    public static PropertyStack GlobalInstance { get; private set; }

    private static readonly List<PropertyStack> AllActiveStacks = new();
    
    [Export] public bool IsGlobal = false;
    
    [Export]
    public PropertyRegistry PropertyRegistry;
    
    [Export]
    public Godot.Collections.Dictionary<string, Variant> OverrideDefaultValues;
    
    private System.Collections.Generic.Dictionary<string, IProperty> _properties;
    private List<PropertyFrameHandle> _frameHandles;
    private List<FrameValidator> _frameValidationRules;
    private int _nextFrameHandleId = 1;

    private PropertyStackDebugger _debugger;
    
    public IEnumerable<string> PropertyIds => _properties.Keys;

    [ExportGroup("Debugging")]
    [Export]
    public bool Debug = false;
    [Export]
    public bool LogValueChanges = false;
    [Export]
    public bool DumpFrameData = false;
    [Export] public Dictionary DebuggerOutput;

    public PropertyStack()
    {
        _properties = new System.Collections.Generic.Dictionary<string, IProperty>();
        _frameHandles = new List<PropertyFrameHandle>();
        _frameValidationRules = new List<FrameValidator>();
    }

    public override void _Ready()
    {
        PropertyRegistry?.PopulatePropertiesDictionary(_properties);

        CreateDefaultValueOverrides();
    }

    private void CreateDefaultValueOverrides()
    {
        if (OverrideDefaultValues is not { Count : > 0 }) return;

        var frame = NewFrame("Overriding Default Values", -9999F);
        foreach ((string key, var value) in OverrideDefaultValues)
        {
            frame.Set(key, value);
        }
    }

    public PropertyFrame NewFrame(string description, float order = 0)
    {
        return new PropertyFrame(NewHandle(description, order), this);
    }

    private PropertyFrameHandle NewHandle(string description, float order = 0)
    {
        int newHandleId = _nextFrameHandleId++;
        var handle = new PropertyFrameHandle(newHandleId, description, order);
        var insertionIndex = 0;
        while (insertionIndex < _frameHandles.Count && _frameHandles[insertionIndex].Order <= handle.Order) insertionIndex++;
        
        _frameHandles.Insert(insertionIndex, handle);
        return handle;
    }

    public void GDSNewFrame(string description, float order = 0, Godot.Collections.Dictionary properties = null, Node boundNode = null)
    {
        var frame = NewFrame(description, order).BindToNode(boundNode);
        if (properties != null)
        {
            foreach (var (key, value) in properties)
            {
                frame.Set(key.AsString(), value);
            }
        }
    }

    public void SetFrameProperty<T>(PropertyFrameHandle handle, string propertyId, T value)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            GD.PushWarning($"No such property '{propertyId}'");
            return;
        }
        
        property.Set(handle, value);
    }

    public void SetFramePropertyWeight(PropertyFrameHandle handle, string propertyId, float weight)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            GD.PushWarning($"No such property '{propertyId}'");
            return;
        }
        
        property.SetWeight(handle, weight);
    }

    public void SetFrameWeight(PropertyFrameHandle handle, float weight)
    {
        foreach (var property in _properties.Values)
        {
            if (property.ContainsHandle(handle))
            {
                property.SetWeight(handle, weight);
            }
        }
    }

    private void InvalidatePropertiesForHandle(PropertyFrameHandle handle)
    {
        foreach (var property in _properties.Values)
        {
            if (property.ContainsHandle(handle))
            {
                property.InvalidateOrder();
            }
        }
    }

    internal bool RemoveFrame(PropertyFrameHandle handle)
    {
        if (!RemoveHandle(ref handle)) return false;
        InvalidatePropertiesForHandle(handle);
        
        // Remove validation rule for this handle
        int ruleIndex = FrameValidatorIndexOf(handle);
        if (ruleIndex >= 0)
        {
            _frameValidationRules.RemoveAt(ruleIndex);
        }

        return true;
    }

    private bool RemoveHandle(ref PropertyFrameHandle handle)
    {
        for (var i = 0; i < _frameHandles.Count; i++)
        {
            if (_frameHandles[i] != handle) continue;
            _frameHandles.RemoveAt(i);
            return true;
        }

        return false;
    }

    private bool RemoveFrame(PropertyFrameHandle handle, int ruleIndex)
    {
        if (!_frameHandles.Remove(handle)) return false;
        InvalidatePropertiesForHandle(handle);
        _frameValidationRules.RemoveAt(ruleIndex);
        
        return true;
    }

    public void SetFrameValidator(PropertyFrameHandle handle, FrameValidator rule)
    {
        rule.Handle = handle;
        
        for (var i = 0; i < _frameValidationRules.Count; i++)
        {
            if (_frameValidationRules[i].Handle == handle)
            {
                _frameValidationRules[i] = rule;
                return;
            }
        }
        _frameValidationRules.Add(rule);
    }

    private int FrameValidatorIndexOf(PropertyFrameHandle handle)
    {
        for (var i = 0; i < _frameValidationRules.Count; i++)
        {
            var rule = _frameValidationRules[i];
            if (rule.Handle == handle) return i;
        }

        return -1;
    }

    public bool RemoveByBoundNode(Node node)
    {
        ulong queriedInstanceId = node.GetInstanceId();
        foreach (var rule in _frameValidationRules)
        {
            if (rule.BoundNodeId.HasValue && rule.BoundNodeId.Value == queriedInstanceId)
            {
                RemoveFrame(rule.Handle);
                return true;
            }
        }

        return false;
    }

    public int GetFrameIndex(PropertyFrameHandle handle)
    {
        for (var i = 0; i < _frameHandles.Count; i++)
        {
            if (_frameHandles[i] == handle) return i;
        }

        return -1;
    }

    public float CompareFrameIndices(PropertyFrameHandle a, PropertyFrameHandle b)
    {
        int frameIndexA = GetFrameIndex(a);
        int frameIndexB = GetFrameIndex(b);
        if(frameIndexA == -1 || frameIndexB == -1) return float.NaN;
        return frameIndexA - frameIndexB;
    }

    public void InvalidatePropertyOrder(string propertyId)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            GD.PushWarning($"No such property '{propertyId}'");
            return;
        }

        property.InvalidateOrder();
    }

    public bool HasProperty(string propertyId)
    {
        return _properties.ContainsKey(propertyId);
    }
    
    public T GetEffectiveValue<T>(string propertyId)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            //GD.PushWarning($"No such property '{propertyId}'");
            return default;
        }

        if (property.IsInheritable())
        {
            UpdateInheritableProperty(propertyId, property);
        }
        
        property.Order(_frameHandles);
        if (property is IProperty<T> typedProperty) return typedProperty.Compute();
        return (T)property.Compute();
    }
    
    public T GetEffectiveValue<T>(string propertyId, T defaultValue)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            return defaultValue;
        }

        if (property.IsInheritable())
        {
            UpdateInheritableProperty(propertyId, property);
        }
        
        property.Order(_frameHandles);
        if (property is IProperty<T> typedProperty) return typedProperty.Compute();
        return (T)property.Compute();
    }

    public Variant GetEffectiveValue(string propertyId)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            GD.PushWarning($"No such property '{propertyId}'");
            return default;
        }

        if (property.IsInheritable())
        {
            UpdateInheritableProperty(propertyId, property);
        }
        
        property.Order(_frameHandles);
        if (property is IPropertyComputableAsVariant variantProperty)
        {
            return variantProperty.ComputeAsVariant();
        }
        object computedValue = property.Compute();
        return property.OutputToVariant(computedValue);
    }

    public int GetModCount(string propertyId)
    {
        if (!_properties.TryGetValue(propertyId, out var property))
        {
            return -1;
        }

        property.Order(_frameHandles);
        return property.GetModCount();
    }

    public Tween CreateFrameTween(PropertyFrame frame, float from, float to, double seconds, Tween.TransitionType transition = Tween.TransitionType.Linear, Tween.EaseType ease = Tween.EaseType.InOut, bool removeAtEnd = false)
    {
        var tween = CreateTween();
        tween.SetIgnoreTimeScale();
        tween.SetTrans(transition);
        tween.SetEase(ease);
        tween.SetProcessMode(Tween.TweenProcessMode.Idle);
        tween.TweenMethod(Callable.From((float weight) => SetFrameWeight(frame, weight)), from, to, seconds);
        if (removeAtEnd) tween.TweenCallback(Callable.From(() => RemoveFrame(frame)));
        return tween;
    }

    private void SetFrameWeight(PropertyFrame frame, float weight)
    {
        frame.SetWeight(weight);
    }

    private void RemoveFrame(PropertyFrame frame)
    {
        frame.Remove();
    }

    private void RunValidationRules()
    {
        var anyRemoved = false;
        for (int i = _frameValidationRules.Count - 1; i >= 0; i--)
        {
            var rule = _frameValidationRules[i];
            if (!rule.IsValid())
            {
                // remove this handle
                RemoveFrame(rule.Handle, i);
                anyRemoved = true;
            }
        }

        if (!anyRemoved) return;
        foreach (var property in _properties.Values)
        {
            property.InvalidateOrder();
        }
    }

    public Array GetFrameDebugInfo()
    {
        var stackArr = new Array();
        foreach (var handle in _frameHandles)
        {
            var handleDict = new Dictionary();
            stackArr.Add(handleDict);
            handleDict["handle"] = handle.Id;
            handleDict["description"] = handle.Description;
            handleDict["order"] = handle.Order;
            
            int validatorIndex = FrameValidatorIndexOf(handle);
            if (validatorIndex >= 0)
            {
                var validator = _frameValidationRules[validatorIndex];
                if(validator.BoundNodeId != null)
                    handleDict["bound_node_id"] = validator.BoundNodeId.Value;
            }
            
            var propertiesDict = new Dictionary();
            handleDict["properties"] = propertiesDict;
            foreach (var property in _properties.Values)
            {
                property.CollectDebugInfoForHandle(handle, propertiesDict);
            }
        }

        return stackArr;
    }

    public override void _Process(double delta)
    {
        RunValidationRules();

        if (Debug)
        {
            _debugger ??= new PropertyStackDebugger();
            DebuggerOutput = _debugger.GetOutput(this, LogValueChanges, DumpFrameData);
        }
    }

    public override void _EnterTree()
    {
        TreeOrderUtil.InsertInTreeOrder(AllActiveStacks, this);
        if (IsGlobal) GlobalInstance = this;
    }

    public override void _ExitTree()
    {
        if (GlobalInstance == this) GlobalInstance = null;
        AllActiveStacks.Remove(this);
    }

    public static PropertyStack GetGlobalInstance()
    {
        return GlobalInstance;
    }

    public struct FrameValidator
    {
        public PropertyFrameHandle Handle;
        public ulong? BoundNodeId;

        public bool IsValid()
        {
            if (BoundNodeId.HasValue && !IsInstanceIdValid(BoundNodeId.Value)) return false;
            return true;
        }
    }
}
