namespace GDF.Attributes;

public interface IModifierChain
{
    public ModifierToken CreateToken(string description, float order = 0);
    public bool ClearModification(ModifierToken token);
    public void SetWeight(ModifierToken token, float weight);
    public void SetIntegrityChecker(ModifierToken token, IntegrityChecker integrityChecker);
    void CheckIntegrity(IntegrityCheckParams param);
    public bool HasModification(ModifierToken token);
    public int GetEditCount();
}
public interface IAttribute : IModifierChain
{
}
public interface IAttribute<TChain> : IAttribute
    where TChain : IModifierChain
{
    public TChain GetModifierChain();
}

public interface IModifiable<in TIn, out TParam> : IModifiable<TIn>
{
    public TParam InToParam(TIn value);

    void IModifiable<TIn>.Modify(ModifierToken token, TIn value)
    {
        if (this is IModifiable<TParam> paramModifiable)
        {
            paramModifiable.Modify(token, InToParam(value));
        }
        else
        {
            GdfLogger.Error($"Failed to call Modify on {this.GetType().Name}. Attribute type must implement {nameof(IModifiable<TParam>)} to properly implement {nameof(IModifiable<TIn,TParam>)}.");
        }
    }
}

public interface IModifiable<in T>
{
    public void Modify(ModifierToken token, T value);
}

public interface IAttributeComputable<out T>
{
    public T CurrentValue { get; }
}

public interface IModifierComputable<T>
{
    public T Compute(in T initial);
}

public delegate IntegrityCheckResult IntegrityChecker(ref IntegrityCheckParams param);
public readonly ref struct IntegrityCheckParams
{
    public bool RemoveFailures { get; init; }
}
public readonly ref struct IntegrityCheckResult
{
    public readonly bool Passed { get; init; }
    public readonly bool Failed => !Passed;
    public readonly string ResultMessage { get; init; }
}