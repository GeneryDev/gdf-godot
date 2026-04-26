using GDF.PropertyStacks.Definitions.Specialized;

namespace GDF.PropertyStacks.Extensions;

public static class InputGroupExtensions
{
    public static bool HasControl(this PropertyFrame frame, string propertyId)
    {
        if (!frame.HasProperty(propertyId)) return false;
        var topFrameData = frame.Stack.GetEffectiveValue<InputGroupProperty.FrameData>(propertyId);
        int topFrameIndex = frame.Stack.GetFrameIndex(topFrameData.BlockingHandle);
        int frameIndex = frame.GetIndex();

        if (frameIndex < topFrameIndex)
        {
            // blocked by above frame
            return false;
        }
        else if (frameIndex == topFrameIndex)
        {
            // is the top frame - see if it has control
            return (topFrameData.Mode & InputGroupMode.AcceptsInput) != 0;
        }
        else
        {
            // is above the top frame.
            // We're making the assumption that there is never a frame that neither blocks nor accepts inputs,
            // so if it didn't block, it accepts.
            return true;
        }
    }
}