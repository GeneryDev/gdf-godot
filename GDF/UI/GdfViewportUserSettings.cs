using GDF.Serialization;
using Godot;

namespace GDF.UI;

[GlobalClass]
public partial class GdfViewportUserSettings : Resource, IJsonSerializable
{
    [Signal]
    public delegate void SettingChangedEventHandler(string name, Variant value);

    [Export]
    public Vector2I Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution == value) return;
            _resolution = value;
            EmitSignalSettingChanged(nameof(Resolution), value);
        }
    }
    [Export]
    public DisplayServer.WindowMode FullscreenMode
    {
        get => _fullscreenMode;
        set
        {
            if (_fullscreenMode == value) return;
            _fullscreenMode = value;
            EmitSignalSettingChanged(nameof(FullscreenMode), Variant.From(value));
        }
    }

    [Export]
    public DisplayServer.WindowMode LastUsedFullscreenMode
    {
        get => _lastUsedFullscreenMode;
        set
        {
            if (_lastUsedFullscreenMode == value) return;
            _lastUsedFullscreenMode = value;
            EmitSignalSettingChanged(nameof(LastUsedFullscreenMode), Variant.From(value));
        }
    }

    [Export]
    public DisplayServer.WindowMode LastUsedWindowedMode
    {
        get => _lastUsedWindowedMode;
        set
        {
            if (_lastUsedWindowedMode == value) return;
            _lastUsedWindowedMode = value;
            EmitSignalSettingChanged(nameof(LastUsedWindowedMode), Variant.From(value));
        }
    }

    private Vector2I _resolution = default; // default: Resizable
    private DisplayServer.WindowMode _fullscreenMode = default;
    private DisplayServer.WindowMode _lastUsedFullscreenMode = DisplayServer.WindowMode.Fullscreen;
    private DisplayServer.WindowMode _lastUsedWindowedMode = DisplayServer.WindowMode.Maximized;

    public void Deserialize(Variant v)
    {
        var json = JsonSerializer.Default;
        var dict = v.AsGodotDictionary();
        json.DeserializeEnum(dict, nameof(FullscreenMode), ref _fullscreenMode);
        json.DeserializeEnum(dict, nameof(LastUsedFullscreenMode), ref _lastUsedFullscreenMode);
        json.DeserializeEnum(dict, nameof(LastUsedWindowedMode), ref _lastUsedWindowedMode);
        json.Deserialize(dict, nameof(Resolution), ref _resolution);
    }

    public Variant Serialize()
    {
        var json = JsonSerializer.Default;
        var dict = new Godot.Collections.Dictionary();
        json.SerializeEnum(dict, nameof(FullscreenMode), ref _fullscreenMode);
        json.SerializeEnum(dict, nameof(LastUsedFullscreenMode), ref _lastUsedFullscreenMode);
        json.SerializeEnum(dict, nameof(LastUsedWindowedMode), ref _lastUsedWindowedMode);
        json.Serialize(dict, nameof(Resolution), ref _resolution);
        return dict;
    }
}