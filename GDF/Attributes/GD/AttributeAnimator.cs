using GDF.Composition;
using GDF.DocHints;
using GDF.Logical.Values;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/attribute_animator.svg")]
public partial class AttributeAnimator : Node
{
    [Signal]
    public delegate void BecameActiveEventHandler();

    [Signal]
    public delegate void BecameInactiveEventHandler();
    
    private const string GeneratedPropertyPrefix = "modifiers/";
    
    private Godot.Collections.Dictionary<StringName, AttributeModifierResource> _modifiers = new();
    
    [Export]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            if (value)
            {
                if(IsInsideTree() && _bulkModifier == null) CreateBulkModifier();
            }
            else
            {
                ClearSetModifier();
            }
        }
    }
    
    [Export]
    public float Weight
    {
        get => _weight;
        set
        {
            _weight = value;
            _bulkModifier?.SetWeight(value);
        }
    }

    [Export]
    public Godot.Collections.Dictionary<StringName, AttributeModifierResource> Modifiers
    {
        get => _modifiers;
        set
        {
            ClearCache();
            if (_modifiers == value) return;
            _modifiers = value?.Duplicate(); //Godot please stop sharing exported collections across instances of a scene thx
        }
    }
    
    [UsedOnlyAtInitialization([CommonAccessTimes.Other])]
    [Export] public int Order = 0;

    [Export] public UpdateModeEnum UpdateMode = UpdateModeEnum.Idle;

    // TODO allow assigning remote attribute indices
    private ComponentCache<AttributeIndex> _attributeIndex = new();
    
    private BulkModifier _bulkModifier;
    private float _weight = 1.0f;
    private bool _active = false;

    private Array<Dictionary> _cachedPropertyList;
    private Dictionary<StringName, StringName> _generatedPropertyToKeyMap;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint()) return;
        if (Active && _bulkModifier == null)
        {
            CreateBulkModifier();
        }
    }
    
    private void CreateBulkModifier()
    {
        if (_bulkModifier != null)
        {
            return;
        }
        if (!IsInsideTree())
        {
            GD.PrintErr($"Cannot apply {nameof(AttributeAnimator)} from outside the tree");
        }

        if (_attributeIndex.Get(this) is not { } index) return;

        _bulkModifier = new BulkModifier($"{nameof(AttributeAnimator)} [{Name}]", Order).BindToNode(this);
        
        _bulkModifier.SetWeight(Weight);

        UpdateSetModifier(_bulkModifier);
        EmitSignalBecameActive();
    }

    private void UpdateSetModifier(BulkModifier frame)
    {
        if (frame == null) return;
        if (Modifiers != null)
            foreach ((var id, IModifyInvoker modifierRes) in Modifiers)
            {
                if (modifierRes is AttributeModifierWithValueSource vs)
                {
                    vs.SetSourceNode(this);
                }

                var attr = _attributeIndex.Get(this)?.GetAttribute(id);

                if (!_bulkModifier.TryModify(attr, modifierRes))
                    if (!_bulkModifier.TryModify(attr, modifierRes))
                        _bulkModifier.Modify(attr, Variant.From(modifierRes));
            }
    }

    private void ClearSetModifier()
    {
        if (_bulkModifier == null) return;
        _bulkModifier = _bulkModifier.Clear();
        EmitSignalBecameInactive();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (UpdateMode == UpdateModeEnum.Idle) Update();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (UpdateMode == UpdateModeEnum.Physics) Update();
    }

    public void Update()
    {
        if (Engine.IsEditorHint()) return;
        
        if (Active && _bulkModifier == null) CreateBulkModifier();
        if (Active && _bulkModifier != null) UpdateSetModifier(_bulkModifier);
        if (!Active && _bulkModifier != null) ClearSetModifier();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (Engine.IsEditorHint()) return;
        ClearSetModifier();
    }

    private void ClearCache()
    {
        _cachedPropertyList = null;
        _generatedPropertyToKeyMap = null;
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        EnsurePropertyListReady();
        return _cachedPropertyList;
    }

    private void EnsurePropertyListReady()
    {
        if (_cachedPropertyList != null) return;

        _cachedPropertyList = new();
        _generatedPropertyToKeyMap ??= new();
        foreach ((var key, IModifyInvoker value) in _modifiers)
        {
            string generatedName = GeneratedPropertyPrefix + key;
            _cachedPropertyList.Add(new Dictionary()
            {
                {"name", generatedName},
                {"type", Variant.From(Variant.Type.Nil)},
                {"hint", Variant.From(PropertyHint.None)},
                {"hint_string", ""},
                {"usage", Variant.From(PropertyUsageFlags.Editor | PropertyUsageFlags.NilIsVariant)}
            });
            _generatedPropertyToKeyMap[generatedName] = key;
        }
    }

    public override Variant _Get(StringName property)
    {
        EnsurePropertyListReady();
        if (_generatedPropertyToKeyMap.TryGetValue(property, out var key))
        {
            IModifyInvoker modifierRes = _modifiers[key];
            switch (modifierRes)
            {
                case AttributeModifierWithValueSource attributeModifierWithValueSource:
                    return attributeModifierWithValueSource.OperandSource;
                case AttributeModifierResource attributeModifierResource:
                    return attributeModifierResource.Operand;
            }
        }
        return base._Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        EnsurePropertyListReady();
        if (_generatedPropertyToKeyMap.TryGetValue(property, out var key))
        {
            IModifyInvoker modifierRes = _modifiers[key];
            switch (modifierRes)
            {
                case AttributeModifierWithValueSource attributeModifierWithValueSource:
                    attributeModifierWithValueSource.OperandSource = value.As<ValueSource>();
                    break;
                case AttributeModifierResource attributeModifierResource:
                    attributeModifierResource.Operand = value;
                    break;
            }
            return true;
        }
        return base._Set(property, value);
    }

    public override bool _PropertyCanRevert(StringName property)
    {
        EnsurePropertyListReady();
        return _generatedPropertyToKeyMap.ContainsKey(property);
    }

    public override Variant _PropertyGetRevert(StringName property)
    {
        return default;
    }
    
    public enum UpdateModeEnum
    {
        Idle,
        Physics,
        Manual
    }
}