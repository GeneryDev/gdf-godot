using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GDF.Input;

[Tool]
[GlobalClass]
public partial class GdfPlayerInput : Node
{
    [Signal]
    public delegate void PlayerIdChangedEventHandler(int playerId);

    [Export]
    public int PlayerId
    {
        get => _playerId;
        set
        {
            if (_playerId == value) return;
            _playerId = value;
            EmitSignalPlayerIdChanged(_playerId);
        }
    }

    [Export] public int[] DeviceIndices = System.Array.Empty<int>();
    [Export] public bool UsesKeyboard;
    [Export] public bool UsesMouse;
    [Export] public Array<PackedScene> InputContexts = new();
    private System.Collections.Generic.Dictionary<string, InputActionState> _actionStates = new();
    private HashSet<InputActionEventPair> _actionsNeedUpdating = new();
    private List<InputActionEvent> _actionEventBuffer = new();
    private int _playerId = 0;

    public override void _EnterTree()
    {
        if (!Engine.IsEditorHint())
        {
            ResetActionStates();
            GdfInputManager.Singleton?.ConnectPlayer(this);
        }
    }

    public void ResetActionStates()
    {
        _actionStates.Clear();
        if (InputContexts == null) return;
        foreach (var contextScene in InputContexts)
        {
            var context = GdfInputManager.Singleton?.GetContextInstance(contextScene);
            context?.MergeActionStates(this);
        }
    }

    public override void _ExitTree()
    {
        if(!Engine.IsEditorHint())
            GdfInputManager.Singleton?.DisconnectPlayer(this);
    }

    public bool UsesDevice(int deviceIndex)
    {
        if (DeviceIndices != null)
        {
            foreach (int device in DeviceIndices)
            {
                if (device == deviceIndex) return true;
            }
        }

        return false;
    }

    public bool CanHandleEvent(InputEvent evt)
    {
        return UsesDevice(evt.Device) ||
               (evt is InputEventKey && UsesKeyboard) ||
               (evt is InputEventMouse && UsesMouse);
    }

    public void HandleEvent(InputEvent evt)
    {
        if (!CanHandleEvent(evt)) return;
        if (InputContexts == null) return;
        
        foreach (var contextScene in InputContexts)
        {
            var context = GdfInputManager.Singleton.GetContextInstance(contextScene);
            context.HandleInput(this, evt);
        }
    }

    public void SetActionState(GdfInputAction action, InputActionState result)
    {
        if (action == null) return;
        string actionKey = action.ActionKey;
        var state = _actionStates.GetValueOrDefault(actionKey);

        state.Strength = result.Strength;
        state.BoolValue = result.BoolValue;
        state.AxisValue = result.AxisValue;
        
        _actionStates[actionKey] = state;
    }

    public void MergeActionState(GdfInputAction action, InputActionState result)
    {
        if (action == null) return;
        string actionKey = action.ActionKey;
        var state = _actionStates.GetValueOrDefault(actionKey);
        if (state.Strength >= result.Strength) return;

        state.Strength = result.Strength;
        state.BoolValue = result.BoolValue;
        state.AxisValue = result.AxisValue;
        
        _actionStates[actionKey] = state;
    }

    public void QueueUpdateAction(GdfInputAction action, InputEvent associatedInputEvent)
    {
        _actionsNeedUpdating.Add(new InputActionEventPair() {Action = action, Event = associatedInputEvent});
    }

    public void NotifyUsed(GdfInputAction action, GdfInputContext context, InputEvent associatedEvent)
    {
        GdfInputManager.Singleton.NotifyUsed(this, action, context, associatedEvent);
    }

    private void UpdateAction(GdfInputAction action, InputEvent associatedInputEvent)
    {
        InputActionState maxStrengthState = default;
        if (InputContexts != null)
        {
            foreach (var contextScene in InputContexts)
            {
                var context = GdfInputManager.Singleton.GetContextInstance(contextScene);
                var state = context.GetActionState(this, action);
                if (state.Strength >= maxStrengthState.Strength) maxStrengthState = state;
            }
        }

        var prevState = GetActionState(action);
        var newState = maxStrengthState;
        SetActionState(action, newState);
        if (action.SendEventOnPress && !prevState.BoolValue && newState.BoolValue)
        {
            SendActionEvent(action, InputActionEventType.Press, action.EventBufferTime / 1000f, associatedInputEvent);
        }
        if (action.SendEventOnRelease && prevState.BoolValue && !newState.BoolValue)
        {
            SendActionEvent(action, InputActionEventType.Release, action.EventBufferTime / 1000f, associatedInputEvent);
        }
    }

    public void UpdateActions(double delta)
    {
        UpdateActionEvents(delta);
        foreach (var entry in _actionsNeedUpdating)
        {
            UpdateAction(entry.Action, entry.Event);
        }
        _actionsNeedUpdating.Clear();
    }

    private void UpdateActionEvents(double delta)
    {
        for (var i = 0; i < _actionEventBuffer.Count; i++)
        {
            var evt = _actionEventBuffer[i];
            evt.RemainingTime -= (float)delta;
            if (evt.RemainingTime <= 0)
            {
                // Event expired, not consumed
                if (evt.Type != InputActionEventType.Expired && evt.Action.SendEventOnExpire)
                {
                    // replace with expired event
                    _actionEventBuffer[i] = evt with
                    {
                        Type = InputActionEventType.Expired,
                        RemainingTime = evt.Action.ExpireEventBufferTime
                    };
                }
                else
                {
                    // Remove event
                    _actionEventBuffer.RemoveAt(i);
                    i--;
                }
            }
            else
            {
                _actionEventBuffer[i] = evt;
            }
        }
    }

