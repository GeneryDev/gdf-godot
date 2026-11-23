using System;

namespace GDF.Attributes.Impl;

public class InputStack : Attribute<Empty, InputGroupMode>
{
    public override Empty CurrentValue => default;

    protected override InputGroupMode GetInitialParam() => default;

    protected override void Reduce(ref Modification current, in Modification incoming, float weight)
    {
        if ((incoming.Param & InputGroupMode.BlocksInput) != 0 && weight > 0)
        {
            current = incoming;
        }
    }

    public bool HasControl(AttributeModifierToken token)
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
    public static bool HasControl(this AttributeModifierTokenTyped<InputStack> token)
    {
        return token.Target.HasControl(token);
    }
}