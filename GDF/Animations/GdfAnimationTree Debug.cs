using System.Text;

namespace GDF.Animations;

public partial class GdfAnimationTree
{
    public void GetDebugInfo(StringBuilder sb)
    {
        EnsureScanStillValid();
        foreach (var (key, playback) in _stateMachinePlaybacks)
        {
            sb.Append(key);
            sb.Append(": ");
            sb.Append(playback.GetCurrentNode());
            if (playback.IsPlaying())
            {
                sb.Append(" [active]");
            }
            else
            {
                sb.Append(" [inactive]");
            }
            sb.Append($" [{playback.GetCurrentPlayPosition():N2}/{playback.GetCurrentLength():N2}]");
            sb.AppendLine();
        }
    }

    public string GetDebugInfo()
    {
        var sb = new StringBuilder();
        GetDebugInfo(sb);
        return sb.ToString();
    }
}