    public void SendActionEvent(GdfInputAction action, InputActionEventType eventType, float timeSec, InputEvent associatedInputEvent = null)
    {
        // First, look for a matching event in the buffer, and if it exists, refresh its duration.
        for (var i = 0; i < _actionEventBuffer.Count; i++)
        {
            var evt = _actionEventBuffer[i];
            if (evt.Action != action) continue;
            if (eventType != evt.Type) continue;

            evt.RemainingTime = Mathf.Max(evt.RemainingTime, timeSec);
            _actionEventBuffer[i] = evt;
            return;
        }
        // Otherwise, add the new event to the buffer
        _actionEventBuffer.Add(new InputActionEvent()
        {
            Action = action,
            Type = eventType,
            RemainingTime = timeSec,
            AssociatedInputEvent = associatedInputEvent
        });
    }

    public bool ConsumeActionEvent(GdfInputAction action, InputActionEventType? eventType = null)
    {
        return HasActionEvent(action, eventType, consume: true);
    }

    public bool HasActionEvent(GdfInputAction action, InputActionEventType? eventType, bool consume = false)
    {
        // Note: Any changes to this method must also be applied to FindActionEvent below
        for (var i = 0; i < _actionEventBuffer.Count; i++)
        {
            var evt = _actionEventBuffer[i];
            if (evt.Action != action) continue;
            if (eventType.HasValue && eventType != evt.Type) continue;

            if (consume)
            {
                _actionEventBuffer.RemoveAt(i);
                if (evt.AssociatedInputEvent != null)
                    GdfInputManager.Singleton.RetroactivelyConsumeInputEvent(evt.AssociatedInputEvent);
            }

            return true;
        }

        return false;
    }

    // Deliberately copied the above method to avoid unnecessary struct copying most of the time
    public bool FindActionEvent(GdfInputAction action, InputActionEventType? eventType, out InputActionEvent foundEvent, bool consume = false)
    {
        // Note: Any changes to this method must also be applied to HasActionEvent above
        for (var i = 0; i < _actionEventBuffer.Count; i++)
        {
            var evt = _actionEventBuffer[i];
            if (evt.Action != action) continue;
            if (eventType.HasValue && eventType != evt.Type) continue;

            if (consume)
            {
                _actionEventBuffer.RemoveAt(i);
                if (evt.AssociatedInputEvent != null)
                    GdfInputManager.Singleton.RetroactivelyConsumeInputEvent(evt.AssociatedInputEvent);
            }

            foundEvent = evt;

            return true;
        }

        foundEvent = default;

        return false;
    }

    public void RetroactivelyConsumeInputEvent(InputEvent iEvt)
    {
        // Retroactively remove buffered action events that were created by the given input event.
        // This allows multiple actions mapped to the same key to consume one another when one is pressed.
        for (var i = 0; i < _actionEventBuffer.Count; i++)
        {
            var evt = _actionEventBuffer[i];
            if (evt.AssociatedInputEvent != iEvt) continue;
            _actionEventBuffer.RemoveAt(i);
            i--;
        }
    }

    public void PrintState()
    {
        GD.Print($"[{Name}] action states:");
        foreach (var entry in _actionStates)
        {
            GD.Print(entry.Key + ": " + entry.Value.BoolValue + " or " + entry.Value.AxisValue);
        }
        foreach (var entry in _actionEventBuffer)
        {
            GD.Print(entry.Action.Name + ": Buffered " + entry.Type + " " + entry.RemainingTime);
        }
        GD.Print("");
    }

    public InputActionState GetActionState(GdfInputAction action)
    {
        string actionKey = action?.ActionKey;
        if (actionKey == null) return default;
        return _actionStates.GetValueOrDefault(actionKey);
    }

    public bool GetBool(GdfInputAction action)
    {
        return GetActionState(action).BoolValue;
    }

    public float GetFloat(GdfInputAction action)
    {
        return GetActionState(action).Axis1Value;
    }

    public Vector2 GetVec2(GdfInputAction action)
    {
        return GetActionState(action).Axis2Value;
    }

    public Vector3 GetVec3(GdfInputAction action)
    {
        return GetActionState(action).Axis3Value;
    }
    
    public struct InputActionEvent
    {
        public GdfInputAction Action;
        public InputActionEventType Type;
        public float RemainingTime;
        public InputEvent AssociatedInputEvent;
    }

    public enum InputActionEventType
    {
        Press,
        Release,
        Expired
    }

    public struct InputActionState
    {
        public bool BoolValue;
        
        public float Strength;
        
        public Vector3 AxisValue;

        public float Axis1Value
        {
            get => AxisValue.X;
            set => AxisValue = AxisValue with { X = value };
        }
        public Vector2 Axis2Value
        {
            get => new Vector2(AxisValue.X, AxisValue.Y);
            set => AxisValue = new Vector3(value.X, value.Y, 0);
        }
        public Vector3 Axis3Value
        {
            get => AxisValue;
            set => AxisValue = value;
        }
    }

    private struct InputActionEventPair
    {
        public GdfInputAction Action;
        public InputEvent Event;

        public bool Equals(InputActionEventPair other)
        {
            return Equals(Action, other.Action);
        }

        public override bool Equals(object obj)
        {
            return obj is InputActionEventPair other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Action != null ? Action.GetHashCode() : 0);
        }

        public static bool operator ==(InputActionEventPair left, InputActionEventPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InputActionEventPair left, InputActionEventPair right)
        {
            return !left.Equals(right);
        }
    }
}
