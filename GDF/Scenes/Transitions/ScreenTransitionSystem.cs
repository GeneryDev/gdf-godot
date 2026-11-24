using System.Collections.Generic;
using GDF.Attributes;
using GDF.Attributes.Impl;
using GDF.Data;
using GDF.Util;
using Godot;

namespace GDF.Scenes.Transitions;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class ScreenTransitionSystem : SingletonNode<ScreenTransitionSystem>, IIndexableAttributeSet
{
    public BoolAttribute HoldScreenTransitions = new BoolAttribute(false)
    {
        DefaultFunction = BoolModifierChain.Functions.Or
    };

    private ScreenTransition _activeTransition;
    private ScreenTransitions _transitionsLibrary;

    public ScreenTransition StartTransition(ScreenTransitionReference transitionReference, Callable callbackOnStay, Callable callbackOnFinished, InitialTransitionState initialState = default)
    {
        var transitionId = transitionReference?.TransitionId;
        var transitionScene = ScreenTransitions.FromId(transitionId).Scene;
        if (transitionScene == null)
        {
            if (callbackOnStay.Method != null)
                callbackOnStay.Call();
            if (callbackOnFinished.Method != null)
                callbackOnFinished.Call();
            return null;
        }
        
        var transition = transitionScene.GdfInstantiate<ScreenTransition>();
        if (transition == null) return null;
        transition.SetMultiplayerAuthority(Instance.GetMultiplayerAuthority());

        var node = transition.ToPlaceholder();

        this.AddChild(node);

        if (!Engine.IsEditorHint())
            node.Owner = Instance;

        Instance._activeTransition = transition;
        transition.InjectContext(transitionReference);
        transition.TryConnect(ScreenTransition.SignalName.Staying, callbackOnStay);
        transition.TryConnect(ScreenTransition.SignalName.Finished, callbackOnFinished);
        transition.SetTransitionStatus(initialState.Status ?? "");
        transition.BlockedExternal = initialState.BlockedExternal;

        transition.ProcessMode = ProcessModeEnum.Always;
        transition.ShowScreen();
        return transition;
    }

    public override void _Notification(int what)
    {
        switch ((long) what)
        {
            case NotificationParented:
            {
                if (_transitionsLibrary == null)
                {
                    this.AddChild(_transitionsLibrary = new ScreenTransitions());
                }
                return;
            }
        }
    }

    public struct InitialTransitionState
    {
        public string Status = "";
        public bool BlockedExternal = false;

        public InitialTransitionState()
        {
        }
    }

    public void CollectAttributes(Dictionary<StringName, IAttribute> output)
    {
        output["hold_screen_transitions"] = HoldScreenTransitions;
    }
}