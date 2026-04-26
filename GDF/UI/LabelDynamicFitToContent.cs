using Godot;

namespace GDF.UI;

[Tool]
[GlobalClass]
public partial class LabelDynamicFitToContent : LabelPostProcessor
{
    [Export] public int MaximumContentWidth = 100;
    [Export] public Vector2I MinimumSize = new Vector2I();

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
        if (label.AutowrapMode is TextServer.AutowrapMode.Off) return;
        Initialize();
        
        // GD.Print($"Text updated: {label.Text}");
        // GD.Print($"Theme: {label.GetThemeFont("normal_font")?.ResourcePath}");
        // GD.Print($"Content width: {label.GetContentWidth()}");

        CopyLabelTheme(label, _metrics);
        _metrics.Text = " ";
        int spaceWidth = TestContentSizeFullyExpanded(label).X;
        // GD.Print($"Space width: {spaceWidth}");
        
        _metrics.Text = label.Text;
        
        // GD.Print($"Default content size: {TestContentSizeWithMaxSize(label, (Vector2I)label.Size)}");
        
        var contentSizeFullyExpanded = TestContentSizeFullyExpanded(label);
        // GD.Print($"First iteration: {contentSizeFullyExpanded}");

        Vector2I bestFit;
        
        if (contentSizeFullyExpanded.X <= MaximumContentWidth)
        {
            bestFit = contentSizeFullyExpanded;
        }
        else
        {
            bestFit = contentSizeFullyExpanded with { X = MaximumContentWidth };
            // GD.Print($"Second iteration: {bestFit}");
            var contentSizeReducedAgain = TestContentSizeWithMaxSize(label, bestFit);
            if (contentSizeReducedAgain.Y > contentSizeFullyExpanded.Y)
            {
                // Line break happened, require extra space at the end of the longest line just in case.
                contentSizeReducedAgain.X += spaceWidth;
            }
            // GD.Print($"Third iteration: {contentSizeReducedAgain}");
            if (contentSizeReducedAgain.X <= MaximumContentWidth)
            {
                bestFit = contentSizeReducedAgain;
            }
        }
        label.CustomMinimumSize = MinimumSize.Max(bestFit);
    }

    private Vector2I TestContentSizeFullyExpanded(FormattedLabel label)
    {
        _metrics.AutowrapMode = TextServer.AutowrapMode.Off;
        _metrics.FitContent = true;
        return TestContentSize(label, null);
    }

    private Vector2I TestContentSizeWithMaxSize(FormattedLabel label, Vector2I maxSize)
    {
        _metrics.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _metrics.FitContent = false;
        _metrics.CustomMinimumSize = Vector2.Zero;
        _metrics.AnchorRight = 0;
        _metrics.AnchorLeft = 0;
        _metrics.AnchorTop = 0;
        _metrics.AnchorBottom = 0;
        _metrics.OffsetBottom = 0;
        _metrics.OffsetTop = 0;
        _metrics.OffsetLeft = 0;
        _metrics.OffsetRight = 0;
        return TestContentSize(label, maxSize);
    }

    private Vector2I TestContentSize(FormattedLabel label, Vector2I? forceSize)
    {
        label.AddChild(_metrics);
        if (forceSize.HasValue)
        {
            _metrics.Size = forceSize.Value;
        }
        int width = _metrics.GetContentWidth();
        int height = _metrics.GetContentHeight();
        label.RemoveChild(_metrics);

        return new Vector2I(width, height);
    }
}