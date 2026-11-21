using GDF.Data;
using Godot;

namespace GDF.Attributes;

public partial class AttributeWatcher : IDataContext
{
    StringName IDataContext.UpdatedSignalName => SignalName.Updated;

    bool IDataContext.GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        if (_observedAttributes.TryGetValue(key, out var attr) && attr is IAttributeComputable<Variant> computable)
        {
            output = computable.CurrentValue;
            return true;
        }

        return false;
    }
}