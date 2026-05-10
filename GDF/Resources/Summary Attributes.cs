using System;

namespace GDF.Resources;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class StoreInSummaryAttribute : Attribute
{
    public bool AlsoStoreInScene = false;
}