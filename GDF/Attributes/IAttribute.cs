namespace GDF.Attributes;

public interface IAttribute
{
    public AttributeModifierToken CreateToken(string description, float order = 0);
    public bool ClearModification(AttributeModifierToken token);
    public void SetWeight(AttributeModifierToken token, float weight);
    public void SetIntegrityChecker(AttributeModifierToken token, IntegrityChecker integrityChecker);
    void CheckIntegrity(IntegrityCheckParams param);
    public bool HasModification(AttributeModifierToken token);
    public int GetEditCount();
}

public interface IAttributeModifiable<in TIn, out TParam> : IAttributeModifiable<TIn>
{
    public TParam InToParam(TIn value);

    void IAttributeModifiable<TIn>.Modify(AttributeModifierToken token, TIn value)
    {
        if (this is IAttributeModifiable<TParam> paramModifiable)
        {
            paramModifiable.Modify(token, InToParam(value));
        }
        else
        {
            GdfLogger.Error($"Failed to call Modify on {this.GetType().Name}. Attribute type must implement {nameof(IAttributeModifiable<TParam>)} to properly implement {nameof(IAttributeModifiable<TIn,TParam>)}.");
        }
    }
}

public interface IAttributeModifiable<in T>
{
    public void Modify(AttributeModifierToken token, T value);
}

public interface IAttributeComputable<out T>
{
    public T CurrentValue { get; }
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