using Godot;
using Godot.Collections;

namespace GDF.Input;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/input_trigger_input.png")]
public partial class GdfInputTriggerInput : GdfInputTrigger
{
    // Modifiers are not well implemented and take up a lot of space on the inspector.
    // As they are not needed for the time being, they're hidden in the inspector.
    private const bool ModifiersDisabled = true;
    
    [Export] public GdfInputType Type;

    [Export] public Key Key;
    [Export] public MouseButton MouseButton;
    [Export] public JoyButton JoyButton;
    [Export] public JoyAxis JoyAxis;
    
    [Export] public KeyModifierMask Modifiers;
    [Export] public float Deadzone = 0.1f;
    [Export] public bool UseCustomVector = false;
    [Export] public Vector3 UnitVector = new Vector3(1, 0, 0);

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ValueParameterNotUsed
    [Export] private string _locationCode { get => GetInputLocation().ToString(); set {} }
    private int _axisSign = 1;
    
    private GdfInputLocation? _savedInitialLocation;
    
    public override EventMatchResult MatchEvent(GdfPlayerInput player, InputEvent evt)
    {
        switch (Type)
        {
            case GdfInputType.None:
                return default;
            case GdfInputType.Key:
            {
                if (evt is not InputEventKey kEvt) return default;
                if (kEvt.PhysicalKeycode != Key) return default;
                if ((kEvt.GetModifiersMask() & Modifiers) != Modifiers) return default;
                return EventMatchResult.StrongChange;
            }
            case GdfInputType.MouseButton:
            {
                if (evt is not InputEventMouseButton mbEvt) return default;
                if (mbEvt.ButtonIndex != MouseButton) return default;
                if ((mbEvt.GetModifiersMask() & Modifiers) != Modifiers) return default;
                return EventMatchResult.StrongChange;
            }
            case GdfInputType.MouseMotion:
            {
                // TODO what to do with this. Is this even useful?
                if (evt is not InputEventMouseMotion mmEvt) return default;
                return EventMatchResult.WeakChange;
            }
            case GdfInputType.JoypadButton:
            {
                if (evt is not InputEventJoypadButton jbEvt) return default;
                if (jbEvt.ButtonIndex != JoyButton) return default;
                return EventMatchResult.StrongChange;
            }
            case GdfInputType.JoypadAxis:
            {
                if (evt is not InputEventJoypadMotion jmEvt) return default;
                if (jmEvt.Axis != JoyAxis) return default;
                return Mathf.Abs(jmEvt.AxisValue) >= Deadzone ? EventMatchResult.StrongChange : EventMatchResult.WeakChange;
            }
        }
        
        return default;
    }

    private GdfPlayerInput.InputActionState CreateMatch(float strength)
    {
        bool significant = Mathf.Abs(strength) >= Deadzone;
        if (!significant) strength = 0;
        return new GdfPlayerInput.InputActionState()
        {
            BoolValue = significant,
            Strength = strength,
            AxisValue = UseCustomVector ? UnitVector * strength : new Vector3(strength, 0, 0)
        };
    }

    private bool TestModifiers()
    {
        if ((Modifiers & KeyModifierMask.MaskShift) != 0 && !Godot.Input.IsKeyPressed(Key.Shift))
            return false;
        if ((Modifiers & KeyModifierMask.MaskAlt) != 0 && !Godot.Input.IsKeyPressed(Key.Alt))
            return false;
        if ((Modifiers & KeyModifierMask.MaskCtrl) != 0 && !Godot.Input.IsKeyPressed(Key.Ctrl))
            return false;
        if ((Modifiers & KeyModifierMask.MaskMeta) != 0 && !Godot.Input.IsKeyPressed(Key.Meta))
            return false;
        return true;
    }

