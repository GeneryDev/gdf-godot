using System;
using Godot;
using Array = Godot.Collections.Array;

namespace GDF.Networking;

public partial class GdfRpcSystem
{
    private const int MaxArgCount = 16; // cannot exceed sizeof(ulong)/ArgFlagBitCount
    
    private Array _tempArray = new();
    
    private bool SerializeArgs(out Array outArgs, out ulong outArgFlags, params Variant[] args)
    {
        if (args.Length > MaxArgCount)
        {
            GD.PushError($"GdfRpc does not support more than {MaxArgCount} arguments, got {args.Length}");
            outArgs = null;
            outArgFlags = 0;
            return false;
        }

        _tempArray.Clear();
        ulong argFlags = 0;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            RpcArgumentFlags thisArgFlags = 0;
            if (arg.VariantType == Variant.Type.Object && arg.AsGodotObject() is Node argNode)
            {
                thisArgFlags |= RpcArgumentFlags.IsNode;
                arg = argNode.GetPath();
            }

            if (arg.VariantType == Variant.Type.Object && arg.AsGodotObject() is Resource argResource &&
                !string.IsNullOrEmpty(argResource.ResourcePath))
            {
                GD.PushError(
                    "GdfRpc does not support passing resources via parameters due to security risks. Look into using ResourceLibrary Descriptors");
                outArgs = default;
                outArgFlags = default;
                return false;
            }

            argFlags |= (ulong)thisArgFlags << (i * ArgFlagBitCount);
            _tempArray.Add(arg);
        }

        outArgs = _tempArray;
        outArgFlags = argFlags;
        return true;
    }

    private void DeserializeArgs(Array args, ulong argFlags)
    {
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            var thisArgFlags = (RpcArgumentFlags)((argFlags >> (i * ArgFlagBitCount)) & ((1 << ArgFlagBitCount) - 1));
            if ((thisArgFlags & RpcArgumentFlags.IsNode) != 0) arg = GetTree().Root.GetNodeOrNull(arg.AsNodePath());

            args[i] = arg;
        }
    }

    [Flags]
    private enum RpcArgumentFlags
    {
        IsNode = 1 << 0
    }

    private const int ArgFlagBitCount = 1;
}