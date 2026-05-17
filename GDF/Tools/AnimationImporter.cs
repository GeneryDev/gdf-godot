using System;
using System.Collections.Generic;
using System.Linq;
using GDF.Editor;
using GDF.Util;
using Godot;

namespace GDF.Tools;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/tool_cog.png")]
public partial class AnimationImporter : Node
{
    private static readonly StringName MetaNameInheritingFromAnimation = "inheriting_from_animation";

    [Export] public bool RetimeOnReimport = true;

#if TOOLS
    [ExportToolButton("New Animation")] // Add icon
    private Callable ButtonNewAnimation => new Callable(this, MethodName.NewAnimation);
    [ExportToolButton("Change Animation Inheritance")] // Reparent icon
    private Callable ButtonChangeAnimationInheritance => new Callable(this, MethodName.ChangeAnimationInheritance);
    [ExportToolButton("Reimport All")] // AssetLib icon
    private Callable ButtonReimportAll => new Callable(this, MethodName.Reimport);
    
    private AnimationTimingTool.Transactions _transactions;

    private void Error(string err)
    {
        GD.PrintErr($"[Animation Importer] {err}");
    }

    public void NewAnimation()
    {
        if (!GetRequiredNodes(out var importedAnimationPlayer, out var workingAnimationPlayer,
                out var gltfNode)) return;

        string saveDir = GltfToAnimationDir(gltfNode.SceneFilePath);

        const string defaultNameFieldValue = "animation_name";

        var dialog = AnimationToolDialogs.NewAnimationWindow(out var nameField, out var inheritOptions,
            out var okButton, out var cancelButton);

        var animations = GetAnimations(importedAnimationPlayer).ToList();
        inheritOptions.AddItem("[none]", -1);
        for (var i = 0; i < animations.Count; i++) inheritOptions.AddItem(animations[i].Key, i);

        nameField.SetMeta("ever_changed", false);

        inheritOptions.ItemSelected += (index) =>
        {
            var id = (int)(index - 1);
            if (id >= 0 && (string.IsNullOrWhiteSpace(nameField.Text) || nameField.Text == defaultNameFieldValue ||
                            !nameField.GetMeta("ever_changed", false).AsBool()))
            {
                nameField.Text = TransformAnimationName(animations[id].Key);
                nameField.SetMeta("ever_changed", false);
                ValidateOkButton();
            }
        };

        nameField.Text = defaultNameFieldValue;
        nameField.TextChanged += (text) =>
        {
            ValidateOkButton();
            nameField.SetMeta("ever_changed", true);
        };
        okButton.Disabled = workingAnimationPlayer.HasAnimation(nameField.Text);

        okButton.Pressed += () =>
        {
            StringName inheritedAnimationName = default;
            Animation inheritedAnimation = null;
            int inheritedAnimationId = inheritOptions.Selected - 1;
            if (inheritedAnimationId >= 0)
            {
                inheritedAnimationName = animations[inheritedAnimationId].Key;
                inheritedAnimation = animations[inheritedAnimationId].Value;
            }

            NewAnimation(nameField.Text, workingAnimationPlayer, inheritedAnimationName, inheritedAnimation, saveDir);
            dialog.QueueFree();
        };
        cancelButton.Pressed += () => dialog.QueueFree();

        ValidateOkButton();

        EditorInterface.Singleton.PopupDialogCentered(dialog);
        nameField.GrabFocus();

        void ValidateOkButton()
        {
            okButton.Disabled = string.IsNullOrWhiteSpace(nameField.Text) ||
                                workingAnimationPlayer.HasAnimation(nameField.Text);
        }
    }

    private void NewAnimation(StringName name, AnimationPlayer workingAnimationPlayer, StringName inheritingFromName,
        Animation inheritedAnimation, string saveDir)
    {
        if (!GetWorkingLibrary(workingAnimationPlayer, out var workingLibrary, ref name)) return;

        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        var newAnim = new Animation();
        if (!inheritingFromName.IsEmpty) newAnim.SetMeta(MetaNameInheritingFromAnimation, inheritingFromName);

        var actionName = $"Create animation '{name}'";
        undoRedo.CreateAction(actionName, UndoRedo.MergeMode.All, backwardUndoOps: true);
        undoRedo.AddDoMethod(workingLibrary, AnimationLibrary.MethodName.AddAnimation, name, newAnim);
        undoRedo.AddUndoMethod(workingLibrary, AnimationLibrary.MethodName.RemoveAnimation, name);
        undoRedo.AddDoReference(newAnim);
        undoRedo.CommitAction();

        ImportAnimation(newAnim, inheritedAnimation, workingAnimationPlayer, name, actionName);

        EditAnimation(workingAnimationPlayer, name);

        GD.Print($"Created new animation '{name}' inheriting from imported animation '{inheritingFromName}'");

        if (string.IsNullOrEmpty(workingLibrary.ResourcePath) || workingLibrary.ResourcePath.Contains("::"))
        {
            // library is built-in
            DirAccess.MakeDirRecursiveAbsolute(saveDir);
            newAnim.TakeOverPath(saveDir + $"/{name}.tres");
        }
        else
        {
            // library is saved to file - don't make a separate file for this animation
        }
    }

