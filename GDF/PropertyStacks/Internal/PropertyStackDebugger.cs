using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.PropertyStacks.Internal;

public partial class PropertyStackDebugger
{
    private Dictionary _effectiveValues;
    private Dictionary<string, int> _modCounts;

    private Dictionary _output;

    public Dictionary GetOutput(PropertyStack stack, bool logValueChanges, bool dumpFrameData)
    {
        _output ??= new Dictionary();
        
        _effectiveValues ??= new Dictionary();
        _modCounts ??= new Dictionary<string, int>();
        foreach (string propertyId in stack.PropertyIds)
        {
            int modCount = stack.GetModCount(propertyId);
            if (_modCounts.TryGetValue(propertyId, out int prevModCount) && prevModCount == modCount)
            {
                // mod count is the same, no change
            }
            else
            {
                var currentValue = stack.GetEffectiveValue(propertyId);
                if (_effectiveValues.TryGetValue(propertyId, out var prevValue) && prevValue.VariantEquals(currentValue))
                {
                    // no change
                }
                else
                {
                    if (logValueChanges)
                    {
                        GD.Print($"Property '{propertyId}' changed from {prevValue} to {currentValue}");
                    }
                    _effectiveValues[propertyId] = currentValue;
                }

                _modCounts[propertyId] = modCount;
            }
        }

        _output["effective_values"] = _effectiveValues;
        
        if (dumpFrameData)
        {
            _output["stack"] = stack.GetFrameDebugInfo();
        }

        return _output;
    }
}