using System;

namespace GDF.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
sealed class ExposedStackableAttribute : Attribute
{
    public string Id;
    
    public ExposedStackableAttribute(string id)
    {
        Id = id;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
sealed class HasExposedStackablesAttribute : Attribute;