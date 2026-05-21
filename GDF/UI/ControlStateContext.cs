using System;
using System.Collections.Generic;
using GDF.Data;
using GDF.Util;
using Godot;

namespace GDF.UI;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_context.png")]
public partial class ControlStateContext : Node, IDataContext
{
    [Signal]
    public delegate void UpdatedEventHandler();
    
    [Export] public TrackedStateEnum TrackedStates;
    
    private Control _parentControl;
    private UserInterfaceComponent _uiComponent;
    private UserInterface _ui;
    private bool _hovered = false;
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();

    private TrackedStateEnum _connectedSignalStates;

    public StringName UpdatedSignalName => SignalName.Updated;

    public override void _EnterTree()
    {
        base._EnterTree();
        _parentControl = GetParent<Control>();
        _uiComponent = _parentControl?.GetChildOfType<UserInterfaceComponent>();
        _ui = _uiComponent?.GetUserInterface();
        ConnectSignals();
        Update();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DisconnectSignals();
    }

    private void Update()
    {
        EmitSignalUpdated();
    }

    private void ConnectSignals()
    {
        if ((TrackedStates & TrackedStateEnum.Hover) != 0)
        {
            if (_parentControl != null && TrySetStateSignalConnected(TrackedStateEnum.Hover, true))
            {
                _parentControl.MouseEntered += OnMouseEntered;
                _parentControl.MouseExited += OnMouseExited;
            }
        }
        if ((TrackedStates & TrackedStateEnum.Press) != 0)
        {
            if (_parentControl != null && TrySetStateSignalConnected(TrackedStateEnum.Press, true))
            {
                _parentControl.GuiInput += OnGuiInput;
            }
        }

        if ((TrackedStates & TrackedStateEnum.Visible) != 0)
        {
            if (_parentControl != null && TrySetStateSignalConnected(TrackedStateEnum.Visible, true))
            {
                _parentControl.VisibilityChanged += Update;
            }
        }
        
        if ((TrackedStates & TrackedStateEnum.Focus) != 0)
        {
            if (_uiComponent != null && TrySetStateSignalConnected(TrackedStateEnum.Focus, true))
            {
                _uiComponent.FocusEntered += Update;
                _uiComponent.FocusExited += Update;
            }
        }
        
        if ((TrackedStates & TrackedStateEnum.GroupFocus) != 0)
        {
            _uiComponent = _parentControl.GetChildOfType<UserInterfaceComponent>();
            if (_uiComponent != null && TrySetStateSignalConnected(TrackedStateEnum.GroupFocus, true))
            {
                _uiComponent.GroupFocusEntered += Update;
                _uiComponent.GroupFocusExited += Update;
                _uiComponent.GroupExclusiveFocusEntered += Update;
                _uiComponent.GroupExclusiveFocusExited += Update;
            }
        }
        
        if ((TrackedStates & TrackedStateEnum.InputType) != 0)
        {
            if (_ui != null && TrySetStateSignalConnected(TrackedStateEnum.InputType, true))
            {
                _ui.InputTypeUpdated += Update;
            }
        }
    }

    private void DisconnectSignals()
    {
        if (TrySetStateSignalConnected(TrackedStateEnum.Hover, false))
        {
            if (_parentControl != null)
            {
                _parentControl.MouseEntered -= OnMouseEntered;
                _parentControl.MouseExited -= OnMouseExited;
            }
        }
        if (TrySetStateSignalConnected(TrackedStateEnum.Press, false))
        {
            if (_parentControl != null)
            {
                _parentControl.GuiInput -= OnGuiInput;
            }
        }
        if (TrySetStateSignalConnected(TrackedStateEnum.Visible, false))
        {
            if (_parentControl != null)
            {
                _parentControl.VisibilityChanged -= Update;
            }
        }
        if (TrySetStateSignalConnected(TrackedStateEnum.Focus, false))
        {
            if (_uiComponent != null)
            {
                _uiComponent.FocusEntered -= Update;
                _uiComponent.FocusExited -= Update;
            }
        }
        if (TrySetStateSignalConnected(TrackedStateEnum.GroupFocus, false))
        {
            if (_uiComponent != null)
            {
                _uiComponent.GroupFocusEntered -= Update;
                _uiComponent.GroupFocusExited -= Update;
                _uiComponent.GroupExclusiveFocusEntered -= Update;
                _uiComponent.GroupExclusiveFocusExited -= Update;
            }
        }
        if (TrySetStateSignalConnected(TrackedStateEnum.InputType, false))
        {
            if (_ui != null)
            {
                _ui.InputTypeUpdated -= Update;
            }
        }
    }

