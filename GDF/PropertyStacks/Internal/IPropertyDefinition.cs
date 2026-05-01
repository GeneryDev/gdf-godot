using Godot;

namespace GDF.PropertyStacks.Internal;

public interface IPropertyDefinition
{
    public IProperty CreateProperty();
}

public interface IPropertyDefinition<TMed, TOut, TCache> : IPropertyDefinition
{
    public TMed GetInitialValue(TCache cache);
    public TMed Reduce(TMed lower, TMed higher, float weight, PropertyFrameHandle handle);
    public TOut IntermediateToOutput(TMed value);
    public Variant OutputToVariant(TOut value);

    public virtual Variant IntermediateToDebug(TMed value)
    {
        return OutputToVariant(IntermediateToOutput(value));
    }

    IProperty IPropertyDefinition.CreateProperty()
    {
        return new PropertyImpl<TMed, TOut, TCache>(this);
    }

    TCache CreateCache();
    
    public bool IsInheritable();
}

public interface IPropertyAcceptsInput<in TIn, out TMed>
{
    public TMed InputToIntermediate(TIn input);
}

public interface IPropertyComputableAsVariant
{
    public Variant ComputeAsVariant();
}