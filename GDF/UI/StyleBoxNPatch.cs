using Godot;
using Godot.Collections;

namespace GDF.UI;

[Tool]
[GlobalClass]
public partial class StyleBoxNPatch : StyleBox
{
    [Export]
    public Texture2D Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            EmitChanged();
        }
    }

    [Export]
    public Vector2I OverrideTextureSize
    {
        get => _overrideTextureSize;
        set
        {
            _overrideTextureSize = value;
            EmitChanged();
        }
    }
    
    [ExportGroup("N-Patch")]
    [Export(PropertyHint.Range, "1,16,1,or_greater")]
    public int PatchCountHorizontal // N_x
    {
        get => _patchCountHorizontal;
        set
        {
            _patchCountHorizontal = value;
            EmitChanged();
        }
    }

    [Export(PropertyHint.Range, "1,16,1,or_greater")]
    public int PatchCountVertical // N_y
    {
        get => _patchCountVertical;
        set
        {
            _patchCountVertical = value;
            EmitChanged();
        }
    }

    [Export]
    public Array<int> SlicesX // N_x-1
    {
        get => _slicesX;
        set
        {
            _slicesX = value;
            EmitChanged();
        }
    }

    [Export]
    public Array<int> SlicesY // N_y-1
    {
        get => _slicesY;
        set
        {
            _slicesY = value;
            EmitChanged();
        }
    }

    [Export]
    public Array<float> PatchWeightsX // N_x
    {
        get => _patchWeightsX;
        set
        {
            _patchWeightsX = value;
            EmitChanged();
        }
    }

    [Export]
    public Array<float> PatchWeightsY // N_y
    {
        get => _patchWeightsY;
        set
        {
            _patchWeightsY = value;
            EmitChanged();
        }
    }

    [Export]
    public Array<RenderingServer.NinePatchAxisMode> PatchAxisModesX // N_x
    {
        get => _patchAxisModesX;
        set
        {
            _patchAxisModesX = value;
            EmitChanged();
        }
    }

    [Export]
    public Array<RenderingServer.NinePatchAxisMode> PatchAxisModesY // N_y
    {
        get => _patchAxisModesY;
        set
        {
            _patchAxisModesY = value;
            EmitChanged();
        }
    }
    
    [Export(PropertyHint.Range,"0.001,10,0.001,or_greater")]
    public float ResolutionScale
    {
        get => _resolutionScale;
        set
        {
            _resolutionScale = value;
            EmitChanged();
        }
    }

    [ExportGroup("Expand Margins", "ExpandMargin")]
    [Export]
    public float ExpandMarginLeft
    {
        get => _expandMarginLeft;
        set
        {
            _expandMarginLeft = value;
            EmitChanged();
        }
    }
    [Export]
    public float ExpandMarginTop
    {
        get => _expandMarginTop;
        set
        {
            _expandMarginTop = value;
            EmitChanged();
        }
    }
    [Export]
    public float ExpandMarginRight
    {
        get => _expandMarginRight;
        set
        {
            _expandMarginRight = value;
            EmitChanged();
        }
    }
    [Export]
    public float ExpandMarginBottom
    {
        get => _expandMarginBottom;
        set
        {
            _expandMarginBottom = value;
            EmitChanged();
        }
    }

    [ExportGroup("Sub-Region")]
    [Export]
    public Rect2I SubRegion
    {
        get => _subRegion;
        set
        {
            _subRegion = value;
            EmitChanged();
        }
    }

    [ExportGroup("Modulate", "Modulate")]
    [Export]
    public Color ModulateColor
    {
        get => _modulateColor;
        set
        {
            _modulateColor = value;
            EmitChanged();
        }
    }

    [ExportGroup("Importing")]
    [Export] public StyleBoxTexture ImportSettingsFrom
    {
        get => null;
        set => CallDeferred(MethodName.ImportSettings, value);
    }

    private Texture2D _texture;
    private Vector2I _overrideTextureSize;
    private int _patchCountHorizontal = 1;
    private int _patchCountVertical = 1;
    private Array<int> _slicesX = new();
    private Array<int> _slicesY = new();
    private Array<float> _patchWeightsX = new();
    private Array<float> _patchWeightsY = new();
    private Array<RenderingServer.NinePatchAxisMode> _patchAxisModesX = new();
    private Array<RenderingServer.NinePatchAxisMode> _patchAxisModesY = new();
    private Rect2I _subRegion;
    private float _resolutionScale = 1.0f;
    private float _expandMarginLeft = 0;
    private float _expandMarginTop = 0;
    private float _expandMarginRight = 0;
    private float _expandMarginBottom = 0;
    private Color _modulateColor = Colors.White;

    public override Vector2 _GetMinimumSize()
    {
        var referenceSize = GetReferenceSize();

        int nx = _patchCountHorizontal;
        int ny = _patchCountVertical;
        
        CollectAxisData(SlicesX, PatchWeightsX, nx, referenceSize.X,
            out float _,
            out float fixedWidth);
        
        CollectAxisData(SlicesY, PatchWeightsY, ny, referenceSize.Y,
            out float _,
            out float fixedHeight);

        return new Vector2(fixedWidth, fixedHeight);
    }
    
    public override void _Draw(Rid toCanvasItem, Rect2 rect)
    {
        rect = rect.GrowIndividual(_expandMarginLeft, _expandMarginTop, _expandMarginRight, _expandMarginBottom);
        base._Draw(toCanvasItem, rect);
        if (Texture == null) return;
        var textureRid = _texture.GetRid();

        var referenceSize = GetReferenceSize();

        int nx = _patchCountHorizontal;
        int ny = _patchCountVertical;
        
        CollectAxisData(SlicesX, PatchWeightsX, nx, referenceSize.X,
            out float totalWeightX,
            out float fixedWidth);
        float flexibleWidth = rect.Size.X - fixedWidth;
        
        CollectAxisData(SlicesY, PatchWeightsY, ny, referenceSize.Y,
            out float totalWeightY,
            out float fixedHeight);
        float flexibleHeight = rect.Size.Y - fixedHeight;

        float drawX = 0;
        for (int ix = 0; ix < nx; ix++)
        {
            CollectPatchExtents(SlicesX, ix, nx, referenceSize.X, out int fromX, out int toX, out int patchWidth);

            float weightX = GetSliceParam(PatchWeightsX, ix);
            var axisModeX = GetSliceParam(PatchAxisModesX, ix);

            float drawWidth = patchWidth;
            if (float.IsFinite(_resolutionScale))
                drawWidth /= _resolutionScale;
            if (weightX != 0)
                drawWidth = flexibleWidth * (weightX / totalWeightX);
            
            float drawY = 0;
            for (int iy = 0; iy < ny; iy++)
            {
                CollectPatchExtents(SlicesY, iy, ny, referenceSize.Y, out int fromY, out int toY,
                    out int patchHeight);

                float weightY = GetSliceParam(PatchWeightsY, iy);
                var axisModeY = GetSliceParam(PatchAxisModesY, iy);

                float drawHeight = patchHeight;
                if (float.IsFinite(_resolutionScale))
                    drawHeight /= _resolutionScale;
                if (weightY != 0)
                    drawHeight = flexibleHeight * (weightY / totalWeightY);
                
                if (drawWidth > 0 && drawHeight > 0)
                {
                    var drawRect = new Rect2(rect.Position + new Vector2(drawX, drawY), new Vector2(drawWidth, drawHeight));
                    var sourceRect = new Rect2(fromX, fromY, patchWidth, patchHeight);
                    DrawPatch(toCanvasItem, drawRect, textureRid, sourceRect, axisModeX, axisModeY);
                }
                
                drawY += drawHeight;
            }
            
            drawX += drawWidth;
        }
    }

    private void DrawPatch(Rid toCanvasItem, Rect2 patchRect, Rid textureRid, Rect2 sourceRect, RenderingServer.NinePatchAxisMode axisModeX, RenderingServer.NinePatchAxisMode axisModeY)
    {
        if (patchRect.Size.X <= 0 || patchRect.Size.Y <= 0) return;
        
        if (axisModeX is RenderingServer.NinePatchAxisMode.Stretch &&
            axisModeY is RenderingServer.NinePatchAxisMode.Stretch)
        {
            RenderingServer.CanvasItemAddTextureRectRegion(toCanvasItem, patchRect, textureRid, ReferenceRectToSourceTextureRect(sourceRect), _modulateColor);
            return;
        }

        CollectPatchTileData(patchRect.Size.X, sourceRect.Size.X, axisModeX, out var tileSizeX, out var stretchX);
        CollectPatchTileData(patchRect.Size.Y, sourceRect.Size.Y, axisModeY, out var tileSizeY, out var stretchY);
        
        for (float fromX = 0; fromX < patchRect.Size.X; fromX += tileSizeX)
        {
            float toX = Mathf.Min(fromX + tileSizeX, patchRect.Size.X);

            for (float fromY = 0; fromY < patchRect.Size.Y; fromY += tileSizeY)
            {
                float toY = Mathf.Min(fromY + tileSizeY, patchRect.Size.Y);
                
                var drawSubRect = new Rect2(patchRect.Position + new Vector2(fromX, fromY),
                    new Vector2(toX - fromX, toY - fromY));
                var sourceSubSize = sourceRect.Size;
                if (!stretchX)
                    sourceSubSize.X = (toX - fromX) * _resolutionScale;
                if (!stretchY)
                    sourceSubSize.Y = (toY - fromY) * _resolutionScale;
                RenderingServer.CanvasItemAddTextureRectRegion(toCanvasItem, drawSubRect, textureRid, ReferenceRectToSourceTextureRect(sourceRect with {Size = sourceSubSize}), _modulateColor);
            }
        }
    }

    private void CollectPatchTileData(float patchLength, float sourceLength, RenderingServer.NinePatchAxisMode axisMode,
        out float tileLength, out bool stretch)
    {
        tileLength = patchLength;
        stretch = false;
        if (axisMode is RenderingServer.NinePatchAxisMode.Stretch)
        {
            tileLength = patchLength;
            stretch = true;
        }
        if (axisMode is RenderingServer.NinePatchAxisMode.Tile or RenderingServer.NinePatchAxisMode.TileFit && !Mathf.IsZeroApprox(sourceLength))
        {
            tileLength = sourceLength / _resolutionScale;
        }
        if (axisMode is RenderingServer.NinePatchAxisMode.TileFit)
        {
            int tileCount = Mathf.FloorToInt(patchLength / sourceLength);
            float tileScale = patchLength / (tileCount * tileLength);
            if (float.IsFinite(tileLength * tileScale))
            {
                tileLength *= tileScale;
            }
            stretch = true;
        }
    }

    private void CollectPatchExtents(Array<int> slicesArr, int i, int n, int referenceLength, out int from,
        out int to, out int patchLength)
    {
        from = i == 0 ? 0 : SliceCoordinate(GetSliceParam(slicesArr, i - 1, 0), referenceLength);
        to = i == n - 1 ? referenceLength : SliceCoordinate(GetSliceParam(slicesArr, i, referenceLength), referenceLength);
        patchLength = to - from;
    }

    private int SliceCoordinate(int offset, int textureLength)
    {
        if (offset < 0) offset += textureLength;
        return offset;
    }

    private void CollectAxisData(Array<int> slicesArr, Array<float> weightsArr, int n, int referenceLength, out float totalWeight,
        out float fixedLength)
    {
        totalWeight = 0;
        fixedLength = 0;
        
        for (var i = 0; i < n; i++)
        {
            CollectPatchExtents(slicesArr, i, n, referenceLength, out int from, out int to, out int patchLength);

            float weight = GetSliceParam(weightsArr, i);
            totalWeight += weight;
            if (weight == 0)
            {
                fixedLength += patchLength;
            }
        }
        if (float.IsFinite(_resolutionScale))
            fixedLength /= _resolutionScale;
    }

    private float SumWeights(Array<float> arr, int n)
    {
        float sum = 0;
        for (int i = 0; i < n; i++)
        {
            GetSliceParam(arr, i, 0f);
        }
        return sum;
    }

    private T GetSliceParam<[MustBeVariant] T>(Array<T> arr, int i, T fallback = default)
    {
        if(arr is not {Count: > 0}) return fallback;
        if (i < 0) return fallback;
        if (i >= arr.Count) return fallback;
        return arr[i];
    }

    private Vector2I GetSourceTextureSize()
    {
        return (Vector2I)(Texture?.GetSize() ?? Vector2.Zero);
    }

    private Vector2I GetReferenceSize()
    {
        var refSize = GetSourceTextureSize();
        if (OverrideTextureSize != default) refSize = OverrideTextureSize;
        if (_subRegion != default) refSize = _subRegion.Size;

        return refSize;
    }

    private Rect2 ReferenceRectToSourceTextureRect(Rect2 refRect)
    {
        var sourceTextureSize = GetSourceTextureSize();

        var sourceRect = refRect;
        sourceRect.Position += _subRegion.Position;

        if (OverrideTextureSize != default)
        {
            var scale = (Vector2)sourceTextureSize / OverrideTextureSize;
            sourceRect.Position *= scale;
            sourceRect.Size *= scale;
        }

        return sourceRect;
    }
}