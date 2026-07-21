using GDF.Data;
using GDF.PropertyStacks;
using GDF.UI;
using Godot;

namespace GDF.Scenes.Transitions;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/screen_transition.png")]
public partial class ScreenTransition : Screen, IDataContext
{
    [Signal]
    public delegate void StayingEventHandler();

    [Signal]
    public delegate void FinishedEventHandler();

    [Signal]
    public delegate void StatusUpdatedEventHandler();

    private const int StateNotStarted = 0;
    private const int StateFadingIn = 1;
    private const int StateStaying = 2;
    private const int StateFadingOut = 3;
    private const int StateFinished = 4;

    [ExportGroup("Timings")] 
    [Export(PropertyHint.Range, "0,10,0.01,or_greater,suffix:s")]
    public float FadeInTime = 1f;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater,suffix:s")]
    public float StayTime = 1f;

    [Export(PropertyHint.Range, "0,10,0.01,or_greater,suffix:s")]
    public float FadeOutTime = 1f;

    [ExportGroup("Auto Animations")] 
    [Export]
    public AnimationPlayer AnimationPlayer;

    [Export] public StringName FadeInAnimationName = "fade_in";
    [Export] public StringName StayAnimationName = "stay";
    [Export] public StringName FadeOutAnimationName = "fade_out";

    [Export] public bool ScaleStayAnimationSpeed = false;

    private int _currentState;
    private float _stateTime;
    private bool _staySignalSent = false;
    
    public bool BlockedExternal = false;
    [Export]
    public bool BlockedInternal = false;

    public bool BlockedGlobal => GlobalPropertyStack.InstanceExists &&
                                 (GlobalPropertyStack.Instance?.GetEffectiveValue("hold_screen_transitions",
                                     false) ?? false);

    public bool BlockedDebug => (OS.HasFeature("debug") && Godot.Input.IsKeyPressed(Key.H));
    
    public bool Blocked =>
        BlockedExternal || BlockedInternal || BlockedGlobal || BlockedDebug;
    
    private string _status = "";

    public void Start()
    {
        if (FadeInTime > 0)
            StartFadeIn();
        else
            StartStay();
    }

    private void SetState(int state)
    {
        _currentState = state;
        _stateTime = 0;
    }

    private void StartFadeIn()
    {
        SetState(StateFadingIn);
        if (AnimationPlayer != null && AnimationPlayer.HasAnimation(FadeInAnimationName))
        {
            AnimationPlayer.Stop();
            AnimationPlayer.Play(FadeInAnimationName, customSpeed: 1.0f / FadeInTime);
            AnimationPlayer.Advance(0.0f);
        }
    }

    private void StartStay()
    {
        SetState(StateStaying);
        if (AnimationPlayer != null && AnimationPlayer.HasAnimation(StayAnimationName))
        {
            AnimationPlayer.Stop();
            AnimationPlayer.Play(StayAnimationName,
                customSpeed: ScaleStayAnimationSpeed && StayTime > 0 ? 1.0f / StayTime : 1);
            AnimationPlayer.Advance(0.0f);
        }
    }

    private void StartFadeOut()
    {
        SetState(StateFadingOut);
        if (AnimationPlayer != null && AnimationPlayer.HasAnimation(FadeOutAnimationName))
        {
            AnimationPlayer.Stop();
            AnimationPlayer.Play(FadeOutAnimationName, customSpeed: 1.0f / FadeOutTime);
            AnimationPlayer.Advance(0.0f);
        }
    }

    public void Finish()
    {
        SetState(StateFinished);
        EmitSignalFinished();
    }

    public override void _Process(double delta)
    {
        if (_currentState is StateNotStarted) return;
        if (_currentState is StateStaying && !_staySignalSent)
        {
            _staySignalSent = true;
            EmitSignalStaying();
        }

        if (_currentState is StateStaying && Blocked) return;
        _stateTime += (float)delta;
        switch (_currentState)
        {
            case StateFadingIn when _stateTime >= FadeInTime:
            {
                StartStay();
                break;
            }
            case StateStaying when _stateTime >= StayTime && !Blocked:
            {
                if (FadeOutTime > 0)
                    StartFadeOut();
                else
                    Finish();
                break;
            }
            case StateFadingOut when _stateTime >= FadeOutTime:
            {
                Finish();
                break;
            }
        }
    }

    public void SetTransitionStatus(string status)
    {
        _status = status;
        EmitSignalStatusUpdated();
    }

    public StringName UpdatedSignalName => SignalName.StatusUpdated;
    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "status":
            {
                this.OutputStringVariable(_status, ref output, input);
                return true;
            }
        }

        return false;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long) what)
        {
            case GdfConstants.NotificationDeepSceneInstantiated:
                this.ScreenShown += Start;
                break;
        }
    }
}