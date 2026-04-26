using Godot;

namespace GDF.Data;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/data_context.png")]
public partial class ResourceContextNode : Node, IDataContext
{
    [Signal]
    public delegate void UpdatedEventHandler();

    [Export]
    public Resource ContextResource
    {
        get => _contextResource;
        set
        {
            if (_contextResource == value) return;
            _contextResource = value;
            EmitSignalUpdated();
        }
    }
    
    private Resource _contextResource;


    public StringName UpdatedSignalName => SignalName.Updated;

    public IDataContext ParentContext => _contextResource as IDataContext;
}