using GDF.Data;
using Godot;

namespace GDF.PropertyStacks;

public partial class PropertyStackWatcher : IDataContext
{
    public StringName UpdatedSignalName => SignalName.Updated;

    public bool GetContextVariable(string key, string input, ref Variant output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "property":
            {
                if (_prevObservedStates.TryGetValue(input, out var state))
                {
                    output = state.Value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        return false;
    }
}