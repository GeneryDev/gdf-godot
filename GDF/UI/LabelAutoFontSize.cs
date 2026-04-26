using Godot;

namespace GDF.UI;

[Tool]
[GlobalClass]
public partial class LabelAutoFontSize : LabelPostProcessor
{
    private static readonly int[] StandardFontSizes = new[]
        { 4, 5, 7, 8, 9, 10, 11, 12, 14, 16, 18, 21, 24, 30, 36, 42, 48, 60, 80 };
    
    [Export] public int BaseFontSize = 16;
    [Export] public int ContentWidthThresholdToShrink = 0;
    [Export] public int MinimumFontSize = 7;
    [Export] public int ContentWidthThresholdToShrinkSubsequentLines = 0;

    public override bool RequiresUpdateOnResize => ContentWidthThresholdToShrink <= 0;

    private RichTextLabel _metrics;

    private void Initialize()
    {
        _metrics ??= new()
        {
            AutowrapMode = TextServer.AutowrapMode.Off,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
            Name = "_metrics"
        };
    }
    
    public override void LabelTextUpdated(FormattedLabel label)
    {
        Initialize();
        
        // GD.Print($"Text updated: {label.Text}");
        // GD.Print($"Theme: {label.GetThemeFont("normal_font")?.ResourcePath}");
        // GD.Print($"Content width: {label.GetContentWidth()}");

        _metrics.Text = label.Text.ReplaceLineEndings("");
        CopyLabelTheme(label, _metrics);

        int baseThresholdToShrink = ContentWidthThresholdToShrink;
        if (ContentWidthThresholdToShrink <= 0) baseThresholdToShrink = (int)label.Size.X + ContentWidthThresholdToShrink;
        int thresholdToShrink = baseThresholdToShrink;
        int thresholdToShrinkSubsequentLines = ContentWidthThresholdToShrinkSubsequentLines != 0 ? ContentWidthThresholdToShrinkSubsequentLines : thresholdToShrink;

        int fontSize = BaseFontSize;
        while (fontSize > MinimumFontSize && fontSize > 0 && thresholdToShrink > 0)
        {
            SetLabelFontSizes(_metrics, fontSize);
            
            label.AddChild(_metrics);
            int totalWidth = _metrics.GetContentWidth();
            label.RemoveChild(_metrics);

            if (totalWidth > thresholdToShrink)
            {
                if (label.AutowrapMode != TextServer.AutowrapMode.Off && thresholdToShrinkSubsequentLines > 0)
                {
                    thresholdToShrink += thresholdToShrinkSubsequentLines;
                }
                fontSize--;
                while (fontSize > MinimumFontSize && fontSize > 0 && !CanUseFontSize(fontSize))
                {
                    fontSize--;
                }
            }
            else
            {
                break;
            }
        }
        
        SetLabelFontSizes(label, fontSize);

        // GD.Print($"Total content width (as one line): {totalWidth}");
    }

    private bool CanUseFontSize(int fontSize)
    {
        foreach (var size in StandardFontSizes)
        {
            if (size == fontSize) return true;
        }

        return false;
    }
}