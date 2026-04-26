using Godot;
using Godot.Collections;

namespace GDF.Logical.Values;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/constant.png")]
public partial class ConstantValue : ValueSource
{
    [Export]
    public Variant Value;

#if TOOLS
    [ExportGroup("Input as Expression")]
    [Export(PropertyHint.Expression)]
    public string Expression
    {
        get => _lastTypedExpr;
        set
        {
            _lastTypedExpr = value;
            CallDeferred(MethodName.SetValueAsExpression, value);
        }
    }

    [Export(PropertyHint.Expression)] public string ExpressionOutput = "";

    private string _lastTypedExpr = "";

    private void SetValueAsExpression(string rawExpr)
    {
        ExpressionOutput = "";
        _lastTypedExpr = rawExpr;
        if (string.IsNullOrEmpty(rawExpr)) return;
        if (!Engine.IsEditorHint()) return;
        var expr = new Expression();
        var err = expr.Parse(rawExpr);
        if (err == Error.Ok)
        {
            var result = expr.Execute(showError: false);
            if (!expr.HasExecuteFailed())
            {
                var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
                undoRedo.CreateAction("Set Constant Value with Expression", UndoRedo.MergeMode.Ends, customContext: this);
                undoRedo.AddDoProperty(this, PropertyName.Value, result);
                undoRedo.AddUndoProperty(this, PropertyName.Value, Value);
                undoRedo.CommitAction();
                ExpressionOutput = $"Value set: {result}";
                return;
            }
        }
        ExpressionOutput = expr.GetErrorText();
    }
#endif

    public ConstantValue()
    {
    }

    public ConstantValue(Variant value)
    {
        Value = value;
    }

    public override Variant GetValue(Node source)
    {
        return Value;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.Expression)
        {
            usage &= ~(PropertyUsageFlags.Storage);
            property["usage"] = Variant.From(usage);
        }
        if (propName == PropertyName.ExpressionOutput)
        {
            usage &= ~(PropertyUsageFlags.Storage);
            usage |= PropertyUsageFlags.ReadOnly;
            property["usage"] = Variant.From(usage);
        }
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}