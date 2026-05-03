#if TOOLS
using System;
using System.Reflection;
using Godot;

namespace GDF.Editor;

public static partial class EditorUtils
{
    public static readonly StringName IconNameSignal = "Signal";
    public static readonly StringName IconNameMethod = "Slot";
    

    public static Texture2D GetObjectIcon(GodotObject obj)
    {
        if (obj == null) return null;

        if(GetTypeIcon(obj.GetType()) is {} actualTypeIcon) return actualTypeIcon;
        
        string className = obj.GetClass();
        var theme = EditorInterface.Singleton.GetEditorTheme();
        if (theme.HasIcon(className, "EditorIcons"))
            return theme.GetIcon(className, "EditorIcons");

        return null;
    }

    private static Texture2D GetTypeIcon(Type type)
    {
        foreach (var iconAttr in type.GetCustomAttributes<IconAttribute>())
        {
            var icon = GD.Load<Texture2D>(iconAttr.Path);
            if (icon != null) return icon;
        }

        return null;
    }
}
#endif
