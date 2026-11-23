using System;

namespace GDF.Attributes.Impl;

public class InputStack : Attribute<Empty, InputGroupMode, InputStack.Reducer>
{
    public override Empty CurrentValue => default;

    protected override InputGroupMode GetInitialParam() => default;

    public bool HasControl(ModifierToken token)
    {
        if (!ModifierChain.AssertTokenOwn(token)) return false;
        var computed = GetComputed();

        int blockingTokenIndex = !computed.Token.IsEmpty ? ModifierChain.StepIndexOfToken(computed.Token) : -1;
        int requestedTokenIndex = ModifierChain.StepIndexOfToken(token);
        
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

    public class Reducer : ModifierChain<InputGroupMode>
    {
        protected override void Reduce(ref Step current, in Step incoming, float weight)
        {
            if ((incoming.Param & InputGroupMode.BlocksInput) != 0 && weight > 0)
            {
                current = incoming;
            }
        }

        public new int StepIndexOfToken(ModifierToken token)
        {
            return base.StepIndexOfToken(token);
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
    public static bool HasControl(this ModifierTokenTyped<InputStack> token)
    {
        return token.Target.HasControl(token);
    }
}