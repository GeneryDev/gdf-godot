using Godot;
using Godot.Collections;

namespace GDF.Input;

[Tool]
[GlobalClass]
public partial class GdfInputAction : Resource
{
    [Export]
    public string Name;
    [Export]
    public string DisplayName;

    [ExportGroup("Events")]
    [Export] public bool SendEventOnPress = false;
    [Export] public bool SendEventOnRelease = false;
    [Export] public bool SendEventOnExpire = false;
    [Export(PropertyHint.Range, "0,1000,1,or_greater,suffix:ms")]
    public int EventBufferTime = 250;
    [Export(PropertyHint.Range, "0,1000,1,or_greater,suffix:ms")]
    public int ExpireEventBufferTime = 100;

    private string _cachedResourcePath;
    public string ActionKey => _cachedResourcePath ??= ResourcePath;
    
    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.SendEventOnPress || propName == PropertyName.SendEventOnRelease || propName == PropertyName.SendEventOnExpire)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
        }
        if (propName == PropertyName.EventBufferTime)
        {
            if (!(SendEventOnPress || SendEventOnRelease)) usage &= ~PropertyUsageFlags.Editor;
        }
        if (propName == PropertyName.ExpireEventBufferTime)
        {
            if (!(SendEventOnExpire)) usage &= ~PropertyUsageFlags.Editor;
        }
        property["usage"] = Variant.From(usage);
    }
}
