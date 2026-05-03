using Godot;

namespace GDF.Util;

public static class ExpressionUtil
{
    public static Expression Parse(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        var expr = new Expression();
        var error = expr.Parse(raw);
        if (error != Error.Ok)
        {
            var msg = $"Error parsing expression. Message: {expr.GetErrorText()}. Expression: {raw}";
            if (Engine.IsEditorHint()) GD.PushWarning(msg);
            else GD.PrintErr(msg);
            expr = null;
        }

        return expr;
    }
}