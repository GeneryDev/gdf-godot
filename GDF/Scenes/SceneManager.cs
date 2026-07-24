using System;
using GDF.IO;
using GDF.Multiplayer;
using GDF.Scenes.Transitions;
using GDF.Util;
using Godot;

namespace GDF.Scenes;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class SceneManager : SingletonNode<SceneManager>
{
    public static readonly StringName GroupNameAsyncDisabled = "async_disabled";
    
    public const bool MimicSyncedTransitionDelayOffline = true;
    public static SceneLoadingStrategy LoadingStrategy = SceneLoadingStrategy.AsynchronousMultipleCollection;
    
    [Signal]
    public delegate void SceneChangedEventHandler(Resource request);
    [Signal]
    public delegate void TransitionStatusChangedEventHandler();

    public static PeerAwaiter SceneReadyAwaiter;
    public static SceneChangeRequest LastLevelChangeRequest { get; private set; }
    
    [Export] public ScreenTransitionReference DefaultScreenTransition;

    public Node CurrentScene { get; private set; }

    public TransitionStatusEnum TransitionStatus
    {
        get => _transitionStatus;
        set
        {
            _transitionStatus = value;
            _activeTransition?.SetTransitionStatus(value.ToString().ToSnakeCase());
            EmitSignalTransitionStatusChanged();
        }
    }

    private ScreenTransition _activeTransition;
    private SceneChangeRequest _activeTransitionRequest;
    private ResourceReference _activeTransitionSceneRef;
    private bool _activeTransitionSynced;
    private Node _activeTransitionPendingScene;
    private bool _waitingForLoad = false;

