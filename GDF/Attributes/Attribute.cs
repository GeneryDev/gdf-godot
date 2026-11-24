using System.Runtime.InteropServices;

namespace GDF.Attributes;

public abstract class Attribute<TValue, TParam, TChain> :
    IAttribute,
    IAttribute<TChain>,
    IModifiable<TParam>,
    IAttributeComputable<TValue>,
    IModifierComputable<ModifierChain<TParam>.Step>
    where TChain :
        ModifierChain<TParam>,
        IModifierComputable<ModifierChain<TParam>.Step>,
        IModifiable<TParam>,
        new()
{
    public abstract TValue CurrentValue { get; }
    
    private ModifierChain<TParam>.Step _computedStep;
    public readonly TChain ModifierChain;
    
    private int _editCount;
    private int _computedEditCount;
    private bool _computedDirty = true;

    public Attribute()
    {
        ModifierChain = new TChain();
    }

    protected abstract TParam GetInitialParam();

    protected ModifierChain<TParam>.Step GetComputed()
    {
        if (_computedDirty || ModifierChain.GetEditCount() != _computedEditCount) Recompute();
        return _computedStep;
    }

    public void Recompute()
    {
        var initialStep = new ModifierChain<TParam>.Step()
        {
            Param = GetInitialParam(),
            Weight = 1.0f
        };
        var result = ModifierChain.Compute(initialStep);
        _computedStep = result;
        _computedEditCount = ModifierChain.GetEditCount();
        _computedDirty = false;
    }

    protected void MarkDirty()
    {
        _editCount++;
        _computedDirty = true;
    }

    public void Modify(ModifierToken token, TParam value)
    {
        ModifierChain.Modify(token, value);
    }

    public ModifierTokenTyped<TChain> CreateToken(string description, float order = 0)
    {
        return ModifierChain.CreateToken(description, order);
    }

    ModifierToken IModifierChain.CreateToken(string description, float order)
    {
        return ModifierChain.CreateToken(description, order);
    }

    public bool ClearModification(ModifierToken token)
    {
        return ModifierChain.ClearModification(token);
    }

    public void SetWeight(ModifierToken token, float weight)
    {
        ModifierChain.SetWeight(token, weight);
    }

    public void SetIntegrityChecker(ModifierToken token, IntegrityChecker integrityChecker)
    {
        ModifierChain.SetIntegrityChecker(token, integrityChecker);
    }

    public void CheckIntegrity(IntegrityCheckParams param)
    {
        ModifierChain.CheckIntegrity(param);
    }

    public bool HasModification(ModifierToken token)
    {
        return ModifierChain.HasModification(token);
    }

    public int GetEditCount()
    {
        return ModifierChain.GetEditCount() + _editCount;
    }

    public TChain GetModifierChain()
    {
        return ModifierChain;
    }

    ModifierChain<TParam>.Step IModifierComputable<ModifierChain<TParam>.Step>.Compute(in ModifierChain<TParam>.Step initial)
    {
        return ModifierChain.Compute(initial);
    }
}
