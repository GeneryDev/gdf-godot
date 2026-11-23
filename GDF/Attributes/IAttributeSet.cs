namespace GDF.Attributes;

public interface IAttributeSet<in TKey>
{
    public IAttribute GetAttribute(TKey id);
    
    public bool IsAttribute<T>(TKey id)
    {
        return GetAttribute(id) is T;
    }
    public bool IsAttributeComputable<T>(TKey id)
    {
        return GetAttribute(id) is IAttributeComputable<T>;
    }
    public bool IsAttributeModifiable<T>(TKey id)
    {
        return GetAttribute(id) is IModifiable<T>;
    }
}