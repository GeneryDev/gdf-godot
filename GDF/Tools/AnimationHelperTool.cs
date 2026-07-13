#if TOOLS
#endif
using GDF.Editor;
using GDF.Util;
using Godot;

namespace GDF.Tools;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/tool_wrench.png")]
public partial class AnimationHelperTool : Node
{
    [Export] public AnimationPlayer Player;
    [Export] public AnimationTree Tree;

    [Export] public bool DisableTreeWhileOnPlayer = false;

    [ExportCategory("Root Motion")]
#if TOOLS
    [ExportToolButton("Set Root Motion Track Path")] private Callable ButtonSetRootMotionTrackPath => new Callable(this, MethodName.SetRootMotionTrackPath);
#endif
    [Export]
    public string RootTrackPath;

#if TOOLS
    
    public void SetRootMotionTrackPath()
    {
        ClearOutput();
        Player?.SetRootMotionTrack(RootTrackPath);
        Tree?.SetRootMotionTrack(RootTrackPath);
    }

    [ExportCategory("Animation Data Editor")]
    [Export]
    public string TrackPath;

    [ExportToolButton("Get Animation Info")] private Callable ButtonGetAnimationInfo => new Callable(this, MethodName.GetAnimationInfo);
    [ExportToolButton("Get Track Info")] private Callable ButtonGetTrackInfo => new Callable(this, MethodName.GetTrackInfo);
    [ExportToolButton("Show Track Browser")] private Callable ButtonShowTrackBrowser => new Callable(this, MethodName.ShowTrackBrowser);
    [Export(PropertyHint.MultilineText)] public string Output;

    public void GetAnimationInfo()
    {
        ClearOutput();
        var anim = GetCurrentAnimation(out string nameInLibrary);
        if (anim == null) return;

        PushOutput($"Animation name: {anim.ResourceName} [{nameInLibrary}]");
        PushOutput($"Length: {anim.Length}s");

        for (var trackIdx = 0; trackIdx < anim.GetTrackCount(); trackIdx++)
        {
            var track = anim.GetTrack(trackIdx);
            PushOutput($"    [{track.Type} Track] {track.Path}");
            PushOutput($"        Imported: {track.IsImported}");
            PushOutput($"        Compressed: {track.IsCompressed}");
        }

        PushOutput("Metadata:");
        var anyMeta = false;
        foreach (var key in anim.GetMetaList())
        {
            PushOutput($"    [{key}] {anim.GetMeta(key)}");
            anyMeta = true;
        }

        if (!anyMeta) PushOutput("    None");
    }

    public void GetTrackInfo()
    {
        ClearOutput();
        var anim = GetCurrentAnimation(out string nameInLibrary);
        if (anim == null) return;

        PushOutput($"Animation name: {anim.ResourceName} [{nameInLibrary}]");

        var foundAny = false;
        for (var trackIdx = 0; trackIdx < anim.GetTrackCount(); trackIdx++)
        {
            var track = anim.GetTrack(trackIdx);
            if (track.Path != TrackPath) continue;
            foundAny = true;
            PushOutput($"    [{track.Type} Track] {track.Path}");
            PushOutput($"        Imported: {track.IsImported}");
        }

        if (!foundAny) PushOutput($"No track found for the given path '{TrackPath}'");
    }

