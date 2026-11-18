using System;
using Godot;

namespace GDF.Attributes;

public class InputStack : StackableValue<Empty, InputGroupMode>
{
    public override Empty CurrentValue => default;

    protected override InputGroupMode GetInitialParam() => default;

    protected override Modification Reduce(in Modification current, in Modification incoming, float weight)
    {
        if ((incoming.Param & InputGroupMode.BlocksInput) != 0 && weight > 0)
        {
            return incoming;
        }
        return current;
    }

    public bool HasControl(StackableValueToken token)
    {
        if (!AssertTokenOwn(token)) return false;
        var computed = GetComputed();

        int blockingTokenIndex = !computed.Token.IsEmpty ? ModIndexOfToken(computed.Token) : -1;
        int requestedTokenIndex = ModIndexOfToken(token);
        
        if (requestedTokenIndex < blockingTokenIndex)
        {
            // blocked by above token
            return false;
        }
        else if (requestedTokenIndex == blockingTokenIndex)
        {
            // is the blocking token - see if it has control
            return (computed.Param & InputGroupMode.AcceptsInput) != 0;
        }
        else
        {
            // is above the blocking token.
            // We're making the assumption that there is never a token that neither blocks nor accepts inputs,
            // so if it didn't block, it accepts.
            return true;
        }
    }
}

[Flags]
public enum InputGroupMode
{
    AcceptsInput = 1,
    BlocksInput = 2,
    
    Disable = BlocksInput,
    Capture = AcceptsInput | BlocksInput,
    PassThrough = AcceptsInput,
}

public static class InputStackExtensions
{
    public static void Set(this StackableValue<Empty, InputGroupMode> target, StackableValueToken token, InputGroupMode inputGroupMode)
    {
        target.Modify(token, inputGroupMode);
    }
    public static StackableValueTokenTyped<Empty, InputGroupMode> Set(this StackableValueTokenTyped<Empty, InputGroupMode> token, InputGroupMode inputGroupMode)
    {
        return token.Modify(inputGroupMode);
    }
    
    public static bool HasControl(this StackableValueTokenTyped<Empty, InputGroupMode> token)
    {
        if (token.Target is InputStack stack)
            return stack.HasControl(token);
        GD.PrintErr("Called HasControl on a token not belonging to an input stack!");
        return false;
    }
}