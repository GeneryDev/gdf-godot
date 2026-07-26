using System;
using GDF.IO;
using GDF.UI;
using GDF.Util;
using Godot;

namespace GDF.Scenes;

public interface ISceneToggler
{
    public Resource SubScene { get; set; }
    
    public bool Loaded { get; set; }
    
    public struct MemoryData
    {
        public Node LoadedSceneNode;
        public bool Dirty;
    }

    public ref MemoryData GetTogglerMemory();
    public bool IsAsync();

    public Node GetRelativeNode(out RelativeModeEnum relativeMode);
    public bool ShouldCopyTogglerTransform();
    public bool ShouldFadeOutScreensOnUnload();

    public void EmitSignalNodeInstantiated(Node node);
    public void EmitSignalNodeLoaded(Node node);
    public void EmitSignalLoadComplete();
    public void EmitSignalUnloadComplete();

    public enum RelativeModeEnum
    {
        AsChild,
        AsSibling
    }
}

public static class SceneTogglerMethods
{
    public static string GetSubScenePath<T>(this T toggler) where T : ISceneToggler
    {
        return toggler.SubScene switch
        {
            PackedScene ps => ps.ResourcePath,
            ResourceReference rr => rr.StoredResourcePath,
            _ => null
        };
    }
    public static PackedScene GetSubScenePacked<T>(this T toggler) where T : ISceneToggler
    {
        return toggler.SubScene switch
        {
            PackedScene ps => ps,
            ResourceReference rr => rr.GetResource<PackedScene>(cacheReference: toggler.IsAsync()),
            _ => null
        };
    }
    public static Node GetSpawnParent<T>(this T toggler) where T : ISceneToggler
    {
        var relativeNode = toggler.GetRelativeNode(out var mode);

        return mode == ISceneToggler.RelativeModeEnum.AsSibling
            ? relativeNode.GetParent() ?? relativeNode
            : relativeNode;
    }
    public static void InsertToRelativeNode<T>(this T toggler, Node node) where T : Node, ISceneToggler
    {
        var relativeNode = toggler.GetRelativeNode(out var mode);

        switch (mode)
        {
            case ISceneToggler.RelativeModeEnum.AsChild:
                relativeNode.AddChild(node);
                break;
            case ISceneToggler.RelativeModeEnum.AsSibling:
                relativeNode.AddSibling(node);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (toggler.ShouldCopyTogglerTransform())
        {
            node.CopyGlobalTransformFrom(toggler);
        }
    }

    public static void ProcessToggler<T>(this T toggler) where T : Node, ISceneToggler
    {
        ref var memory = ref toggler.GetTogglerMemory();
        if (toggler.Loaded && toggler.SubScene != null && memory.LoadedSceneNode == null)
            foreach (var child in toggler.GetSpawnParent().GetChildren())
                if (child.SceneFilePath == toggler.GetSubScenePath())
                {
                    memory.LoadedSceneNode = child;
                    // GD.Print("RESTORED LOADED SCENE NODE");
                    break;
                }

        if (memory.Dirty)
        {
            if (toggler.Loaded)
            {
                if (toggler.IsAsync() && toggler.SubScene is ResourceReference rr)
                {
                    if(rr.GetStatus() != ResourceLoader.ThreadLoadStatus.InProgress)
                        rr.RequestLoad();
                    if (rr.GetStatus() == ResourceLoader.ThreadLoadStatus.Loaded)
                    {
                        toggler.LoadToggler();
                        memory.Dirty = false;
                    }
                }
                else
                {
                    toggler.LoadToggler();
                    memory.Dirty = false;
                }
            }
            else
            {
                toggler.UnloadToggler();
                memory.Dirty = false;
            }
        }
    }

    public static void LoadToggler<T>(this T toggler) where T : Node, ISceneToggler
    {
        ref var memory = ref toggler.GetTogglerMemory();
        
        var subScenePacked = toggler.GetSubScenePacked();
        if (subScenePacked == null) return;
        if (memory.LoadedSceneNode != null && GodotObject.IsInstanceValid(memory.LoadedSceneNode)) return;

        var node = subScenePacked.GdfInstantiate();
        if (node == null) return;
        node.SetMultiplayerAuthority(toggler.GetMultiplayerAuthority());

        {
            // Instantiated
            toggler.EmitSignalNodeInstantiated(node);
        }
        var screen = node as Screen;

        if (screen != null)
            node = screen.ToPlaceholder();

        {
            // Enter tree
            toggler.InsertToRelativeNode(node);

            memory.LoadedSceneNode = node;
        }

        if (!Engine.IsEditorHint())
            node.Owner = toggler.Owner;

        {
            // After entered tree
            screen?.ShowScreen();

            toggler.EmitSignalNodeLoaded(node);
            toggler.EmitSignalLoadComplete();
        }
    }

    public static void UnloadToggler<T>(this T toggler) where T : Node, ISceneToggler
    {
        ref var memory = ref toggler.GetTogglerMemory();

        var loadedSceneNode = memory.LoadedSceneNode;
        if (loadedSceneNode == null) return;
        if (GodotObject.IsInstanceValid(loadedSceneNode))
            switch (loadedSceneNode)
            {
                case Screen screen when toggler.ShouldFadeOutScreensOnUnload():
                    screen.ForceFadeOutScreen();
                    break;
                case ScreenPlaceholder screenPlaceholder when toggler.ShouldFadeOutScreensOnUnload():
                    screenPlaceholder.ForceFadeOutScreen();
                    break;
                default:
                    loadedSceneNode.Free();
                    break;
            }

        memory.LoadedSceneNode = null;
        toggler.EmitSignalUnloadComplete();
    }
}