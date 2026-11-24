#if TOOLS
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using GDF.DocHints;
using Godot;
using MemberTypes = System.Reflection.MemberTypes;

namespace GDF.Editor;

[Tool]
public partial class DocHintInspectorPlugin : EditorInspectorPlugin
{
    private Type _currentType;
    
    public override bool _CanHandle(GodotObject obj)
    {
        return obj?.GetScript().As<CSharpScript>() != null;
    }

    public override void _ParseBegin(GodotObject obj)
    {
        base._ParseBegin(obj);
        var cs = obj.GetScript().As<CSharpScript>();
        string fullyQualifiedName = GetFullyQualifiedTypeNameFromScript(cs);
        _currentType = GetType().Assembly.GetType(fullyQualifiedName) ?? obj.GetType();
    }

    public override bool _ParseProperty(GodotObject obj, Variant.Type type, string name, PropertyHint hintType, string hintString,
        PropertyUsageFlags usageFlags, bool wide)
    {
        // TODO VERY SCUFFED CODE, NEEDS CLEANING UP
        // Will keep as-is until I come up with another icon I may want to add next to properties.
        
        if (_currentType != null)
        {
            var members = _currentType.GetMember(name, MemberTypes.Field | MemberTypes.Property,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (members.Length > 0)
            {
                foreach (var member in members)
                {
                    if (member.GetCustomAttribute<UsedOnlyAtInitializationAttribute>() != null)
                    {
                        var wrapper = new Control();
                        wrapper.Size = Vector2.Zero;
                        wrapper.MouseFilter = Control.MouseFilterEnum.Ignore;
                        
                        var overlay = new HBoxContainer();
                        wrapper.AddChild(overlay);
                        overlay.CustomMinimumSize = new Vector2(40, 30);
                        overlay.AnchorLeft = 0;
                        overlay.AnchorRight = 0.5f;
                        overlay.AnchorTop = 0;
                        overlay.AnchorBottom = 1;
                        overlay.OffsetTop = 2;
                        overlay.OffsetLeft = 0;
                        overlay.OffsetRight = -8;
                        overlay.GrowHorizontal = Control.GrowDirection.End;
                        overlay.GrowVertical = Control.GrowDirection.End;
                        overlay.MouseFilter = Control.MouseFilterEnum.Ignore;

                        var labelPadding = new Label();
                        overlay.AddChild(labelPadding);
                        labelPadding.Text = GetPropertyEditorLabelText(obj, name);
                        labelPadding.AutowrapMode = TextServer.AutowrapMode.Off;
                        labelPadding.MouseFilter = Control.MouseFilterEnum.Ignore;
                        labelPadding.Modulate = Colors.Transparent;
                        labelPadding.MouseFilter = Control.MouseFilterEnum.Ignore;

                        var iconContainer = new HBoxContainer();
                        overlay.AddChild(iconContainer);
                        iconContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        iconContainer.MouseFilter = Control.MouseFilterEnum.Ignore;

                        {
                            var iconRect = new TextureRect();
                            iconContainer.AddChild(iconRect);
                            iconRect.Texture = GD.Load<Texture2D>($"{GdfConstants.IconRoot}/ready.png");
                            iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                            iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                            iconRect.CustomMinimumSize = new Vector2(16, 16);
                            iconRect.TooltipText = "This property only useful during the initialization of the object (such as on ready).\nSetting this property any time after then will have no effect.";
                        }
                        
                        AddCustomControl(wrapper);
                    }
                }
            }
        }
        return base._ParseProperty(obj, type, name, hintType, hintString, usageFlags, wide);
    }

    private static string GetPropertyEditorLabelText(GodotObject obj, string propertyName)
    {
        var currentGroupPrefix = "";
        foreach (var property in obj.GetPropertyList())
        {
            var usage = property["usage"].As<PropertyUsageFlags>();
            if ((usage & (PropertyUsageFlags.Group | PropertyUsageFlags.Subgroup)) != 0)
            {
                string hintString = property["hint_string"].AsString();
                currentGroupPrefix = hintString;
            }

            if (property["name"].AsString() == propertyName)
            {
                if (currentGroupPrefix.Length > 0 && propertyName.StartsWith(currentGroupPrefix))
                {
                    return propertyName[currentGroupPrefix.Length..].Capitalize();
                }
                else
                {
                    return propertyName.Capitalize();
                }
            }
        }

        return propertyName.Capitalize();
    }

    public override void _ParseEnd(GodotObject @object)
    {
        base._ParseEnd(@object);
        _currentType = null;
    }

    private static string GetFullyQualifiedTypeNameFromScript(CSharpScript cs)
    {
        string source = cs.SourceCode;
        if (source == null) return null;

        var namespaceMatch = NamespacePattern().Match(source);
        if (!namespaceMatch.Success) return null;
        
        string ns = namespaceMatch.Groups[1].Value;
        string filename = cs.ResourcePath.GetBaseName().GetFile();

        return $"{ns}.{filename}";
    }

    [GeneratedRegex(@"\bnamespace\s+([\w\.]+)(?:;|\n)")]
    private static partial Regex NamespacePattern();
}
#endif