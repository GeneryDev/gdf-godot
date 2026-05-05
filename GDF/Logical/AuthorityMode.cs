using System;
using GDF.Multiplayer;
using Godot;

namespace GDF.Logical;

public enum AuthorityMode
{
    Authority,
    AnyPeer,
    NonAuthority
}

public static class AuthorityModeExt
{
    public static bool CanExecute(this AuthorityMode mode, Node node)
    {
        return mode switch
        {
            AuthorityMode.Authority => node.IsMultiplayerAuthority(),
            AuthorityMode.AnyPeer => true,
            AuthorityMode.NonAuthority => !node.IsMultiplayerAuthority(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
    public static bool CanExecuteNoTree(this AuthorityMode mode, Node node)
    {
        return mode switch
        {
            AuthorityMode.Authority => node.GetMultiplayerAuthority() == (Room.Instance?.PeerId ?? 1),
            AuthorityMode.AnyPeer => true,
            AuthorityMode.NonAuthority => node.GetMultiplayerAuthority() != (Room.Instance?.PeerId ?? 1),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}