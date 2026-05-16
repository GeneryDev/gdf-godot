namespace GDF.Animations;

public partial class GdfAnimationTree
{
    public void UpdateExpressions(double delta)
    {
        var baseNodePath = AdvanceExpressionBaseNode;
        var exprBaseNode = baseNodePath.IsEmpty ? this : GetNode(baseNodePath);
        foreach (var (key, (node, meta)) in _animNodePathsToMetadataNodes)
        {
            if (!meta.HasExpression()) continue;
            var targetValue = meta.GetParsedExpression().Execute(baseInstance: exprBaseNode);
            var currentValue = targetValue;
            if (meta.BlendSpeed > 0)
            {
                currentValue = Get(key);
                currentValue = meta.Blend(currentValue, targetValue, delta);
            }
            // GD.Print($"Updated {key} to result of {expr}: {value}");
            Set(key, currentValue);
        }
    }
}