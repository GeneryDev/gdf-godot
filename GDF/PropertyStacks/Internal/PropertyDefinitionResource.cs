using Godot;

namespace GDF.PropertyStacks.Internal;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/property_stack_property.png")]
public abstract partial class PropertyDefinitionResource : Resource, IPropertyDefinition
{
    private string _propertyId;

    [Export]
    public string PropertyId
    {
        get => _propertyId;
        set
        {
            _propertyId = value;
            ResourceName = $"{_propertyId} [{GetType().Name}]";
        }
    }
    public abstract IProperty CreateProperty();
    [Export] public bool Inheritable;

    public bool IsInheritable() => Inheritable;
}
