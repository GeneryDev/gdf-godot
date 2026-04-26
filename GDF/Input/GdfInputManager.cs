using System.Collections;
using System.Collections.Generic;
using Godot;

namespace GDF.Input;

[GlobalClass]
public partial class GdfInputManager : Node
{
    public static GdfInputManager Singleton;

    [Export]
    public bool AcceptJoypadInputsInBackground = true;
    [Export]
    public bool ProcessKeyEchoInputs = false;
    
    [Signal]
    public delegate void PlayerChangedInputDeviceEventHandler(int playerId);
    [Signal]
    public delegate void PlayerInputNodesUpdatedEventHandler();

    private readonly List<GdfPlayerInput> _players = new();
    private readonly Dictionary<string, GdfInputContext> _contextInstances = new();
    private readonly Dictionary<int, LastPlayerInput> _playerInputMemory = new();

    [Export]
    public GdfInputMap InputMap
    {
        get => _inputMap;
        set
        {
            if (_inputMap == value) return;
            if (_inputMap != null)
            {
                _inputMap.MappingSet -= OnMappingSet;
                _inputMap.MappingCleared -= OnMappingCleared;
                
                ClearMappings();
            }
            _inputMap = value;
            if (_inputMap != null)
            {
                _inputMap.MappingSet += OnMappingSet;
                _inputMap.MappingCleared += OnMappingCleared;
                
                ApplyMappings();
            }
        }
    }

    public bool ApplicationFocused { get; private set; } = false;
    
    private GdfInputMap _inputMap;

    public override void _EnterTree()
    {
        Singleton = this;
        
        var applicationWindow = GetWindow();
        ApplicationFocused = applicationWindow.HasFocus();
        applicationWindow.FocusEntered += OnApplicationWindowFocusEnter;
        applicationWindow.FocusExited += OnApplicationWindowFocusExit;
    }

    public override void _ExitTree()
    {
        if(Singleton == this)
            Singleton = null;
        
        var applicationWindow = GetWindow();
        ApplicationFocused = false;
        applicationWindow.FocusEntered -= OnApplicationWindowFocusEnter;
        applicationWindow.FocusExited -= OnApplicationWindowFocusExit;
    }

    public override void _UnhandledInput(InputEvent evt)
    {
        if (!AcceptJoypadInputsInBackground && evt is InputEventJoypadButton or InputEventJoypadMotion && !ApplicationFocused) return;
        if (!ProcessKeyEchoInputs && evt is InputEventKey { Echo: true }) return;
        // GD.Print($"[{evt.Device}] Evt: {evt}");
        foreach (var player in _players)
        {
            player.HandleEvent(evt);
        }
    }

    public override void _Process(double delta)
    {
        foreach (var player in _players)
        {
            player.UpdateActions(delta);
        }
    }

    public void ConnectPlayer(GdfPlayerInput player)
    {
        _players.Add(player);
        EmitSignalPlayerInputNodesUpdated();
    }

    public void DisconnectPlayer(GdfPlayerInput player)
    {
        _players.Remove(player);
        EmitSignalPlayerInputNodesUpdated();
    }

    public PlayerInputEnumerator GetPlayerNodes()
    {
        return new PlayerInputEnumerator(_players);
    }

    public GdfInputContext GetContextInstance(PackedScene scene)
    {
        string key = scene.ResourcePath;
        if (_contextInstances.TryGetValue(key, out var existing)) return existing;
        
        var newInstance = scene.Instantiate<GdfInputContext>();
        this.AddChild(newInstance);
        _contextInstances[key] = newInstance;
        
        ApplyMappings(newInstance);
        
        return newInstance;
    }

    public void RetroactivelyConsumeInputEvent(InputEvent evt)
    {
        foreach (var player in _players)
        {
            if (player.CanHandleEvent(evt))
            {
                player.RetroactivelyConsumeInputEvent(evt);
            }
        }
    }