    private void OnMouseEntered()
    {
        _hovered = true;
        Update();
    }

    private void OnMouseExited()
    {
        _hovered = false;
        Update();
    }

    private void OnGuiInput(InputEvent evt)
    {
        if (evt is InputEventMouseButton mbEvt)
        {
            if (mbEvt.Pressed)
            {
                _pressedMouseButtons.Add(mbEvt.ButtonIndex);
            }
            else
            {
                _pressedMouseButtons.Remove(mbEvt.ButtonIndex);
            }
            Update();
        }
    }

    private bool IsStateSignalConnected(TrackedStateEnum state)
    {
        return (_connectedSignalStates & state) != 0;
    }

    private void SetStateSignalConnected(TrackedStateEnum state, bool connected)
    {
        if (connected)
        {
            _connectedSignalStates |= state;
        }
        else
        {
            _connectedSignalStates &= ~state;
        }
    }

    private bool TrySetStateSignalConnected(TrackedStateEnum state, bool connected)
    {
        if (connected)
        {
            if (IsStateSignalConnected(state)) return false;
            _connectedSignalStates |= state;
            return true;
        }
        else
        {
            if (!IsStateSignalConnected(state)) return false;
            _connectedSignalStates &= ~state;
            return true;
        }
    }

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "hovered":
            case "is_hovered":
            {
                return this.OutputBooleanVariable(_hovered, ref output, input);
            }
            case "pressed":
            case "is_pressed":
            {
                return this.OutputBooleanVariable(_pressedMouseButtons.Contains(MouseButton.Left), ref output, input);
            }
            case "visible":
            case "is_visible":
            {
                return this.OutputBooleanVariable(_parentControl?.IsVisibleInTree() ?? false, ref output, input);
            }
            
            case "focused":
            case "is_focused":
            {
                return this.OutputBooleanVariable(_uiComponent?.IsAnyPlayerFocused() ?? false, ref output, input);
            }
            case "group_focused":
            case "is_group_focused":
            {
                return this.OutputBooleanVariable(_uiComponent?.IsGroupFocused() ?? false, ref output, input);
            }
            case "group_exclusively_focused":
            case "is_group_exclusively_focused":
            {
                return this.OutputBooleanVariable(_uiComponent?.IsGroupExclusivelyFocused() ?? false, ref output, input);
            }
            
            case "using_button_inputs":
            case "is_using_button_inputs":
            {
                return this.OutputBooleanVariable(_ui?.LastUsedInputType == UserInterface.NavigationInputType.ButtonsAndSticks, ref output, input);
            }
            case "using_mouse_inputs":
            case "is_using_mouse_inputs":
            {
                return this.OutputBooleanVariable(_ui?.LastUsedInputType == UserInterface.NavigationInputType.Mouse, ref output, input);
            }
            case "using_touch_inputs":
            case "is_using_touch_inputs":
            {
                return this.OutputBooleanVariable(_ui?.LastUsedInputType == UserInterface.NavigationInputType.Touch, ref output, input);
            }
        }

        return false;
    }

    [Flags]
    public enum TrackedStateEnum
    {
        Hover = 1 << 0,
        Press = 1 << 1,
        Visible = 1 << 2,
        
        Focus = 1 << 8,
        GroupFocus = 1 << 9,
        
        InputType = 1 << 16
    }
}