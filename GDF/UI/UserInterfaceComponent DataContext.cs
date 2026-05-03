using GDF.Data;
using Godot;

namespace GDF.UI;

public partial class UserInterfaceComponent : IDataContext
{
    [Signal]
    public delegate void DataContextUpdatedEventHandler();

    public IDataContext ParentContext => _focusInterface;
    StringName IDataContext.UpdatedSignalName => SignalName.DataContextUpdated;
}