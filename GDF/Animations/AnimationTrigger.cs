using System.Text;
using GDF.Editor;
using GDF.Logical;
using Godot;
using Godot.Collections;

namespace GDF.Animations;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/animation_trigger.png")]
public partial class AnimationTrigger : TriggerableLogicNode
{
    private static readonly string[] ConfigWarningNoPlayer =
    {
        $"This node must be a child of an {nameof(AnimationPlayer)} node"
    };

    [Export(PropertyHint.EnumSuggestion,"")]
    public StringName AnimationName
    {
        get => _animationName;
        set
        {
            _animationName = value;
#if TOOLS
            if(EditorUtils.IsSettingPropertyThroughInspector(this))
                if (MatchNodeNameToAnimation)
                    SetName($"Play {value}");
#endif
            UpdateConfigurationWarnings();
        }
    }

    [Export] public bool Restart = false;
    [Export] public bool Deferred = false;

    [ExportGroup("Conditions")]
    [ExportSubgroup("Animation Player State")]
    [Export(PropertyHint.GroupEnable)] public bool ConditionAnimationPlayerState = false;
    [Export(PropertyHint.Enum, "Current Animation,Assigned Animation")] public int WhichAnimation = 0;
    [Export(PropertyHint.Enum, "Is one of,Is not any of")] public int AnimationListOperator = 0; 
    [Export] public StringName[] FromAnimationList;
    
    [ExportGroup("Auto Advance")]
    [Export(PropertyHint.GroupEnable)] public bool AutoAdvance = false;
    [Export] public float AutoAdvanceDelta = 0;

    [ExportGroup("Editor")] 
    [Export] public bool MatchNodeNameToAnimation = true;

    private StringName _animationName;

    protected override Empty Execute()
    {
        if (Deferred)
            CallDeferred(MethodName.PlayImmediate);
        else
            PlayImmediate();

        base.Execute();
        return default;
    }

    public void Trigger()
    {
        HandleTrigger();
    }

    public void Play()
    {
        HandleTrigger();
    }

    private void PlayImmediate()
    {
        var player = GetParent<AnimationPlayer>();
        if (player == null) return;

        if (ConditionAnimationPlayerState)
        {
            string current = WhichAnimation switch
            {
                0 => player.CurrentAnimation,
                1 => player.AssignedAnimation,
                _ => null
            };
            bool foundInList = false;
            if (FromAnimationList != null)
            {
                foreach (var name in FromAnimationList)
                {
                    if (current == name)
                    {
                        foundInList = true;
                        break;
                    }
                }
            }

            switch (AnimationListOperator)
            {
                case 0: // one of
                    if (!foundInList) return;
                    break;
                case 1: // none of
                    if (foundInList) return;
                    break;
            }
        }
        
        if (Restart)
            player.Stop();
        player.Play(AnimationName);
        if (AutoAdvance)
            player.Advance(AutoAdvanceDelta);
    }

    private string GetAnimationNamesHint()
    {
        if (GetParentOrNull<AnimationPlayer>() is not { } player) return null;
        string[] animNames = player.GetAnimationList();
        var sb = new StringBuilder();
        for (var i = 0; i < animNames.Length; i++)
        {
            if (i != 0) sb.Append(',');
            sb.Append(animNames[i]);
        }

        return sb.ToString();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        if (!Engine.IsEditorHint()) return;
        
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == PropertyName.AnimationName)
        {
            if (GetAnimationNamesHint() is { } animNames)
            {
                property["hint_string"] = animNames;
            }
        }

        if (propName == PropertyName.FromAnimationList)
        {
            if (GetAnimationNamesHint() is { } animNames)
            {
                property["hint"] = (int)PropertyHint.TypeString;
                property["hint_string"] = $"{Variant.Type.StringName:D}/{PropertyHint.EnumSuggestion:D}:{animNames}";
            }
        }

        property["usage"] = Variant.From(usage);
    }

    public override string[] _GetConfigurationWarnings()
    {
        var player = GetParentOrNull<AnimationPlayer>();
        if (player == null) return ConfigWarningNoPlayer;
        if (AnimationName != null && !player.HasAnimation(AnimationName))
            return new string[]
            {
                $"The parent {nameof(AnimationPlayer)} does not have an animation '{AnimationName}'"
            };
        return null;
    }
}