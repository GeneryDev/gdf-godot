using GDF.Data;
using GDF.Util;
using Godot;

namespace GDF.UI;

public partial class UserInterfaceGroup : IDataContext
{
    [Signal]
    public delegate void DataContextUpdatedEventHandler();

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "has_focus":
            {
                return this.OutputBooleanVariable(HasFocus(), ref output, input);
            }
            case "has_exclusive_focus":
            {
                return this.OutputBooleanVariable(HasExclusiveFocus(), ref output, input);
            }
        }

        return false;
    }

    public void ConnectUpdateSignal(Callable callable)
    {
        this.TryConnect(SignalName.GroupFocusEntered, callable);
        this.TryConnect(SignalName.GroupFocusExited, callable);
        this.TryConnect(SignalName.ExclusiveGroupFocusEntered, callable);
        this.TryConnect(SignalName.ExclusiveGroupFocusExited, callable);
    }

    public void DisconnectUpdateSignal(Callable callable)
    {
        this.TryDisconnect(SignalName.GroupFocusEntered, callable);
        this.TryDisconnect(SignalName.GroupFocusExited, callable);
        this.TryDisconnect(SignalName.ExclusiveGroupFocusEntered, callable);
        this.TryDisconnect(SignalName.ExclusiveGroupFocusExited, callable);
    }
}