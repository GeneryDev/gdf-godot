using Godot;

namespace GDF.Data;

public interface IDataContextInjectable
{
    public bool CanInjectContext(StringName injectingSlotId);
    public void InjectContext(StringName slotId, IDataContext itemContext);
}