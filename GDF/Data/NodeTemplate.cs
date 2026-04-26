using GDF.IO;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/node_template.png")]
public partial class NodeTemplate : Node
{
    [Export(PropertyHint.ResourceType,$"{nameof(PackedScene)},{nameof(ResourceReference)}")] public Resource TemplateScene;

    public NodeTemplateTask<Node> New()
    {
        return New<Node>();
    }

    public NodeTemplateTask<T> New<T>() where T : Node
    {
        var packed = TemplateScene switch
        {
            PackedScene p => p,
            ResourceReference r => r.GetResource<PackedScene>(),
            _ => null
        };
        var instance = packed?.GdfInstantiate<T>();
        instance?.SetMultiplayerAuthority(GetMultiplayerAuthority());
        return new NodeTemplateTask<T>(instance, GetParent());
    }


    // EDITOR ONLY STUFF: Previews
#if TOOLS
    private static readonly StringName GroupNamePreview = "_node_template_preview";
    private static readonly StringName MetaNamePreview = "_node_template_preview";

    [Export(PropertyHint.Range, "0,16,1")]
    public int PreviewCount
    {
        get => _previewCount;
        set
        {
            _previewCount = value;
            if (Engine.IsEditorHint())
                UpdatePreviews();
        }
    }

    [Export] public NodePath PreviewContainer = "..";

    [Export] public bool PreviewOnlyInEditedScene = false;

    private int _previewCount = 0;
    private Array<Node> _previews;

    public override void _Notification(int what)
    {
        if (!Engine.IsEditorHint()) return;
        if (what == NotificationEnterTree) CallDeferred(MethodName.UpdatePreviews);
        if (what == NotificationExitTree || what == NotificationUnparented) ClearPreviews();
        if (what == NotificationEditorPreSave) ClearPreviews();
        if (what == NotificationEditorPostSave) CallDeferred(MethodName.UpdatePreviews);
    }

    private void ClearPreviews()
    {
        if (_previews == null) return;
        foreach (var preview in _previews)
            if (IsInstanceValid(preview))
                preview.QueueFree();
        _previews.Clear();
    }

    private void AddPreview()
    {
        if (GetParent() is not { } parent) return;
        if (PreviewOnlyInEditedScene && EditorInterface.Singleton.GetEditedSceneRoot() != this.Owner) return;
        var previewContainer = this.GetNodeOrNull(PreviewContainer);
        var preview = New().Insert(previewContainer, setOwner: false);
        if (preview == null) return;
        if (parent != Owner)
            preview.Owner = parent;
        preview.AddToGroup(GroupNamePreview);
        preview.SetMeta(MetaNamePreview, GetInstanceId());
        _previews ??= new Array<Node>();
        _previews.Add(preview);
    }

    private void PopPreview()
    {
        if (_previews?.Count > 0)
        {
            var preview = _previews[0];
            _previews.RemoveAt(0);
            if (IsInstanceValid(preview)) preview.QueueFree();
        }
    }

    private void FindPreviews()
    {
        _previews = new Array<Node>();
        if (!IsInsideTree()) return;
        foreach (var node in GetTree().GetNodesInGroup(GroupNamePreview))
            if (node.GetMeta(MetaNamePreview).AsUInt64() == GetInstanceId())
                _previews.Add(node);
    }

    private void UpdatePreviews()
    {
        if (_previews == null) FindPreviews();
        int toAdd = PreviewCount - (_previews?.Count ?? 0);
        while (toAdd > 0)
        {
            toAdd--;
            AddPreview();
        }

        while (toAdd < 0)
        {
            toAdd++;
            PopPreview();
        }
    }
#endif
}

public readonly ref struct NodeTemplateTask<T> where T : Node
{
    public readonly T Instance;
    public readonly Node Container;

    public NodeTemplateTask(T instance, Node container)
    {
        Instance = instance;
        Container = container;
    }

    public NodeTemplateTask<T> SetMeta(StringName name, Variant value)
    {
        Instance?.SetMeta(name, value);
        return this;
    }

    public NodeTemplateTask<T> SetName(StringName name)
    {
        Instance?.SetName(name);
        return this;
    }

    public TChild GetChild<TChild>()
    {
        return Instance != null ? Instance.GetChildOfType<TChild>() : default;
    }

    public T Insert(Node container = null, bool setOwner = true)
    {
        container ??= Container;
        if (Instance != null && container != null)
        {
            container.AddChild(Instance);
            if (setOwner)
                Instance.Owner = container.Owner;
        }

        return Instance;
    }
}