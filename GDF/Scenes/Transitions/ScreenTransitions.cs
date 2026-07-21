using GDF.Resources;

namespace GDF.Scenes.Transitions;

[LibraryAccessibleInEditor]
public partial class ScreenTransitions : SceneResourceLibrary<ScreenTransition>
{
    public override LibraryConfig GetLibraryConfig()
    {
        return new LibraryConfig()
        {
            Roots = new[]
            {
                new LibraryConfig.LibraryRoot("res://scenes/ui/screen_transitions")
            }
        };
    }
}