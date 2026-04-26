using Godot;

namespace GDF.UI;

[Tool]
[GlobalClass]
public abstract partial class LabelPostProcessor : Resource
{
    public abstract void LabelTextUpdated(FormattedLabel label);

    public virtual bool RequiresUpdateOnResize => false;

    private static readonly StringName ThemeNameNormalFont = "normal_font";
    private static readonly StringName ThemeNameBoldFont = "bold_font";
    private static readonly StringName ThemeNameBoldItalicsFont = "bold_italics_font";
    private static readonly StringName ThemeNameItalicsFont = "italics_font";
    private static readonly StringName ThemeNameMonoFont = "mono_font";
    
    private static readonly StringName ThemeNameNormalFontSize = "normal_font_size";
    private static readonly StringName ThemeNameBoldFontSize = "bold_font_size";
    private static readonly StringName ThemeNameBoldItalicsFontSize = "bold_italics_font_size";
    private static readonly StringName ThemeNameItalicsFontSize = "italics_font_size";
    private static readonly StringName ThemeNameMonoFontSize = "mono_font_size";

    public static void CopyLabelTheme(FormattedLabel from, RichTextLabel to)
    {
        to.AddThemeFontOverride(ThemeNameNormalFont, from.GetThemeFont(ThemeNameNormalFont));
        to.AddThemeFontOverride(ThemeNameBoldFont, from.GetThemeFont(ThemeNameBoldFont));
        to.AddThemeFontOverride(ThemeNameBoldItalicsFont, from.GetThemeFont(ThemeNameBoldItalicsFont));
        to.AddThemeFontOverride(ThemeNameItalicsFont, from.GetThemeFont(ThemeNameItalicsFont));
        to.AddThemeFontOverride(ThemeNameMonoFont, from.GetThemeFont(ThemeNameMonoFont));
        
        to.AddThemeFontSizeOverride(ThemeNameNormalFontSize, from.GetThemeFontSize(ThemeNameNormalFontSize));
        to.AddThemeFontSizeOverride(ThemeNameBoldFontSize, from.GetThemeFontSize(ThemeNameBoldFontSize));
        to.AddThemeFontSizeOverride(ThemeNameBoldItalicsFontSize, from.GetThemeFontSize(ThemeNameBoldItalicsFontSize));
        to.AddThemeFontSizeOverride(ThemeNameItalicsFontSize, from.GetThemeFontSize(ThemeNameItalicsFontSize));
        to.AddThemeFontSizeOverride(ThemeNameMonoFontSize, from.GetThemeFontSize(ThemeNameMonoFontSize));

        to.BbcodeEnabled = from.BbcodeEnabled;
    }

    public static void SetLabelFontSizes(RichTextLabel label, int fontSize)
    {
        label.AddThemeFontSizeOverride(ThemeNameNormalFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeNameBoldFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeNameBoldItalicsFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeNameItalicsFontSize, fontSize);
        label.AddThemeFontSizeOverride(ThemeNameMonoFontSize, fontSize);
    }
}