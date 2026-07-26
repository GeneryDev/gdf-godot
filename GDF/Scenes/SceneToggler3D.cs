using GDF.IO;
using Godot;

namespace GDF.Scenes;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/scene_toggler_3d.svg")]
public partial class SceneToggler3D : Node3D, ISceneToggler
{
    [Signal]
    public delegate void NodeInstantiatedEventHandler(Node node);
    [Signal]
    public delegate void NodeLoadedEventHandler(Node node);

    [Signal]
    public delegate void LoadCompleteEventHandler();
    [Signal]
    public delegate void UnloadCompleteEventHandler();

    [Export(PropertyHint.ResourceType, $"{nameof(PackedScene)},{nameof(ResourceReference)}")]
    public Resource SubScene { get; set; }

    [Export]
    public bool Loaded
    {
        get => _loaded;
        set
        {
            if (value == _loaded) return;
            _loaded = value;
            _memory.Dirty = true;
        }
    }

    [ExportGroup("Load Settings")]
    [Export] public bool Async = false;
    
    [Export]
    public NodePath RelativeToNode = ".";

    [Export(PropertyHint.Enum, "As Child,As Sibling")]
    public ISceneToggler.RelativeModeEnum RelativeMode = ISceneToggler.RelativeModeEnum.AsChild;

    [Export] public bool UseTogglerTransform = true;

    [ExportGroup("Unload Settings")]
    [Export]
    public bool FadeOutScreens = false;

    private bool _loaded;
    private ISceneToggler.MemoryData _memory;

    public override void _Process(double delta)
    {
        this.ProcessToggler();
    }

    private void Load()
    {
        this.LoadToggler();
    }

    private void Unload()
    {
        this.UnloadToggler();
    }

    public void Reload()
    {
        Unload();
        Load();
    }

    public void SetLoaded(bool loaded)
    {
        Loaded = loaded;
    }

    bool ISceneToggler.IsAsync()
    {
        return Async /* && !SceneManager.CheckAsyncDisabled(this)*/;
    }

    ref ISceneToggler.MemoryData ISceneToggler.GetTogglerMemory()
    {
        return ref _memory;
    }

    Node ISceneToggler.GetRelativeNode(out ISceneToggler.RelativeModeEnum relativeMode)
    {
        relativeMode = RelativeMode;
        return GetNode(RelativeToNode) ?? this;
    }

    bool ISceneToggler.ShouldCopyTogglerTransform()
    {
        return UseTogglerTransform;
    }

    bool ISceneToggler.ShouldFadeOutScreensOnUnload()
    {
        return FadeOutScreens;
    }

    void ISceneToggler.EmitSignalNodeInstantiated(Node node)
    {
        EmitSignalNodeInstantiated(node);
    }

    void ISceneToggler.EmitSignalNodeLoaded(Node node)
    {
        EmitSignalNodeLoaded(node);
    }

    void ISceneToggler.EmitSignalLoadComplete()
    {
        EmitSignalLoadComplete();
    }

    void ISceneToggler.EmitSignalUnloadComplete()
    {
        EmitSignalUnloadComplete();
    }
}