    public void ShowTrackBrowser()
    {
        var anim = GetCurrentAnimation(out string nameInLibrary);
        if (anim == null) return;

        var dialog = AnimationToolDialogs.TrackBrowserWindow(out var searchBar,
            out var tree, out var propertyContainer, out var importedBox, out var closeButton);
        dialog.Title = $"Track Browser ({nameInLibrary})";

        void PopulateTree(string filter)
        {
            int selectedIndex = tree.GetSelected()?.GetMetadata(0).AsInt32() ?? -1;

            tree.Clear();
            var root = tree.CreateItem();
            root.SetText(0, "Tracks");

            for (var trackIndex = 0; trackIndex < anim.GetTrackCount(); trackIndex++)
            {
                var track = anim.GetTrack(trackIndex);
                var path = track.Path;

                if (!string.IsNullOrEmpty(filter) &&
                    !path.ToString().ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                    continue;

                var item = tree.CreateItem(root);
                item.SetIcon(0, GetIconForTrackType(track.Type));
                item.SetText(0, $"{track.Path}");
                item.SetMetadata(0, trackIndex);
                if (selectedIndex == trackIndex) item.Select(0);
            }

            UpdatePropertyDrawer();
        }

        PopulateTree("");

        searchBar.TextChanged += PopulateTree;
        tree.ItemSelected += UpdatePropertyDrawer;
        importedBox.Toggled += ImportedBoxToggled;

        void UpdatePropertyDrawer()
        {
            int selected = tree.GetSelected()?.GetMetadata(0).AsInt32() ?? -1;
            if (selected >= 0) importedBox.ButtonPressed = anim.TrackIsImported(selected);
            propertyContainer.Visible = selected >= 0;
        }

        void ImportedBoxToggled(bool value)
        {
            int selected = tree.GetSelected()?.GetMetadata(0).AsInt32() ?? -1;
            if (selected < 0) return;

            bool prevValue = anim.TrackIsImported(selected);
            if (prevValue == value) return;

            var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
            undoRedo.CreateAction("Set track imported");
            undoRedo.AddDoMethod(anim, Animation.MethodName.TrackSetImported, selected, value);
            undoRedo.AddUndoMethod(anim, Animation.MethodName.TrackSetImported, selected, !value);
            undoRedo.CommitAction();
        }

        closeButton.Pressed += () => dialog.QueueFree();

        ValidateButtons();

        EditorInterface.Singleton.PopupDialogCentered(dialog);

        void ValidateButtons()
        {
            // inheritOptions.Disabled = okButton.Disabled = workingOptions.Selected <= 0;
        }
    }

    private Texture2D GetIconForTrackType(Animation.TrackType trackType)
    {
        return EditorInterface.Singleton.GetEditorTheme().GetIcon(trackType switch
        {
            Animation.TrackType.Value => "KeyValue",
            Animation.TrackType.Position3D => "KeyTrackPosition",
            Animation.TrackType.Rotation3D => "KeyTrackRotation",
            Animation.TrackType.Scale3D => "KeyTrackScale",
            Animation.TrackType.BlendShape => "KeyTrackBlendShape",
            Animation.TrackType.Method => "KeyCall",
            Animation.TrackType.Bezier => "KeyBezier",
            Animation.TrackType.Audio => "KeyAudio",
            Animation.TrackType.Animation => "KeyAnimation",
            _ => "KeyValue"
        }, "EditorIcons");
    }

    private Animation GetCurrentAnimation(out string nameInLibrary)
    {
        nameInLibrary = null;
        var editor = AnimationEditorUtil.GetAnimationPlayerEditor();
        var mixer = AnimationEditorUtil.GetEditingAnimationMixer();
        if (editor == null || mixer == null)
        {
            PushOutput("No animation mixer selected, or GDF plugin is not working correctly");
            return null;
        }

        nameInLibrary = editor.CurrentAnimationName;
        if (string.IsNullOrEmpty(nameInLibrary))
        {
            PushOutput("No animation selected");
            return null;
        }

        var anim = mixer.HasAnimation(nameInLibrary) ? mixer.GetAnimation(nameInLibrary) : null;
        if (anim == null)
        {
            PushOutput($"Selected animation {nameInLibrary} not in mixer? Mixer is out of date?");
            return null;
        }

        return anim;
    }

    private void ClearOutput()
    {
        Output = "";
        PushOutput($"[Timestamp: {Time.GetTimeStringFromSystem()}]");
    }

    private void PushOutput(string msg)
    {
        if (Output.Length > 0) Output += "\n";
        Output += msg;
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;
        if (DisableTreeWhileOnPlayer && Player != null && Tree != null)
        {
            var editingMixer = AnimationEditorUtil.GetEditingAnimationMixer();
            Tree.Active = editingMixer != Player;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationEditorPreSave)
        {
            Output = "";
            if (DisableTreeWhileOnPlayer && Tree != null) Tree.Active = true;
        }
    }
#endif
}