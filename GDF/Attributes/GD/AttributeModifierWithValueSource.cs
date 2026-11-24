using GDF.Logical.Values;
using Godot;
using Godot.Collections;

namespace GDF.Attributes;

[Tool]
[GlobalClass]
public partial class AttributeModifierWithValueSource : AttributeModifierResource, IModifyInvoker
{
    [Export] public ValueSource OperandSource;

    private Node _sourceNode;
    
    public new void Modify(ModifierToken token)
    {
        if (!TryModify(token))
        {
            GdfLogger.Error($"Failed to modify attribute using AttributeModifierWithValueSource; The combination of type '{Type}' and function '{Function}' is not supported by ModifierChain type '{token.Target.GetType()}'");
        }
    }
    
    public new bool TryModify(ModifierToken token)
    {
        Operand = OperandSource?.GetValue(_sourceNode) ?? default;
        return base.TryModify(token);
    }

    public void SetSourceNode(Node source)
    {
        _sourceNode = source;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyNameOperand)
        {
            usage &= ~PropertyUsageFlags.Editor;
        }
        else if (propName == AttributeModifierResource.PropertyName._operand)
        {
            usage &= ~PropertyUsageFlags.Editor;
        }

        property["usage"] = Variant.From(usage);
    }
}