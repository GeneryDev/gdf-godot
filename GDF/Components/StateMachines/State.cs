using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.Components.StateMachines;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/state.png")]
public partial class State : Node, ITagged<StringName>
{
    [Signal]
    public delegate void EnteredEventHandler();
    [Signal]
    public delegate void ExitedEventHandler();
    
    public StateMachine StateMachine { get; private set; }
    public State ParentState { get; private set; }
    public string StatePath { get; private set; }

    [ExportGroup("Routines")]
    [Export] public Node[] SubRoutines;
    [Export] public bool IncludeChildrenRoutines = true;

    [ExportGroup("Transitions")]
    [Export] public StateTransition[] Transitions;
    [Export] public bool IncludeChildrenTransitions = true;

    [ExportGroup("Tags")]
    [Export] public StringName[] StateTags;

    private readonly List<Node> _allOwnSubRoutines = new();
    private bool _allOwnSubRoutinesUpToDate = false;

    private readonly List<StateTransition> _allOwnTransitions = new();
    private bool _allOwnTransitionsUpToDate = false;

    public void Initialize(StateMachine stateMachine)
    {
        StateMachine = stateMachine;
        ParentState = GetParentOrNull<State>();
        StatePath = stateMachine.GetPathTo(this).ToString();

        ReadyOwnSubRoutines();
        ReadyOwnTransitions();
    }

    public bool TickTransitions(double delta)
    {
        if (ParentState?.TickTransitions(delta) ?? false) return true;
        ReadyOwnTransitions();
        foreach (var transition in _allOwnTransitions)
        {
            if (transition.ProcessTransitions(StateMachine)) return true;
        }

        return false;
    }

    public bool HasTag(StringName tag)
    {
        if (StateTags != null)
            foreach (var t in StateTags)
                if (t == tag)
                    return true;

        return ParentState?.HasTag(tag) ?? false;
    }

    public void CollectSubRoutines(List<Node> routines, bool inherited)
    {
        ReadyOwnSubRoutines();
        
        if (inherited)
            ParentState?.CollectSubRoutines(routines, inherited: true);

        routines.AddRange(_allOwnSubRoutines);
    }

    private void ReadyOwnTransitions()
    {
        if (_allOwnTransitionsUpToDate) return;
        _allOwnTransitionsUpToDate = true;
        _allOwnTransitions.Clear();
        if (Transitions != null)
            _allOwnTransitions.AddRange(Transitions);
        if (IncludeChildrenTransitions)
            _allOwnTransitions.AddRange(this.GetChildrenOfType<StateTransition>());

        _allOwnTransitions.RemoveNulls();
        _allOwnTransitions.RemoveDuplicates();
    }

    private void ReadyOwnSubRoutines()
    {
        if (_allOwnSubRoutinesUpToDate) return;
        _allOwnSubRoutinesUpToDate = true;
        _allOwnSubRoutines.Clear();
        if(SubRoutines != null)
            _allOwnSubRoutines.AddRange(SubRoutines);
        if (IncludeChildrenRoutines)
        {
            foreach (var child in this.GetChildren())
            {
                if (child is State or StateTransition) continue;
                
                _allOwnSubRoutines.Add(child);
            }
        }
        _allOwnSubRoutines.RemoveNulls();
    }

    public void NotifyEntering()
    {
        GD.Print($"Entering {Name}");
        EmitSignalEntered();
    }

    public void NotifyExiting()
    {
        GD.Print($"Exiting {Name}");
        EmitSignalExited();
    }
}