    public void ChangeAnimationInheritance()
    {
        if (!GetRequiredNodes(out var importedAnimationPlayer, out var workingAnimationPlayer, out _)) return;

        var dialog = AnimationToolDialogs.ChangeAnimationInheritanceWindow(out var workingOptions,
            out var inheritOptions, out var okButton, out var cancelButton);

        var workingAnimations = GetAnimations(workingAnimationPlayer).ToList();
        workingOptions.AddItem("[select]", -1);
        for (var i = 0; i < workingAnimations.Count; i++) workingOptions.AddItem(workingAnimations[i].Key, i);

        var importedAnimations = GetAnimations(importedAnimationPlayer).ToList();
        inheritOptions.AddItem("[none]", -1);
        for (var i = 0; i < importedAnimations.Count; i++) inheritOptions.AddItem(importedAnimations[i].Key, i);

        workingOptions.ItemSelected += (index) =>
        {
            var id = (int)(index - 1);
            if (id >= 0)
            {
                var anim = workingAnimations[id].Value;
                string inheritingFromAnimName = anim.GetMeta(MetaNameInheritingFromAnimation, "").AsStringName();
                if (inheritingFromAnimName.Length == 0)
                    inheritOptions.Selected = 0;
                else
                    inheritOptions.Selected =
                        importedAnimations.FindIndex((kv) => kv.Key == inheritingFromAnimName) + 1;
            }

            ValidateButtons();
        };

        okButton.Pressed += () =>
        {
            int workingAnimationId = workingOptions.Selected - 1;
            var workingAnimationName = workingAnimations[workingAnimationId].Key;
            var workingAnimation = workingAnimations[workingAnimationId].Value;

            StringName inheritedAnimationName = default;
            Animation inheritedAnimation = default;
            int inheritedAnimationId = inheritOptions.Selected - 1;
            if (inheritedAnimationId >= 0)
            {
                inheritedAnimationName = importedAnimations[inheritedAnimationId].Key;
                inheritedAnimation = importedAnimations[inheritedAnimationId].Value;
            }

            var actionName = $"Change animation inheritance for '{workingAnimationName}'";
            SetAnimationInheritance(workingAnimation, workingAnimationName, inheritedAnimationName, actionName);
            if (inheritedAnimation != null)
                ImportAnimation(workingAnimation, inheritedAnimation, workingAnimationPlayer, workingAnimationName,
                    actionName);

            dialog.QueueFree();

            EditAnimation(workingAnimationPlayer, workingAnimationName);
        };
        cancelButton.Pressed += () => dialog.QueueFree();

        ValidateButtons();

        EditorInterface.Singleton.PopupDialogCentered(dialog);

        void ValidateButtons()
        {
            inheritOptions.Disabled = okButton.Disabled = workingOptions.Selected <= 0;
        }
    }

    private void EditAnimation(AnimationMixer mixer, StringName name)
    {
        EditorInterface.Singleton.EditNode(mixer);
        var tween = CreateTween();
        tween.TweenInterval(0.1f);
        tween.TweenCallback(Callable.From(() =>
        {
            if (AnimationEditorUtil.GetAnimationPlayerEditor() is { } animEditor) animEditor.CurrentAnimationName = name;
        }));
    }

    private string GltfToAnimationDir(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        int lastSlashIndex = path.LastIndexOf('/');
        if (lastSlashIndex < 0) return null;
        return path[0..lastSlashIndex] + "/animations";
    }

    private string TransformAnimationName(string name)
    {
        return name.ToLowerInvariant().Replace(' ', '_').Replace('-', '_').Replace('\t', ' ');
    }

    private bool GetWorkingLibrary(AnimationPlayer mixer, out AnimationLibrary library, ref StringName animName)
    {
        var rawAnimName = (string)animName;
        var libraryName = "";
        if (rawAnimName?.IndexOf('/') is { } slashIndex && slashIndex != -1)
        {
            libraryName = rawAnimName.Substring(0, slashIndex);
            rawAnimName = rawAnimName.Substring(slashIndex + 1);
            animName = rawAnimName;
        }

        if (!mixer.HasAnimationLibrary(libraryName)) mixer.AddAnimationLibrary(libraryName, new AnimationLibrary());
        library = mixer.GetAnimationLibrary(libraryName);
        return true;
    }

