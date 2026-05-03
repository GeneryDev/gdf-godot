using System;
using System.Collections.Generic;
using System.Linq;
using GDF.Networking;
using GDF.Util;
using Godot;

namespace GDF.Components.StateMachines;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/state_machine.png")]
public partial class StateMachine : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(State from, State to);

    [Export] public State InitialState;

    [Export] public bool AllowRoutinesOutsideStateMachine = false;

    [ExportGroup("Networking")]
    [Export] public bool ReplicateToPeers;

    [ExportGroup("Debug")]
    [Export] public bool PrintStateChanges;
    
    public State CurrentState;

    public double StateTimeSec = 0;
    public ulong StateTimeMs => (ulong)(StateTimeSec * 1000);

    private readonly List<State> _states = new();
    private readonly List<Node> _allRoutines = new();
    private readonly Dictionary<Node, RoutineLocationInfo> _routineLocations = new();
    
    private List<Node> _activeRoutines = new();
    private List<Node> _tempRoutineList = new();
    private List<State> _tempStateListExiting = new();
    private List<State> _tempStateListEntering = new();
    private bool _transitionInProgress = false;

    private bool _initialized = false;

    public override void _Ready()
    {
        base._Ready();
        Initialize();
        TransitionToState(InitialState ?? _states.FirstOrDefault());
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        InitializeStates();
        InitializeRoutines();
    }

    private void InitializeStates()
    {
        _states.Clear();
        _allRoutines.Clear();
        _routineLocations.Clear();
        
        foreach (var state in this.IterateChildrenOfType<State>())
            InitializeState(state);
    }

    private void InitializeState(State state)
    {
        _states.Add(state);
        state.Initialize(this);
        foreach (var subState in state.IterateChildrenOfType<State>())
            InitializeState(subState);
    }

    private void InitializeRoutines()
    {
        foreach (var state in _states)
            state.CollectSubRoutines(_allRoutines, inherited: false);

        _allRoutines.RemoveDuplicates();
        
        // Remove invalid routines
        for (var index = 0; index < _allRoutines.Count; index++)
        {
            var routine = _allRoutines[index];
            if (routine == null)
            {
                _allRoutines.RemoveAt(index);
                index--;
                continue;
            }
            else if (!this.IsAncestorOf(routine) && !AllowRoutinesOutsideStateMachine)
            {
                GD.PrintErr($"Found invalid routine '{routine.Name}' in state machine '{Name}': State Machine must be an ancestor of the routine.");
                _allRoutines.RemoveAt(index);
                index--;
                continue;
            }
        }

        // Initialize routines
        for (var index = 0; index < _allRoutines.Count; index++)
        {
            var routine = _allRoutines[index];
            InitializeRoutine(routine);
        }
    }

    private void InitializeRoutine(Node routine)
    {
        var info = RoutineLocationInfo.From(routine);
        info.ParentNode.RemoveChild(routine);

        _routineLocations[routine] = info;
        routine.ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        StateTimeSec += delta;

        if (IsMultiplayerAuthority())
            CurrentState?.TickTransitions(delta);
    }

    public State GetState(string statePath)
    {
        foreach (var state in _states)
        {
            if (state.StatePath == statePath) return state;
        }

        return null;
    }

    public bool TransitionToState(State state)
    {
        if (state == null)
        {
            GD.PushError($"Cannot transition state machine [{Name}] to a null state.");
            return false;
        }

        if (ReplicateToPeers && IsMultiplayerAuthority())
            this.CustomRpc(MethodName.TransitionToStateRpc, GetPathTo(state));

        var fromState = CurrentState;
        return HandleTransition(fromState, state);
    }

    public bool TransitionToState(string statePath)
    {
        var state = GetState(statePath);
        if (state == null)
        {
            GD.PushError($"Cannot transition state machine [{Name}] to state '{statePath}': state not found.");
            return false;
        }

        return TransitionToState(state);
    }

    private bool HandleTransition(State from, State to)
    {
        if(PrintStateChanges) GD.Print($"[{Name}] Transitioning state from {from?.Name} to {to?.Name}");
        if (_transitionInProgress)
        {
            GD.PrintErr($"Cannot transition state machine [{Name}], a transition is already in progress!");
            return false;
        }
        
        CurrentState = to;
        try
        {
            _transitionInProgress = true;
            // Collect routines to disable and enable
            _tempRoutineList.Clear();
            _tempRoutineList.AddRange(_activeRoutines);
            _activeRoutines.Clear();
            to?.CollectSubRoutines(_activeRoutines, inherited: true);
            // tempRoutineList now contains routines that used to be enabled
            // activeRoutines now contains routines that are now be enabled
            // Collect states entering and exiting
            _tempStateListExiting.Clear();
            _tempStateListEntering.Clear();
            CollectStateHierarchy(from, _tempStateListExiting, reverse: true);
            CollectStateHierarchy(to, _tempStateListEntering, reverse: false);
            RemoveDuplicates(_tempStateListEntering, _tempStateListExiting);

            // Emit state exiting signals
            foreach (var stateExiting in _tempStateListExiting)
            {
                stateExiting.NotifyExiting();
                stateExiting.ProcessMode = ProcessModeEnum.Disabled;
            }

            // Disable exiting routines
            foreach (var routine in _tempRoutineList)
            {
                if (!_activeRoutines.Contains(routine))
                {
                    DisableRoutine(routine);
                }
            }

            StateTimeSec = 0;

            // Enable entering routines
            foreach (var routine in _activeRoutines)
            {
                if (!_tempRoutineList.Contains(routine))
                {
                    EnableRoutine(routine);
                }
            }

            // Emit state exiting signals
            foreach (var stateEntering in _tempStateListEntering)
            {
                stateEntering.NotifyEntering();
                stateEntering.ProcessMode = ProcessModeEnum.Inherit;
            }

        }
        finally
        {
            _transitionInProgress = false;
        }
        EmitSignalStateChanged(from, to);
        return true;
    }

    private void CollectStateHierarchy(State from, List<State> output, bool reverse)
    {
        var state = from;
        int insertionIndex = output.Count;
        while (state != null)
        {
            if (reverse)
                insertionIndex = output.Count;
            output.Insert(insertionIndex, state);
            state = state.ParentState;
        }
    }

    private void DisableRoutine(Node routine)
    {
        var location = _routineLocations[routine];
        if (routine.GetParent() != location.ParentNode)
        {
            GD.PrintErr($"Failed to disable routine {routine.Name}, not in its parent");
            return;
        }
        location.ParentNode.RemoveChild(routine);
    }

    private void EnableRoutine(Node routine)
    {
        var location = _routineLocations[routine];
        if (routine.GetParent() != null)
        {
            GD.PrintErr($"Failed to disable routine {routine.Name}, already has a parent");
            return;
        }
        location.ParentNode.AddChild(routine);
    }

    [CustomRpc(GdfConstants.DefaultRpcChannelPresetName, CallLocal = false, Mode = MultiplayerApi.RpcMode.Authority)]
    private void TransitionToStateRpc(NodePath statePath)
    {
        var state = GetNode<State>(statePath);
        TransitionToState(state);
    }

    private void UpdateRoutines()
    {
        // var newActiveRoutines = _tempRoutineList;
        // CurrentState.CollectSubRoutines(newActiveRoutines);
        // foreach (var oldRoutine in _enabledRoutines)
        //     if (!newActiveRoutines.Contains(oldRoutine))
        //         oldRoutine.RoutineDisabled();
        // foreach (var newRoutine in newActiveRoutines)
        //     if (!_enabledRoutines.Contains(newRoutine))
        //         newRoutine.RoutineEnabled();
        //
        // _tempRoutineList = _enabledRoutines;
        // _tempRoutineList.Clear();
        // _enabledRoutines = newActiveRoutines;
    }

    public List<State>.Enumerator GetStates()
    {
        return _states.GetEnumerator();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == GdfConstants.NotificationDeepSceneInstantiated)
        {
            Initialize();
        }

        if (what == NotificationPredelete)
        {
            foreach (var routine in _allRoutines)
            {
                if (!IsInstanceValid(routine)) continue;
                routine.Free();
            }
        }
    }
    
    public static void RemoveDuplicates<T>(List<T> listA, List<T> listB) where T : class
    {
        for (var i = 0; i < listA.Count; i++)
        {
            for (var j = 0; j < listB.Count; j++)
            {
                if (listA[i] == listB[j])
                {
                    // duplicates
                    listA.RemoveAt(i);
                    listB.RemoveAt(j);
                    i--;
                    j--;
                    break;
                }
            }
        }
    }

    private struct RoutineLocationInfo
    {
        public Node ParentNode;
        public int Index;

        public static RoutineLocationInfo From(Node routine)
        {
            var parent = routine.GetParent();
            return new RoutineLocationInfo()
            {
                ParentNode = parent,
                Index = routine.GetIndex()
            };
        }
    }
}