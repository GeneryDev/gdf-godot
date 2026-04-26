using System;
using Godot;

namespace GDF.Input;

public readonly struct GdfInputLocation
{
    private static readonly (string prefix, GdfInputType type)[] TypesByPrefix = new[]
    {
        ("k_", GdfInputType.Key),
        ("mb_", GdfInputType.MouseButton),
        ("mm_", GdfInputType.MouseMotion),
        ("jb_", GdfInputType.JoypadButton),
        ("ja_", GdfInputType.JoypadAxis)
    };
    
    public readonly GdfInputType Type;
    public readonly int Value;
    public readonly int Sign;

    public GdfInputLocation(GdfInputType type, int value, int sign = 0)
    {
        Type = type;
        Value = value;
        Sign = sign;
    }
    public GdfInputLocation(Key key)
    {
        Type = GdfInputType.Key;
        Value = (int)key;
    }
    public GdfInputLocation(MouseButton button)
    {
        Type = GdfInputType.MouseButton;
        Value = (int)button;
    }
    public GdfInputLocation(JoyButton button)
    {
        Type = GdfInputType.JoypadButton;
        Value = (int)button;
    }
    public GdfInputLocation(JoyAxis axis, int sign = 0)
    {
        Type = GdfInputType.JoypadAxis;
        Value = (int)axis;
        Sign = sign;
    }

    public static GdfInputLocation FromEvent(InputEvent evt, bool mustBePress = false)
    {
        switch (evt)
        {
            case InputEventKey kEvt:
                if (mustBePress && kEvt is not {Pressed:true, Echo: false}) break;
                return new GdfInputLocation(kEvt.PhysicalKeycode);
            case InputEventMouseButton mbEvt:
                if (mustBePress && !mbEvt.Pressed) break;
                return new GdfInputLocation(mbEvt.ButtonIndex);
            case InputEventMouseMotion mmEvt:
                return new GdfInputLocation(GdfInputType.MouseMotion, 0);
            case InputEventJoypadButton jbEvt:
                if (mustBePress && !jbEvt.Pressed) break;
                return new GdfInputLocation(jbEvt.ButtonIndex);
            case InputEventJoypadMotion jmEvt:
                if (mustBePress && Mathf.Abs(jmEvt.AxisValue) < 0.5f) break;
                return new GdfInputLocation(jmEvt.Axis, Mathf.Sign(jmEvt.AxisValue));
        }
        return default;
    }

    public static GdfInputLocation Parse(string str)
    {
        return TryParse(str, out var location) ? location : default;
    }

    public static bool TryParse(string str, out GdfInputLocation location)
    {
        location = default;
        if (string.IsNullOrEmpty(str))
            return true;
        foreach ((var prefix, GdfInputType type) in TypesByPrefix)
        {
            if (!str.StartsWith(prefix)) continue;
            str = str[prefix.Length..];
            int code = 0;
            bool valid = false;
            int sign = 0;

            switch (type)
            {
                case GdfInputType.Key:
                {
                    valid = Enum.TryParse(str, true, out Key parsed);
                    code = (int)parsed;
                    break;
                }
                case GdfInputType.MouseButton:
                {
                    valid = Enum.TryParse(str, true, out MouseButton parsed);
                    code = (int)parsed;
                    break;
                }
                case GdfInputType.MouseMotion:
                {
                    valid = true;
                    code = 0;
                    break;
                }
                case GdfInputType.JoypadButton:
                {
                    valid = Enum.TryParse(str, true, out JoyButton parsed);
                    code = (int)parsed;
                    break;
                }
                case GdfInputType.JoypadAxis:
                {
                    if (str.EndsWith('-'))
                    {
                        sign = -1;
                        str = str[..^1];
                    } else if (str.EndsWith('+'))
                    {
                        sign = +1;
                        str = str[..^1];
                    }
                    valid = Enum.TryParse(str, true, out JoyAxis parsed);
                    code = (int)parsed;
                    break;
                }
            }

            if (!valid) return false;
            location = new GdfInputLocation(type, code, sign);
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        if (Type == GdfInputType.None) return "";
        foreach ((var prefix, GdfInputType type) in TypesByPrefix)
        {
            if (Type == type)
            {
                string str = prefix;

                switch (type)
                {
                    case GdfInputType.Key:
                        str += ((Key)Value).ToString();
                        break;
                    case GdfInputType.MouseButton:
                        str += ((MouseButton)Value).ToString();
                        break;
                    case GdfInputType.JoypadButton:
                        str += ((JoyButton)Value).ToString();
                        break;
                    case GdfInputType.JoypadAxis:
                        str += ((JoyAxis)Value).ToString();
                        switch (Sign)
                        {
                            case > 0:
                                str += '+';
                                break;
                            case < 0:
                                str += '-';
                                break;
                        }
                        break;
                }
                return str;
            }
        }

        return "";
    }

    public bool Equals(GdfInputLocation other)
    {
        return Type == other.Type && Value == other.Value && Sign == other.Sign;
    }

    public override bool Equals(object obj)
    {
        return obj is GdfInputLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Type, Value);
    }

    public static bool operator ==(GdfInputLocation left, GdfInputLocation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GdfInputLocation left, GdfInputLocation right)
    {
        return !left.Equals(right);
    }
}

public enum GdfInputType
{
    None,
    Key,
    MouseButton,
    MouseMotion,
    JoypadButton,
    JoypadAxis
}