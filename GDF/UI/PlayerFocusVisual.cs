using Godot;

namespace GDF.UI;

[Tool]
public partial class PlayerFocusVisual : Panel
{
    private static readonly StringName ThemeOverrideNamePanel = "panel";

    [Export]
    public int OutlineThickness
    {
        get => _outlineThickness;
        set
        {
            _outlineThickness = value;
            if (Engine.IsEditorHint()) Update();
        }
    }

    [Export]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            if (Engine.IsEditorHint()) Update();
        }
    }

    [Export]
    public float InitialLabelAngle
    {
        get => _initialLabelAngle;
        set
        {
            _initialLabelAngle = value;
            if (Engine.IsEditorHint()) Update();
        }
    }

    [Export] public float LabelGap = 4;

    [Export] public float OverlapBaseThicknessMultiplier = 0.75f;
    [Export] public float OverlapExtraThicknessPerLayer = 4f;

    [Export] public StyleBox StyleBox;

    [ExportGroup("Internal Nodes")]
    [Export]
    public Control NameLabel;

    private int _outlineThickness = 8;
    private int _cornerRadius = 16;
    private float _initialLabelAngle = Mathf.Pi / 4f;
    private float _currentLabelAngle;

    public void Update(int occurrenceIndex = 0, int totalOccurrences = 1)
    {
        if (StyleBox == null) return;
        AddThemeStyleboxOverride(ThemeOverrideNamePanel, StyleBox);

        bool isOverlappingAnother = totalOccurrences > 1;

        int effectiveThickness = OutlineThickness;
        if (isOverlappingAnother)
            effectiveThickness = Mathf.RoundToInt(effectiveThickness * OverlapBaseThicknessMultiplier +
                                                  (totalOccurrences - occurrenceIndex - 1) *
                                                  OverlapExtraThicknessPerLayer);

        if (!isOverlappingAnother) occurrenceIndex = 0;

        switch (StyleBox)
        {
            case StyleBoxFlat flat:
                flat.SetBorderWidthAll(effectiveThickness);
                flat.SetExpandMarginAll(effectiveThickness);
                flat.SetCornerRadiusAll(CornerRadius +
                                        (effectiveThickness -
                                         OutlineThickness)); // Increase corner radius to compensate for larger borders
                break;
            case StyleBoxNPatch custom:
                custom.ExpandMarginBottom = custom.ExpandMarginLeft =
                    custom.ExpandMarginLeft = custom.ExpandMarginTop = effectiveThickness;
                break;
        }

        if (NameLabel != null)
        {
            NameLabel.ZIndex = 1;
            _currentLabelAngle = _initialLabelAngle + occurrenceIndex * (Mathf.Tau / totalOccurrences);

            PositionLabel();
        }
    }

    private void PositionLabel()
    {
        var unitVec = Vector2.Left.Rotated(_currentLabelAngle);
        float distance =
            0.5f / Mathf.Cos(Mathf.PosMod(_currentLabelAngle + Mathf.Pi / 4, Mathf.Pi / 2f) - Mathf.Pi / 4f);

        var relativePos = unitVec * distance + new Vector2(0.5f, 0.5f);

        var labelPos = relativePos * Size + unitVec * LabelGap;
        NameLabel.GlobalPosition = GlobalPosition + labelPos - NameLabel.Size / 2f;
    }

    public override void _Process(double delta)
    {
        if (NameLabel != null) PositionLabel();
    }
}