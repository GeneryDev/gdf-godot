using GDF.Util;
using Godot;

#if TOOLS
namespace GDF.Animations;

[Tool]
public partial class AnimationInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object)
    {
        return GdfAnimationNodeMetadata.Supports(@object) || GdfAnimationTransitionMetadata.Supports(@object);
    }

    private Control CreateButtonForMethod(string label, System.Action callable, bool wide = false)
    {
        var button = new Button();
        
        button.Text = label;
        button.Pressed += callable;
        
        return button;
    }

    private void Unfold(Button button)
    {
        if (button == null) return;
        if (!button.ButtonPressed) button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    public override void _ParseBegin(GodotObject @object)
    {
        if (GdfAnimationNodeMetadata.Supports(@object))
        {
            if (!@object.HasMeta(GdfAnimationTree.MetaNameMetadata))
            {
                AddCustomControl(
                    CreateButtonForMethod("Add GDF Metadata", () => AddMetadata<GdfAnimationNodeMetadata>(@object),
                        true)
                );
            }
            else
            {
                AddMetadataInspector<GdfAnimationNodeMetadata>(@object);
                
                AddCustomControl(
                    CreateButtonForMethod("Remove GDF Metadata", () => RemoveMetadata(@object),
                        true)
                );
            }
        }
        if (GdfAnimationTransitionMetadata.Supports(@object))
        {
            if (!@object.HasMeta(GdfAnimationTree.MetaNameMetadata))
            {
                AddCustomControl(
                    CreateButtonForMethod("Add GDF Metadata", () => AddMetadata<GdfAnimationTransitionMetadata>(@object),
                        true)
                );
            }
            else
            {
                AddMetadataInspector<GdfAnimationTransitionMetadata>(@object);

                AddCustomControl(
                    CreateButtonForMethod("Remove GDF Metadata", () => RemoveMetadata(@object),
                        true)
                );
            }
        }
    }

    private void AddMetadataInspector<T>(GodotObject obj)
    {
        var editor = EditorInspector.InstantiatePropertyEditor(obj, Variant.Type.Object, $"metadata/{GdfAnimationTree.MetaNameMetadata}", PropertyHint.ResourceType, typeof(T).Name, usage: (uint)PropertyUsageFlags.Default, wide: true);
        editor.UseFolding = true;
        editor.DrawBackground = false;
        AddPropertyEditor($"metadata/{GdfAnimationTree.MetaNameMetadata}", editor, false, "GDF Metadata");
#if GODOT4_6_OR_GREATER
        editor.GetChildOfType<EditorResourcePicker>()?.SetModulate(new Color(1,1,1,0));
        if (editor.GetChildOfType<EditorResourcePicker>()?.GetChild(1) is Button button)
        {
            CallDeferred(MethodName.Unfold, button);
        }
#else
        (editor.GetChild(0) as Control)?.SetModulate(new Color(1,1,1,0));
        if (editor.GetChild(0)?.GetChild(0) is Button button)
        {
            CallDeferred(MethodName.Unfold, button);
        }
#endif
    }

    private void AddMetadata<[MustBeVariant]T>(GodotObject obj) where T : GodotObject, new()
    {
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Attach GDF Metadata to AnimationNode", customContext: obj);
        var newMeta = new T();
        if (newMeta is GdfAnimationNodeMetadata meta)
        {
            meta.AnimationNodeClassName = obj.GetClass();
        }
        undoRedo.AddDoMethod(obj, GodotObject.MethodName.SetMeta, GdfAnimationTree.MetaNameMetadata, newMeta);
        undoRedo.AddUndoMethod(obj, GodotObject.MethodName.RemoveMeta, GdfAnimationTree.MetaNameMetadata);
        undoRedo.AddDoReference(newMeta);
        undoRedo.CommitAction();
    }

    private void RemoveMetadata(GodotObject obj)
    {
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Remove GDF Metadata from AnimationNode", customContext: obj);
        var oldMeta = obj.GetMeta(GdfAnimationTree.MetaNameMetadata).AsGodotObject();
        undoRedo.AddDoMethod(obj, GodotObject.MethodName.RemoveMeta, GdfAnimationTree.MetaNameMetadata);
        undoRedo.AddUndoMethod(obj, GodotObject.MethodName.SetMeta, GdfAnimationTree.MetaNameMetadata, oldMeta);
        undoRedo.AddUndoReference(oldMeta);
        undoRedo.CommitAction();
    }
}
#endif