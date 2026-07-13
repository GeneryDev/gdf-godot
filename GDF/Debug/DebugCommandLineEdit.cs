using Godot;

namespace GDF.Debug;

#if DEBUG
public partial class DebugCommandLineEdit : LineEdit
{
    [Signal]
    public delegate void CloseRequestedEventHandler();

    private int _historyPosition = 0;
    

    public override void _Ready()
    {
        base._Ready();
        TextSubmitted += Submit;
        TextChanged += OnTextChanged;
    }

    private void OnTextChanged(string newText)
    {
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
        else if (@event is InputEventKey { Pressed: true, Keycode: Key.Up })
        {
            NavigateHistory(+1);
            GetTree().Root.SetInputAsHandled();
        }
        else if (@event is InputEventKey { Pressed: true, Keycode: Key.Down })
        {
            NavigateHistory(-1);
            GetTree().Root.SetInputAsHandled();
        }
        else if (@event is InputEventKey { Pressed: true, Keycode: Key.Tab })
        {
            SuggestCommand();
            GetTree().Root.SetInputAsHandled();
        }
    }

    private void SuggestCommand()
    {
        if (DebugCommandSystem.AutocompleteCommand(Text, out var suggested))
        {
            Text = suggested;
            this.CaretColumn = Text.Length;
        }
    }

    private void NavigateHistory(int dir)
    {
        var history = DebugCommandSystem.GetCommandHistory();
        
        _historyPosition += dir;
        _historyPosition = Mathf.Clamp(_historyPosition, 0, history.Count);

        var newText = "";
        if (_historyPosition != 0)
        {
            newText = history[^_historyPosition];
        }

        Text = newText;
        this.CaretColumn = Text.Length;
    }

    private void Submit()
    {
        Submit(Text);
    }

    private void Submit(string command)
    {
        if (command == null) return;
        DebugCommandSystem.SubmitCommand(command, appendToHistory: true);
        Clear();
    }
}
#endif