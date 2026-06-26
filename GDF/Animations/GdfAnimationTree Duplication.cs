using Godot;

namespace GDF.Animations;

public partial class GdfAnimationTree
{
    [Export] public bool MakeTreeLocalToSceneDeep = false;

    private bool _localToSceneDeepAttempted = false;

    private void AttemptMakeLocalToSceneDeep()
    {
        if (_localToSceneDeepAttempted) return;
        _localToSceneDeepAttempted = true;
        TreeRoot = (AnimationRootNode)TreeRoot?.DuplicateDeep(Resource.DeepDuplicateMode.All);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == GdfConstants.NotificationDeepSceneInstantiated || what == NotificationEnterTree)
            AttemptMakeLocalToSceneDeep();
    }
}