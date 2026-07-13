using GDF.Data;
using GDF.Util;
using Godot;

namespace GDF.Debug;

public partial class DebugLoggerInterface : Node, IDataContext
{
#if DEBUG
    public void ScrollToBottom(ScrollContainer container)
    {
        var childControl = container?.GetChildOfType<Control>();
        if (childControl != null)
        {
            container.ScrollVertical = (int)childControl.Size.Y;
        }
    }

    public bool GetSubContext(string key, string input, ref IDataContext output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "debug_logger":
            {
                output = DebugCommandSystem.DebugLogger;
                return true;
            }
        }

        return false;
    }
#endif
}