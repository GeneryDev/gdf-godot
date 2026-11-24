using GDF.Data;
using Godot;

namespace GDF.Attributes;

public partial class AttributeWatcher : IDataContext
{
    StringName IDataContext.UpdatedSignalName => SignalName.Updated;

    bool IDataContext.GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        foreach (var watched in _observations)
        {
            if (watched.AttributeId != key ||
                watched.Attribute is not IAttributeComputable<Variant> computable) continue;
            output = computable.CurrentValue;
            return true;
        }

        return false;
    }
}