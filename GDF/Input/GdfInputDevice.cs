using System;
using Godot;

namespace GDF.Input;

public struct GdfInputDevice
{
    public GdfInputDeviceType Type;
    public int DeviceIndex;

    public string Name => Type switch
    {
        GdfInputDeviceType.Keyboard => "Default Keyboard",
        GdfInputDeviceType.Mouse => "Default Mouse",
        GdfInputDeviceType.Joypad => JoyName,
        GdfInputDeviceType.Touch => "Default Touch",
        GdfInputDeviceType.Midi => "Default Midi",
        _ => "Other"
    };
    public string JoyName
    {
        get
        {
            if (Type is not GdfInputDeviceType.Joypad) return null;
            var joyInfo = Godot.Input.GetJoyInfo(DeviceIndex);
            if (joyInfo is { Count: > 0 })
            {
                if (joyInfo.TryGetValue("raw_name", out var name)) return name.AsString();
            }
            return Godot.Input.GetJoyName(DeviceIndex);
        }
    }

    public string JoyGuid => Type is not GdfInputDeviceType.Joypad ? null : Godot.Input.GetJoyGuid(DeviceIndex);

    public static GdfInputDevice ForInputEvent(InputEvent evt)
    {
        return new GdfInputDevice()
        {
            Type = evt switch
            {
                InputEventMouse => GdfInputDeviceType.Mouse,
                InputEventKey => GdfInputDeviceType.Keyboard,
                InputEventJoypadMotion or InputEventJoypadButton => GdfInputDeviceType.Joypad,
                InputEventScreenTouch or InputEventScreenDrag or InputEventGesture => GdfInputDeviceType.Touch,
                InputEventMidi => GdfInputDeviceType.Midi,
                _ => GdfInputDeviceType.Other
            },
            DeviceIndex = evt.Device
        };
    }

    public bool Equals(GdfInputDevice other)
    {
        return Type == other.Type && DeviceIndex == other.DeviceIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is GdfInputDevice other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Type, DeviceIndex);
    }

    public static bool operator ==(GdfInputDevice left, GdfInputDevice right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GdfInputDevice left, GdfInputDevice right)
    {
        return !left.Equals(right);
    }
}

public enum GdfInputDeviceType
{
    Keyboard,
    Mouse,
    Joypad,
    Touch,
    Midi,
    Other
}
