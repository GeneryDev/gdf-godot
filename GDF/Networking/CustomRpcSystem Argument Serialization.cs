using System;
using Godot;
using Array = Godot.Collections.Array;

namespace GDF.Networking;

public partial class CustomRpcSystem
{
    private Array _tempArray = new();
    
    private bool SerializeArgs(out Array outArgs, out uint outArgFlags, params Variant[] args)
    {
        if (args.Length > 8)
        {
            GD.PushError($"CustomRpc does not support more than 8 arguments, got {args.Length}");
            outArgs = null;
            outArgFlags = 0;
            return false;
        }

        _tempArray.Clear();
        uint argFlags = 0;
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
                    "CustomRpc no longer supports passing resources via parameters due to security risks. Look into using ResourceLibrary Descriptors");
                outArgs = default;
                outArgFlags = default;
                return false;
            }

            argFlags |= (uint)thisArgFlags << (i * ArgFlagBitCount);
            _tempArray.Add(arg);
        }

        outArgs = _tempArray;
        outArgFlags = argFlags;
        return true;
    }

    private void DeserializeArgs(Array args, uint argFlags)
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

    private const int ArgFlagBitCount = 4;
}