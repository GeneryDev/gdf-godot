using System;

namespace GDF.DocHints;

/// <summary>
/// Indicates that an exported property is only useful during the initialization of the containing object,
/// and that setting this property any time after then will have no effect.
/// For example, a property might only be read once on a node's _Ready, and further changes to the property
/// won't be reflected in the script's behavior.
/// </summary>
[System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public class UsedOnlyAtInitializationAttribute : Attribute
{
    public CommonAccessTimes[] AccessTimes;
    public string Description;

    public UsedOnlyAtInitializationAttribute()
    {
    }

    public UsedOnlyAtInitializationAttribute(CommonAccessTimes[] accessTimes)
    {
        AccessTimes = accessTimes;
    }

    public UsedOnlyAtInitializationAttribute(string description)
    {
        Description = description;
    }

    public UsedOnlyAtInitializationAttribute(CommonAccessTimes[] accessTimes, string description)
    {
        AccessTimes = accessTimes;
        Description = description;
    }
}

public enum CommonAccessTimes
{
    SceneInstantiation,
    EnterTree,
    Ready,
    Other
}