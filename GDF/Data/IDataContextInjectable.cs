using Godot;

namespace GDF.Data;

public interface IDataContextInjectable
{
    public StringName GetInjectableSlotId();
    public void SetContexts(IDataContext itemContext);
}