    public override GdfPlayerInput.InputActionState GetCurrentState(GdfPlayerInput player)
    {
        switch (Type)
        {
            case GdfInputType.None:
                return default;
            case GdfInputType.Key:
            {
                return CreateMatch(Godot.Input.IsPhysicalKeyPressed(Key) ? 1 : 0);
            }
            case GdfInputType.MouseButton:
            {
                return CreateMatch(Godot.Input.IsMouseButtonPressed(MouseButton) ? 1 : 0);
            }
            case GdfInputType.MouseMotion:
            {
                // Unsupported
                return default;
            }
            case GdfInputType.JoypadButton:
            {
                var pressed = false;
                foreach (int device in player.DeviceIndices)
                {
                    if (Godot.Input.IsJoyButtonPressed(device, JoyButton))
                    {
                        pressed = true;
                        break;
                    }
                }
                return CreateMatch(pressed ? 1 : 0);
            }
            case GdfInputType.JoypadAxis:
            {
                var totalValue = 0f;
                foreach (int device in player.DeviceIndices)
                {
                    float axisValue = Godot.Input.GetJoyAxis(device, JoyAxis) * _axisSign;
                    totalValue += axisValue;
                }

                totalValue = Mathf.Clamp(totalValue, -1, 1);
                
                return CreateMatch(totalValue);
            }
        }

        return default;
    }

    public GdfInputLocation GetInputLocation()
    {
        switch (Type)
        {
            case GdfInputType.None:
                return default;
            case GdfInputType.Key:
                return new GdfInputLocation(Key);
            case GdfInputType.MouseButton:
                return new GdfInputLocation(MouseButton);
            case GdfInputType.MouseMotion:
                return new GdfInputLocation(GdfInputType.MouseMotion, 0);
            case GdfInputType.JoypadButton:
                return new GdfInputLocation(JoyButton);
            case GdfInputType.JoypadAxis:
                return new GdfInputLocation(JoyAxis, _axisSign);
        }

        return default;
    }

    public void SetInputLocation(GdfInputLocation location)
    {
        this.Type = location.Type;
        switch (location.Type)
        {
            case GdfInputType.None:
                break;
            case GdfInputType.Key:
                this.Key = (Key)location.Value;
                break;
            case GdfInputType.MouseButton:
                this.MouseButton = (MouseButton)location.Value;
                break;
            case GdfInputType.MouseMotion:
                break;
            case GdfInputType.JoypadButton:
                this.JoyButton = (JoyButton)location.Value;
                break;
            case GdfInputType.JoypadAxis:
                this.JoyAxis = (JoyAxis)location.Value;
                this._axisSign = location.Sign;
                if (_axisSign == 0) _axisSign = 1;
                break;
        }
    }

    public void ClearMapping()
    {
        if (!_savedInitialLocation.HasValue) return;
        SetInputLocation(_savedInitialLocation.Value);
        _savedInitialLocation = null;
    }

    public void ApplyMapping(GdfInputLocation location)
    {
        if (!_savedInitialLocation.HasValue) _savedInitialLocation = GetInputLocation();
        SetInputLocation(location);
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.Type || propName == PropertyName.UseCustomVector)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
        }
        
        if (propName == PropertyName.Modifiers)
        {
            if (ModifiersDisabled) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
            if (Type != GdfInputType.Key && Type != GdfInputType.MouseButton) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        
        if (propName == PropertyName.Key)
        {
            if (Type != GdfInputType.Key) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        if (propName == PropertyName.MouseButton)
        {
            if (Type != GdfInputType.MouseButton) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        if (propName == PropertyName.JoyButton)
        {
            if (Type != GdfInputType.JoypadButton) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        if (propName == PropertyName.JoyAxis)
        {
            if (Type != GdfInputType.JoypadAxis) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        
        if (propName == PropertyName.UnitVector)
        {
            if (!UseCustomVector) usage &= ~(PropertyUsageFlags.Editor | PropertyUsageFlags.Storage);
        }
        
        if (propName == PropertyName._locationCode)
        {
            usage |= PropertyUsageFlags.ReadOnly;
            usage &= ~PropertyUsageFlags.Storage;
        }
        
        property["usage"] = Variant.From(usage);
    }
}