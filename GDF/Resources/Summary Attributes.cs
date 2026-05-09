using System;

namespace GDF.Resources;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class IncludeInSummaryAttribute : Attribute
{
    public bool DisableStorage = true;
}