using System;

namespace GDF.Editor;

[AttributeUsage(AttributeTargets.Method)]
public class InspectorCustomControlAttribute : Attribute
{
    /// <summary>
    /// If not null, replaces the inspector for the given property, if it exists. Otherwise, adds it at the end.
    /// </summary>
    public string AnchorProperty;
    public InspectorPropertyAnchorMode AnchorMode;
}

public enum InspectorPropertyAnchorMode
{
    Before,
    After,
    Replace
}