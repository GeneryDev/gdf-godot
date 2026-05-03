using GDF.Data;

namespace GDF.UI;

public partial class UserInterface : IDataContext
{
    public bool GetSubContext(string key, string input, ref IDataContext output, IDataQueryOptions options)
    {
        switch (key)
        {
            case "interface_input_context":
            {
                // TODO
                // output = new InterfaceInputDataContext(this).Boxed();
                return false;
            }
        }

        return false;
    }
}