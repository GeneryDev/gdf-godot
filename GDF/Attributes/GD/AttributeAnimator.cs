using GDF.Composition;
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
                if(IsInsideTree() && _setModifier == null) CreateSetModifier();
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
            _setModifier?.SetWeight(value);
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
    
    [Export] public int Order = 0;

    [Export] public UpdateModeEnum UpdateMode = UpdateModeEnum.Idle;

    // TODO allow assigning remote attribute indices
    private ComponentCache<AttributeIndex> _attributeIndex = new();
    
    private AttributeSetModifier<StringName> _setModifier;
    private float _weight = 1.0f;
    private bool _active = false;

    private Array<Dictionary> _cachedPropertyList;
    private Dictionary<StringName, StringName> _generatedPropertyToKeyMap;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint()) return;
        if (Active && _setModifier == null)
        {
            CreateSetModifier();
        }
    }
    
    private void CreateSetModifier()
    {
        if (_setModifier != null)
        {
            return;
        }
        if (!IsInsideTree())
        {
            GD.PrintErr($"Cannot apply {nameof(AttributeAnimator)} from outside the tree");
        }

        if (_attributeIndex.Get(this) is not { } index) return;

        _setModifier = index.CreateSetModifier($"{nameof(AttributeAnimator)} [{Name}]", order: Order).BindToNode(this);
        
        _setModifier.SetWeight(Weight);

        UpdateSetModifier(_setModifier);
        EmitSignalBecameActive();
    }

    private void UpdateSetModifier(AttributeSetModifier<StringName> frame)
    {
        if (frame == null) return;
        if (Modifiers != null)
            foreach ((var key, IModifyInvoker modifierRes) in Modifiers)
            {
                if (modifierRes is AttributeModifierWithValueSource vs)
                {
                    vs.SetSourceNode(this);
                }

                if (!_setModifier.TryModify(key, modifierRes))
                    if (!_setModifier.TryModify(key, modifierRes))
                        _setModifier.Modify(key, Variant.From(modifierRes));
            }
    }

    private void ClearSetModifier()
    {
        if (_setModifier == null) return;
        _setModifier = _setModifier.Clear();
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
        
        if (Active && _setModifier == null) CreateSetModifier();
        if (Active && _setModifier != null) UpdateSetModifier(_setModifier);
        if (!Active && _setModifier != null) ClearSetModifier();
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