    public void Reimport()
    {
        if (!GetRequiredNodes(out var importedAnimationPlayer, out var workingAnimationPlayer, out _)) return;

        Dictionary<Animation, List<ValueTuple<StringName, float>>> skippedDueToMismatchingLengths = new();

        foreach (var (animName, anim) in GetAnimations(workingAnimationPlayer))
        {
            string inheritingFromAnimName = anim.GetMeta(MetaNameInheritingFromAnimation, "").AsStringName();
            if (inheritingFromAnimName.Length == 0) continue;

            if (!importedAnimationPlayer.HasAnimation(inheritingFromAnimName))
            {
                Error(
                    $"Animation '{animName}' is configured to inherit from imported animation '{inheritingFromAnimName}', but no such animation exists in the imported animation player. Skipping.");
                continue;
            }

            var parentAnimation = importedAnimationPlayer.GetAnimation(inheritingFromAnimName);
            if (parentAnimation == anim)
            {
                Error($"Animation '{animName}' is configured to inherit from itself. Skipping.");
                continue;
            }

            ImportAnimation(anim, parentAnimation, workingAnimationPlayer, animName,
                skippedDueToMismatchingLengths: skippedDueToMismatchingLengths);
        }

        if (skippedDueToMismatchingLengths.Count > 0)
        {
            Error(
                "Detected mismatching animation lengths. This is usually caused by a source animation in the .gltf having its timing changed after already having been re-timed in Godot.");
            Error("The animations that changed are as follows:");
            foreach (var (parentAnimation, dependentAnimations) in skippedDueToMismatchingLengths)
            {
                Error($"\t{parentAnimation.ResourceName}. New length: {parentAnimation.Length}");
                foreach ((var dependentAnimName, float expectedLength) in dependentAnimations)
                    Error($"\t\tUsed by '{dependentAnimName}', which expected length {expectedLength}");
            }

            Error(
                "These animations were skipped. To fix, please remove or readjust the Original Control Points timing track to match the new animation length");
        }
    }

    private void ImportAnimation(Animation anim, Animation parentAnimation, AnimationMixer mixer, StringName animName,
        string actionName = null,
        Dictionary<Animation, List<ValueTuple<StringName, float>>> skippedDueToMismatchingLengths = null)
    {
        if (anim == parentAnimation)
        {
            Error($"Animation '{animName}' is configured to inherit from itself. Cannot reimport.");
            return;
        }

        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();

        var timingTool = GetParent().GetChildOfType<AnimationTimingTool>();
        if (RetimeOnReimport && timingTool?.GetOriginalRetimedAnimationLength(anim, mixer) is { } expectedLength &&
            Mathf.Abs(expectedLength - parentAnimation.Length) > 0.01f)
        {
            if (skippedDueToMismatchingLengths != null)
            {
                if (skippedDueToMismatchingLengths.TryGetValue(parentAnimation, out var existingList))
                    existingList.Add((animName, expectedLength));
                else
                    skippedDueToMismatchingLengths[parentAnimation] = new List<ValueTuple<StringName, float>>()
                        { (animName, expectedLength) };
            }
            else
            {
                Error(
                    $"Animation '{animName}' was expecting an animation of length {expectedLength}, but the new '{parentAnimation.ResourceName}' is now {parentAnimation.Length}.\nSkipping. Please remove or readjust the Original Control Points timing track to match the new animation length");
            }

            return;
        }


        actionName ??= $"Reimport animation '{animName}'";
        _transactions = new AnimationTimingTool.Transactions(undoRedo, anim, actionName);

        // Remove imported tracks
        for (int trackIndex = anim.GetTrackCount() - 1; trackIndex >= 0; trackIndex--)
        {
            if (!anim.TrackIsImported(trackIndex)) continue;
            _transactions.RemoveTrack(trackIndex);
        }

        // Add all tracks from parent animation
        for (int parentTrackIndex = parentAnimation.GetTrackCount() - 1; parentTrackIndex >= 0; parentTrackIndex--)
        {
            var trackState = TrackState.CreateFrom(parentAnimation, parentTrackIndex);
            var trackPath = parentAnimation.TrackGetPath(parentTrackIndex);

            if (anim.FindTrack(trackPath, trackState.TrackType) != -1)
                continue; // There already is a non-imported track for this track

            _transactions.AddTrack(trackState.TrackType, 0, trackPath);
            _transactions.UpdateTrack(0, trackState);

            _transactions.CopyTrackKeys(0, parentAnimation, parentTrackIndex);
        }

        // Other animation data
        undoRedo.CreateAction(actionName, UndoRedo.MergeMode.All, backwardUndoOps: true);
        undoRedo.AddDoProperty(anim, Animation.PropertyName.Length, parentAnimation.Length);
        undoRedo.AddDoProperty(anim, Animation.PropertyName.LoopMode, Variant.From(parentAnimation.LoopMode));
        undoRedo.AddUndoProperty(anim, Animation.PropertyName.Length, anim.Length);
        undoRedo.AddUndoProperty(anim, Animation.PropertyName.LoopMode, Variant.From(anim.LoopMode));
        undoRedo.CommitAction();

        if (RetimeOnReimport && timingTool != null) timingTool.UpdateTiming(anim, mixer);
    }

