using Godot;

namespace GDF.Debug;

public partial class DebugCommandLineEdit : LineEdit
{
    [Signal]
    public delegate void CloseRequestedEventHandler();

    public override void _Ready()
    {
        base._Ready();
        TextSubmitted += Submit;
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);
        if (@event is InputEventAction evtAction)
        {
            if (evtAction.Action == "ui_cancel")
                EmitSignalCloseRequested();
            // doesn't work, LineEdit handles ui_cancel by default
        }
        else if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            EmitSignalCloseRequested();
            GetTree().Root.SetInputAsHandled();
        }
    }

    private void Submit()
    {
        Submit(Text);
    }

    private void Submit(string command)
    {
        if (command == null) return;
        DebugCommandSystem.SubmitCommand(command);
        Clear();
    }
}