    public void NotifyUsed(GdfPlayerInput playerInput, GdfInputAction action, GdfInputContext context, InputEvent associatedEvent)
    {
        int playerId = playerInput.PlayerId;
        var newInfo = new LastPlayerInput()
        {
            Device = GdfInputDevice.ForInputEvent(associatedEvent),
            Action = action,
            Node = playerInput,
            Context = context
        };
        var deviceChanged = false;

        if (_playerInputMemory.TryGetValue(playerId, out var prevInfo))
        {
            if (prevInfo.Device != newInfo.Device) deviceChanged = true;
        }
        else 
            deviceChanged = true;
        
        _playerInputMemory[playerId] = newInfo;

        if (deviceChanged)
        {
#if TOOLS
            GD.Print($"Player {playerId} changed input to: {newInfo.Device.Name}");
#endif
            EmitSignal(SignalName.PlayerChangedInputDevice, playerId);
        }
    }

    public LastPlayerInput GetLastPlayerInput(int playerId)
    {
        return _playerInputMemory.GetValueOrDefault(playerId);
    }

    private void ClearMappings()
    {
        foreach (var instance in _contextInstances.Values)
        {
            instance.ClearMappings();
        }
    }

    private readonly List<(string ContextTag, NodePath NodePath, GdfInputLocation Location)> _tempMappings = new();
    private void ApplyMappings()
    {
        _tempMappings.Clear();
        _inputMap.DumpMappings(_tempMappings);
        foreach (var instance in _contextInstances.Values)
        {
            foreach (var mapping in _tempMappings)
            {
                if (mapping.ContextTag != null && !instance.HasTag(mapping.ContextTag)) continue;
                ApplyMappingOrdered(instance, mapping.NodePath);
            }
        }
        _tempMappings.Clear();
    }
    private void ApplyMappings(GdfInputContext instance)
    {
        _tempMappings.Clear();
        _inputMap.DumpMappings(_tempMappings);
        foreach (var mapping in _tempMappings)
        {
            if (mapping.ContextTag != null && !instance.HasTag(mapping.ContextTag)) continue;
            ApplyMappingOrdered(instance, mapping.NodePath);
        }
        _tempMappings.Clear();
    }

    private void ApplyMappingOrdered(GdfInputContext instance, NodePath nodePath)
    {
        // Apply all mappings for all tags (including no tag first), not just the tag that changed,
        // in the order they appear on the context to ensure consistency
        if (_inputMap.TryGetMapping(null, nodePath, out var noTagLocation))
            instance.ApplyMapping(nodePath, noTagLocation);
        if (instance.Tags != null)
        {
            foreach (string tag in instance.Tags)
            {
                if (_inputMap.TryGetMapping(tag, nodePath, out var location))
                {
                    instance.ApplyMapping(nodePath, location);
                }
            }
        }
    }

    private void OnMappingSet(string contextTag, NodePath nodePath, GdfInputLocation newLocation)
    {
        foreach (var instance in _contextInstances.Values)
        {
            if (contextTag != null && !instance.HasTag(contextTag)) continue;
            ApplyMappingOrdered(instance, nodePath);
        }
    }

    private void OnMappingCleared(string contextTag, NodePath nodePath)
    {
        foreach (var instance in _contextInstances.Values)
        {
            if (contextTag != null && !instance.HasTag(contextTag)) continue;
            instance.ClearMapping(nodePath);
            ApplyMappingOrdered(instance, nodePath);
        }
    }

    private void OnApplicationWindowFocusEnter()
    {
        ApplicationFocused = true;
    }

    private void OnApplicationWindowFocusExit()
    {
        ApplicationFocused = false;
    }

    public struct LastPlayerInput
    {
        public GdfInputDevice Device;
        public GdfInputAction Action;
        
        public GdfPlayerInput Node;
        public GdfInputContext Context;
    }

    public struct PlayerInputEnumerator : IEnumerable<GdfPlayerInput>, IEnumerator<GdfPlayerInput>
    {
        private List<GdfPlayerInput> _players;
        private int _index = -1;

        public PlayerInputEnumerator()
        {
            
        }
        public PlayerInputEnumerator(List<GdfPlayerInput> players)
        {
            _players = players;
        }

        IEnumerator<GdfPlayerInput> IEnumerable<GdfPlayerInput>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PlayerInputEnumerator GetEnumerator() => this;

        public void Reset()
        {
            _index = -1;
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _players.Count;
        }

        public GdfPlayerInput Current => _players[_index];
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
        }
    }
}