    private void SetAnimationInheritance(Animation anim, StringName animName, StringName inheritName,
        string actionName = null)
    {
        var oldMeta = anim.GetMeta(MetaNameInheritingFromAnimation, "").AsStringName();

        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        actionName ??= $"Set animation inheritance for '{animName}'";
        undoRedo.CreateAction(actionName, UndoRedo.MergeMode.All, backwardUndoOps: true);

        if (inheritName is { IsEmpty: false })
            undoRedo.AddDoMethod(anim, GodotObject.MethodName.SetMeta, MetaNameInheritingFromAnimation, inheritName);
        else
            undoRedo.AddDoMethod(anim, GodotObject.MethodName.RemoveMeta, MetaNameInheritingFromAnimation);

        if (oldMeta is { IsEmpty: false })
            undoRedo.AddUndoMethod(anim, GodotObject.MethodName.SetMeta, MetaNameInheritingFromAnimation, oldMeta);
        else
            undoRedo.AddUndoMethod(anim, GodotObject.MethodName.RemoveMeta, MetaNameInheritingFromAnimation);
        undoRedo.CommitAction();
    }

    private IEnumerable<KeyValuePair<StringName, Animation>> GetAnimations(AnimationMixer mixer)
    {
        foreach (var libraryName in mixer.GetAnimationLibraryList())
        {
            var library = mixer.GetAnimationLibrary(libraryName);
            foreach (var animName in library.GetAnimationList())
            {
                var fullAnimName = animName;
                if (!libraryName.IsNullOrEmpty()) fullAnimName = libraryName + "/" + animName;
                var anim = library.GetAnimation(animName);
                yield return new KeyValuePair<StringName, Animation>(fullAnimName, anim);
            }
        }
    }

    public static void CollectModelNodes(Node node, out Node gltfNode, out Skeleton3D skeleton)
    {
        var gltfNodes = new List<Node>();
        var skeletons = new List<Skeleton3D>();
        CollectModelNodes(node, gltfNodes, skeletons);
        if (gltfNodes.Count == 1) gltfNode = gltfNodes[0];
        else
        {
            gltfNode = null;
            if(gltfNodes.Count > 1)
                GD.PushError("More than one gltf node found");
        }

        skeleton = null;
        foreach (var sk in skeletons)
        {
            if (gltfNode?.IsAncestorOf(sk) ?? false)
            {
                if (skeleton == null)
                {
                    skeleton = sk;
                }
                else
                {
                    GD.PushError("More than one skeleton node found inside gltf imported node");
                }
            }
        }
    }
    
    private static void CollectModelNodes(Node node, List<Node> gltfImportedNodes, List<Skeleton3D> skeletons)
    {
        if(node is Skeleton3D sk) skeletons.Add(sk);

        if (node.SceneFilePath.EndsWith(".gltf"))
        {
            gltfImportedNodes.Add(node);
        }
        
        foreach(var child in node.GetChildren()) CollectModelNodes(child, gltfImportedNodes, skeletons);
    }

    private bool GetRequiredNodes(out AnimationPlayer importedAnimationPlayer,
        out AnimationPlayer workingAnimationMixer, out Node gltfNode)
    {
        importedAnimationPlayer = null;
        workingAnimationMixer = null;
        gltfNode = null;

        var sceneRoot = Owner;
        if (sceneRoot == null)
        {
            Error("This animation importer is not inside a scene?");
            return false;
        }

        workingAnimationMixer = sceneRoot.GetChildOfType<AnimationPlayer>();
        if (workingAnimationMixer == null)
        {
            Error("Missing animation player under the scene root");
            return false;
        }

        CollectModelNodes(sceneRoot, out gltfNode, out _);

        if (gltfNode == null)
        {
            Error("No gltf-imported node in scene");
            return false;
        }

        importedAnimationPlayer = gltfNode.GetChildOfType<AnimationPlayer>();

        if (importedAnimationPlayer == null)
        {
            Error("The gltf-imported scene does not have an animation player");
            return false;
        }

        return true;
    }

    // public override void _ValidateProperty(Dictionary property)
    // {
    //     base._ValidateProperty(property);
    //     property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Editor);
    // }
#endif
}