    public override void _Ready()
    {
        base._Ready();
        CurrentScene = GetTree().CurrentScene;
        AddChild(SceneReadyAwaiter = new PeerAwaiter()
            { Name = "Scene Ready Awaiter", Mode = PeerAwaiter.PeerAwaitMode.AllPeers });
        SceneReadyAwaiter.Completed += FinishSyncedSceneChange;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
        TransferChannel = GdfConstants.DefaultRpcTransferChannel)]
    public void TransitionToSceneRpc(Variant request, Variant transition, bool synced)
    {
        var requestRes = new SceneChangeRequest();
        requestRes.Deserialize(request);
        var transitionRes = new ScreenTransitionReference();
        transitionRes.Deserialize(transition);
        TransitionToScene(requestRes, transitionRes, synced);
    }

    public static void TransitionToScene(SceneChangeRequest request, ScreenTransitionReference transitionReference = null, bool sync = false)
    {
        var requestSerialized = request.Serialize();
        transitionReference ??= Instance.DefaultScreenTransition;
        if (sync && Instance.IsMultiplayerAuthority())
        {
            SceneReadyAwaiter.Start();
            Instance.Rpc(MethodName.TransitionToSceneRpc, requestSerialized, transitionReference?.Serialize() ?? default, sync);
        }

        if (!sync || Instance.IsMultiplayerAuthority())
        {
            // Duplicate request for host or client-side transition.
            // Avoid caching PackedScene in resource potentially passed from an autoloaded or persistent object.
            request = new SceneChangeRequest();
            request.Deserialize(requestSerialized);
        }

        bool interruptingSyncedTransition =
            Instance._activeTransitionSynced && Instance._activeTransitionRequest != null;

        Instance._activeTransitionSynced = sync;
        Instance._activeTransitionRequest = request;
        Instance._activeTransitionSceneRef = request.CreateSceneReference();
        Instance._waitingForLoad = false;
        Instance.TransitionStatus = TransitionStatusEnum.Loading;
        
        // Call garbage collection now before starting a new thread to load the new scene in the background.
        // Doing this now, as well as again a couple frames later BEFORE starting the background loading thread should
        // remove any stale resource references, whose garbage collection could trigger a crash during loading.
        if (!interruptingSyncedTransition)
        {
            MultiPassGarbageCollect(LoadingStrategy.MaxCollectionsOnFadeIn);
        }

        if (LoadingStrategy is { LoadAsynchronously: true })
            Instance._activeTransitionSceneRef.RequestLoad();
        if (IsInstanceValid(Instance._activeTransition))
        {
            Instance._activeTransition.ForceHideAndFreeScreen();
            Instance._activeTransition = null;
        }

        if (!sync && interruptingSyncedTransition)
        {
            // If interrupting a synchronized transition with a non-synchronized transition,
            // confirm self to ensure other peers aren't locked waiting for this peer.
            SceneReadyAwaiter.ConfirmSelf();
        }

        if (sync && interruptingSyncedTransition)
        {
            GD.Print("Interrupting synced scene transition with another synced transition!");
        }

        Instance._activeTransition = ScreenTransitionSystem.Instance.StartTransition(transitionReference,
            new Callable(Instance, MethodName.OnTransitionStay),
            new Callable(Instance, MethodName.OnTransitionFinished),
            initialState: new()
            {
                Status = Instance._transitionStatus.ToString().ToSnakeCase(),
                BlockedExternal = true
            });
        
        Instance._activeTransition.BlockedExternal = true;
    }

    private static void OnTransitionStay()
    {
        ChangeSceneToNode(new Node());
        Instance._waitingForLoad = true;
        Instance.TransitionStatus = TransitionStatusEnum.Loading;
        Instance.WaitForLoad();
    }

    private void WaitForLoad()
    {
        if (Godot.Input.IsKeyPressed(Key.O)) return;
        var sceneReference = Instance._activeTransitionSceneRef;
        // TODO handle null scene reference.
        if (sceneReference == null) return;

        if (LoadingStrategy.LoadAsynchronously)
        {
            if (sceneReference.GetStatus() == ResourceLoader.ThreadLoadStatus.InvalidResource)
            {
                sceneReference.RequestLoad();
            }
            if (sceneReference.GetStatus() == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                OnTransitionSceneLoaded();
            }
        }
        else
        {
            sceneReference.GetResource(); // lock main thread and wait for load
            OnTransitionSceneLoaded();
        }
    }

    private GodotThread _instantiationThread;

    private void OnTransitionSceneLoaded()
    {
        _waitingForLoad = false;
        Instance.TransitionStatus = TransitionStatusEnum.Instantiating;
        var scene = Instance._activeTransitionSceneRef.GetResource<PackedScene>();
        if (LoadingStrategy.InstantiateAsynchronously)
        {
            _instantiationThread = new GodotThread();
            _instantiationThread.Start(Callable.From(() => scene.GdfInstantiate()));
        }
        else
        {
            var node = scene.GdfInstantiate();
            if (!Instance._activeTransitionSynced)
            {
                ChangeScene(Instance._activeTransitionRequest, node);
                Instance._activeTransitionRequest = null;
                Instance._activeTransitionSceneRef = null;
                Instance._activeTransitionSynced = false;
                Instance._activeTransitionPendingScene = null;
            }
            else
            {
                PrepareSyncedSceneChange(Instance._activeTransitionRequest, node);
            }
        }
    }

    private static void OnTransitionFinished()
    {
        if (IsInstanceValid(Instance._activeTransition)) Instance._activeTransition.ForceHideAndFreeScreen();

        Instance._activeTransition = null;
    }

    public override void _Process(double delta)
    {
        if (_waitingForLoad)
        {
            WaitForLoad();
        }

        if (TransitionStatus == TransitionStatusEnum.Instantiating)
        {
            if (!_instantiationThread.IsAlive())
            {
                var node = _instantiationThread.WaitToFinish().As<Node>();
                _instantiationThread = null;
                if (_activeTransition?.BlockedInternal ?? false)
                {
                    TransitionStatus = TransitionStatusEnum.WaitingForTransition;
                    Instance._activeTransitionPendingScene = node;
                }
                else if (!Instance._activeTransitionSynced)
                {
                    ChangeScene(Instance._activeTransitionRequest, node);
                    Instance._activeTransitionRequest = null;
                    Instance._activeTransitionSceneRef = null;
                    Instance._activeTransitionSynced = false;
                    Instance._activeTransitionPendingScene = null;
                }
                else
                {
                    PrepareSyncedSceneChange(Instance._activeTransitionRequest, node);
                }
            }
        }

        if (TransitionStatus == TransitionStatusEnum.WaitingForTransition && !(_activeTransition?.BlockedInternal ?? false))
        {
            if (!Instance._activeTransitionSynced)
            {
                ChangeScene(Instance._activeTransitionRequest, Instance._activeTransitionPendingScene);
                Instance._activeTransitionRequest = null;
                Instance._activeTransitionSceneRef = null;
                Instance._activeTransitionSynced = false;
                Instance._activeTransitionPendingScene = null;
            }
            else
            {
                PrepareSyncedSceneChange(Instance._activeTransitionRequest, Instance._activeTransitionPendingScene);
            }
        }
    }

    public static void ChangeScene(SceneChangeRequest request)
    {
        SceneReadyAwaiter.Start();
        UpdateLastChangeRequest(request);
        var sceneRef = request.CreateSceneReference();
        var loadedScene = sceneRef.GetResource<PackedScene>(cacheReference: false);
        var sceneNode = loadedScene.GdfInstantiate();

        if (ChangeSceneToNode(sceneNode) == Error.Ok) Instance.CallDeferred(MethodName.AfterSceneChanged, request);
    }

    private static void ChangeScene(SceneChangeRequest request, Node sceneNode)
    {
        SceneReadyAwaiter.Start();
        UpdateLastChangeRequest(request);

        if (ChangeSceneToNode(sceneNode) == Error.Ok) Instance.CallDeferred(MethodName.AfterSceneChanged, request);
    }

    public static void PrepareSyncedSceneChange(SceneChangeRequest request, Node sceneNode)
    {
        Instance._activeTransitionPendingScene = sceneNode;
        Instance.TransitionStatus = TransitionStatusEnum.WaitingForPeers;
        if (MimicSyncedTransitionDelayOffline && SceneReadyAwaiter.TotalAwaiting == 1)
        {
            Instance.GetTree().CreateTimer(0.1f, true, false, true).Timeout += TryConfirmReadyToChangeScene;
        }
        else
        {
            TryConfirmReadyToChangeScene();
        }
    }

    private static void TryConfirmReadyToChangeScene()
    {
        if (Instance.TransitionStatus < TransitionStatusEnum.WaitingForPeers)
        {
            return;
        }
        SceneReadyAwaiter.ConfirmSelf();
    }

    private void FinishSyncedSceneChange()
    {
        if (!_activeTransitionSynced) return;
        if (Instance._activeTransition != null) Instance._activeTransition.BlockedExternal = false;
        Instance.TransitionStatus = TransitionStatusEnum.ChangingScene;
        var request = Instance._activeTransitionRequest;
        UpdateLastChangeRequest(request);

        if (ChangeSceneToNode(_activeTransitionPendingScene) == Error.Ok)
            Instance.CallDeferred(MethodName.AfterSceneChanged, request);
        Instance._activeTransitionRequest = null;
        Instance._activeTransitionSceneRef = null;
        Instance._activeTransitionSynced = false;
        Instance._activeTransitionPendingScene = null;
        _waitingForLoad = false;
        FlushSceneChange();
    }

    private static void UpdateLastChangeRequest(SceneChangeRequest request)
    {
        LastLevelChangeRequest = request;
    }

    private Node _outgoingScene;
    private Node _incomingScene;
    private bool _sceneChangeQueued = false;
    private TransitionStatusEnum _transitionStatus;

    private static Error ChangeSceneToNode(Node newScene)
    {
        if (newScene == null)
        {
            GD.PrintErr("Can't change to a null scene.");
            return Error.InvalidParameter;
        }

        var tree = Instance.GetTree();

        if (Instance._incomingScene != null && Instance._incomingScene != newScene)
        {
            // called multiple times in a single frame, delete the previously passed node
            if (IsInstanceValid(Instance._incomingScene)) Instance._incomingScene.QueueFree();
            Instance._incomingScene = null;
        }

        if (tree.CurrentScene is { } currentScene)
        {
            currentScene.GetParent().RemoveChild(currentScene);
            tree.CurrentScene = null;
            Instance.CurrentScene = null;
            Instance._outgoingScene = currentScene;
        }

        Instance._incomingScene = newScene;

        if (!Instance._sceneChangeQueued)
        {
            tree.CreateTimer(0).Timeout += Instance.FlushSceneChange;
            Instance._sceneChangeQueued = true;
        }

        return Error.Ok;
    }

    private void FlushSceneChange()
    {
        if (!_sceneChangeQueued) return;
        _sceneChangeQueued = false;
        var tree = Instance.GetTree();

        if (IsInstanceValid(_outgoingScene))
        {
            _outgoingScene.Free();
            _outgoingScene = null;
        }

        if (IsInstanceValid(_incomingScene))
        {
            var newScene = _incomingScene;
            _incomingScene = null;
            // tree.CurrentScene = newScene; // TODO this throws an error because it requires the node to be a child of the root. Can't replicate the exact behavior of the native scene change methods.
            CurrentScene = newScene;
            tree.Root.AddChild(newScene);
            tree.CurrentScene = newScene;

            tree.Root.UpdateMouseCursorState();

            tree.EmitSignal(SceneTree.SignalName.SceneChanged);
        }
        else
        {
            _incomingScene = null;
            tree.CurrentScene = null;
            CurrentScene = null;

            GD.PrintErr("Scene instance has been freed before becoming the current scene. No current scene is set.");
        }
    }

    private void AfterSceneChanged(SceneChangeRequest request)
    {
        if (Instance._activeTransition != null) Instance._activeTransition.BlockedExternal = false;
        MultiPassGarbageCollect(LoadingStrategy.MaxCollectionsOnFadeOut);
        EmitSignal(SignalName.SceneChanged, request);
        Instance.TransitionStatus = TransitionStatusEnum.NotLoading;
    }

    public static ResourceReference GetCurrentPackedScene()
    {
        var currentScene = Instance.GetTree().CurrentScene;
        return new ResourceReference(currentScene.SceneFilePath);
    }

    public Node GetCurrentScene()
    {
        return CurrentScene;
    }
    
    public static bool CheckAsyncDisabled(Node node)
    {
        if (!InstanceExists) return false;
        if (Instance.GetTree().GetNodeCountInGroup(GroupNameAsyncDisabled) == 0) return false;
        Node ancestor = node;
        while (ancestor != null)
        {
            if (ancestor.IsInGroup(GroupNameAsyncDisabled))
            {
                // GD.Print($"Async load suppressed ({node.GetSceneAndPathString()})");
                return true;
            }
            ancestor = ancestor.GetParent();
        }

        return false;
    }

    public static void MultiPassGarbageCollect(int maxPasses, bool verbose = false)
    {
        if (maxPasses <= 0) return;
        const int finalizationCountBreakThreshold = 2;

        long prevFinalizationPendingCount = GC.GetGCMemoryInfo().FinalizationPendingCount;
        long fromFinalizationPendingCount = prevFinalizationPendingCount;
        var currentPass = 0;
        for (; currentPass < maxPasses; currentPass++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long currentFinalizationPendingCount = GC.GetGCMemoryInfo().FinalizationPendingCount;
            if (currentFinalizationPendingCount <= finalizationCountBreakThreshold)
            {
                prevFinalizationPendingCount = currentFinalizationPendingCount;
                break;
            }
            else
            {
                prevFinalizationPendingCount = currentFinalizationPendingCount;
            }
        }

        if (verbose)
            GD.Print(
                $"Finished multi-pass garbage collection after {Mathf.Min(currentPass, maxPasses)} of {maxPasses} passes ({fromFinalizationPendingCount} -> {prevFinalizationPendingCount} pending finalizers)");
    }

    public enum TransitionStatusEnum
    {
        NotLoading,
        Loading,
        Instantiating,
        WaitingForTransition,
        WaitingForPeers,
        ChangingScene
    }

    public struct SceneLoadingStrategy
    {
        public bool LoadAsynchronously;
        public bool InstantiateAsynchronously;
        public int MaxCollectionsOnFadeIn;
        public int MaxCollectionsOnFadeOut;
        
        public static readonly SceneLoadingStrategy Synchronous = new SceneLoadingStrategy()
        {
            LoadAsynchronously = false,
            InstantiateAsynchronously = false,
            MaxCollectionsOnFadeIn = 0,
            MaxCollectionsOnFadeOut = 0
        };
        public static readonly SceneLoadingStrategy Asynchronous = new SceneLoadingStrategy()
        {
            LoadAsynchronously = true,
            InstantiateAsynchronously = true,
            MaxCollectionsOnFadeIn = 0,
            MaxCollectionsOnFadeOut = 0
        };
        public static readonly SceneLoadingStrategy AsynchronousSingleCollection = new SceneLoadingStrategy()
        {
            LoadAsynchronously = true,
            InstantiateAsynchronously = true,
            MaxCollectionsOnFadeIn = 1,
            MaxCollectionsOnFadeOut = 0
        };
        public static readonly SceneLoadingStrategy AsynchronousMultipleCollection = new SceneLoadingStrategy()
        {
            LoadAsynchronously = true,
            InstantiateAsynchronously = true,
            MaxCollectionsOnFadeIn = 8,
            MaxCollectionsOnFadeOut = 7
        };
    }
}