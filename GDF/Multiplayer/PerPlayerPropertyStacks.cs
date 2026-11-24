using GDF.Util;
using Godot;

namespace GDF.Multiplayer;

[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/per_player_property_stacks.png")]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class PerPlayerPropertyStacks : SingletonNode<PerPlayerPropertyStacks>
{
}