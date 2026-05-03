using Godot;

namespace GDF.Input;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/input_auto_config.png")]
public abstract partial class GdfInputAutoConfiguration : Resource
{
    public abstract void Configure(GdfPlayerInput input);
    public abstract void ConnectUpdateSignal(Callable callable);
    public abstract void DisconnectUpdateSignal(Callable callable);
}