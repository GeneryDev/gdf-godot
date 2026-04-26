using Godot;
using Godot.Collections;

namespace GDF.UI;

public partial class StyleBoxNPatch
{
    private void ImportSettings(StyleBoxTexture source)
    {
#if TOOLS
        if (source == null) return;

        if (source.Texture == null)
        {
            GD.PrintErr("Cannot import settings from a StyleBoxTexture with no texture.");
            return;
        }

        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Import stylebox settings");
        
        undoRedo.AddDoProperty(this, PropertyName.Texture, source.Texture);
        undoRedo.AddDoProperty(this, PropertyName.OverrideTextureSize, source.Texture.GetSize());
        undoRedo.AddDoProperty(this, PropertyName.SubRegion, source.RegionRect);
        
        CalculateAxisParamsFrom(source.TextureMarginLeft, source.TextureMarginRight,
            source.AxisStretchHorizontal, out int patchCountHorizontal, out var slicesX,
            out var patchWeightsX,
            out var patchAxisModesX);
        CalculateAxisParamsFrom(source.TextureMarginTop, source.TextureMarginBottom,
            source.AxisStretchHorizontal, out int patchCountVertical, out var slicesY,
            out var patchWeightsY,
            out var patchAxisModesY);
        
        undoRedo.AddDoProperty(this, PropertyName.PatchCountHorizontal, patchCountHorizontal);
        undoRedo.AddDoProperty(this, PropertyName.PatchCountVertical, patchCountVertical);
        undoRedo.AddDoProperty(this, PropertyName.SlicesX, slicesX);
        undoRedo.AddDoProperty(this, PropertyName.SlicesY, slicesY);
        undoRedo.AddDoProperty(this, PropertyName.PatchWeightsX, patchWeightsX);
        undoRedo.AddDoProperty(this, PropertyName.PatchWeightsY, patchWeightsY);
        undoRedo.AddDoProperty(this, PropertyName.PatchAxisModesX, patchAxisModesX);
        undoRedo.AddDoProperty(this, PropertyName.PatchAxisModesY, patchAxisModesY);
        undoRedo.AddDoProperty(this, PropertyName.ResolutionScale, 1.0f);
        undoRedo.AddDoProperty(this, StyleBox.PropertyName.ContentMarginBottom, source.GetMargin(Side.Bottom));
        undoRedo.AddDoProperty(this, StyleBox.PropertyName.ContentMarginTop, source.GetMargin(Side.Top));
        undoRedo.AddDoProperty(this, StyleBox.PropertyName.ContentMarginLeft, source.GetMargin(Side.Left));
        undoRedo.AddDoProperty(this, StyleBox.PropertyName.ContentMarginRight, source.GetMargin(Side.Right));
        undoRedo.AddDoProperty(this, PropertyName.ExpandMarginBottom, source.ExpandMarginBottom);
        undoRedo.AddDoProperty(this, PropertyName.ExpandMarginTop, source.ExpandMarginTop);
        undoRedo.AddDoProperty(this, PropertyName.ExpandMarginLeft, source.ExpandMarginLeft);
        undoRedo.AddDoProperty(this, PropertyName.ExpandMarginRight, source.ExpandMarginRight);
        undoRedo.AddDoProperty(this, PropertyName.ModulateColor, source.ModulateColor);
        
        undoRedo.AddUndoProperty(this, PropertyName.Texture, Texture);
        undoRedo.AddUndoProperty(this, PropertyName.OverrideTextureSize, OverrideTextureSize);
        undoRedo.AddUndoProperty(this, PropertyName.SubRegion, SubRegion);
        undoRedo.AddUndoProperty(this, PropertyName.PatchCountHorizontal, PatchCountHorizontal);
        undoRedo.AddUndoProperty(this, PropertyName.PatchCountVertical, PatchCountVertical);
        undoRedo.AddUndoProperty(this, PropertyName.SlicesX, SlicesX);
        undoRedo.AddUndoProperty(this, PropertyName.SlicesY, SlicesY);
        undoRedo.AddUndoProperty(this, PropertyName.PatchWeightsX, PatchWeightsX);
        undoRedo.AddUndoProperty(this, PropertyName.PatchWeightsY, PatchWeightsY);
        undoRedo.AddUndoProperty(this, PropertyName.PatchAxisModesX, PatchAxisModesX);
        undoRedo.AddUndoProperty(this, PropertyName.PatchAxisModesY, PatchAxisModesY);
        undoRedo.AddUndoProperty(this, PropertyName.ResolutionScale, ResolutionScale);
        undoRedo.AddUndoProperty(this, PropertyName.ExpandMarginBottom, ExpandMarginBottom);
        undoRedo.AddUndoProperty(this, PropertyName.ExpandMarginTop, ExpandMarginTop);
        undoRedo.AddUndoProperty(this, PropertyName.ExpandMarginLeft, ExpandMarginLeft);
        undoRedo.AddUndoProperty(this, PropertyName.ExpandMarginRight, ExpandMarginRight);
        undoRedo.AddUndoProperty(this, StyleBox.PropertyName.ContentMarginBottom, ContentMarginBottom);
        undoRedo.AddUndoProperty(this, StyleBox.PropertyName.ContentMarginTop, ContentMarginTop);
        undoRedo.AddUndoProperty(this, StyleBox.PropertyName.ContentMarginLeft, ContentMarginLeft);
        undoRedo.AddUndoProperty(this, StyleBox.PropertyName.ContentMarginRight, ContentMarginRight);
        undoRedo.AddUndoProperty(this, PropertyName.ModulateColor, ModulateColor);
        
        undoRedo.CommitAction();
#endif
    }

    private void CalculateAxisParamsFrom(float textureMarginBegin, float textureMarginEnd,
        StyleBoxTexture.AxisStretchMode axisStretch, out int patchCount, out Array<int> slices,
        out Array<float> patchWeights,
        out Array<RenderingServer.NinePatchAxisMode> patchAxisModes)
    {
        patchCount = 1;
        slices = new Array<int>();
        patchWeights = new Array<float>() { 1.0f };
        patchAxisModes = new Array<RenderingServer.NinePatchAxisMode>()
        {
            (RenderingServer.NinePatchAxisMode)(int)axisStretch
        };
        if (textureMarginBegin != 0)
        {
            patchCount++;
            slices.Add((int)textureMarginBegin);
            patchWeights.Insert(0, 0.0f);
            patchAxisModes.Insert(0, RenderingServer.NinePatchAxisMode.Stretch);
        }

        if (textureMarginEnd != 0)
        {
            patchCount++;
            slices.Add(-(int)textureMarginEnd);
            patchWeights.Add(0.0f);
            patchAxisModes.Add(RenderingServer.NinePatchAxisMode.Stretch);
        }
    }
}