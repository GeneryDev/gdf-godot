using GDF.Data;
using Godot;
using Godot.Collections;

namespace GDF.UI;

[Tool]
[GlobalClass]
[Icon($"{GdfConstants.IconRoot}/formatted_label.png")]
public partial class FormattedLabel : RichTextLabel, IDataContext, IDataQueryOptions
{
    [Export(PropertyHint.MultilineText)]
    public string TextFormat
    {
        get => _textFormat;
        set
        {
            _textFormat = value;
            OnPropertiesUpdated();
        }
    }

    [Export]
    public Node DataContext
    {
        get => _contextNode;
        set
        {
            (_contextNode as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.Update));
            _contextNode = value;
            (_contextNode as IDataContext)?.ConnectUpdateSignal(new Callable(this, MethodName.Update));
            OnPropertiesUpdated();
        }
    }

    [Export] public DataQueryType QueryType = DataQueryType.String;

    [ExportGroup("Post Processing")]
    [Export(PropertyHint.GroupEnable)] public bool UsePostProcessing = false;
    [Export] public LabelPostProcessor[] PostProcessors;
    [ExportGroup("")]
    
    [Export]
    public MouseFilterEnum OverrideMouseFilter
    {
        get => _overrideMouseFilter;
        set => MouseFilter = _overrideMouseFilter = value;
    }

    private string _textFormat;
    private Node _contextNode;
    private bool _updateQueued = false;
    private MouseFilterEnum _overrideMouseFilter = MouseFilterEnum.Ignore;
    private ParsedDataQuery _textQueryCache;

    private bool _sceneComplete;

    public override void _EnterTree()
    {
        base._EnterTree();
        MouseFilter = OverrideMouseFilter;
        this.AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        MarkSceneComplete(update: true);
        InvokePostProcessors();
    }

    public void Update()
    {
        if (!_updateQueued)
        {
            CallDeferred(MethodName.ExecuteUpdate);
            _updateQueued = true;
        }
    }

    private void ExecuteUpdate()
    {
        _updateQueued = false;
        Text = QueryType switch
        {
            DataQueryType.Expression => this.Evaluate(TextFormat, ref _textQueryCache, this).AsString(),
            DataQueryType.String => this.Format(TextFormat, ref _textQueryCache, this),
            DataQueryType.SubContext => TextFormat,
            DataQueryType.Collection => TextFormat,
            _ => TextFormat
        };
        InvokePostProcessors();
    }

    private void InvokePostProcessors()
    {
        if (UsePostProcessing && PostProcessors != null)
        {
            foreach (var postProcessor in PostProcessors)
            {
                postProcessor.LabelTextUpdated(this);
            }
        }
    }

    private void OnPropertiesUpdated()
    {
        if (!_sceneComplete) return;
        Update();
    }

    public void SetTextFormat(string textFormat)
    {
        TextFormat = textFormat;
    }

    private void MarkSceneComplete(bool update)
    {
        if (_sceneComplete) return;
        _sceneComplete = true;
        if (update)
            Update();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
            Update();
        if (what == NotificationPredelete)
            (_contextNode as IDataContext)?.DisconnectUpdateSignal(new Callable(this, MethodName.Update));
        if (what == NotificationResized)
        {
            if (UsePostProcessing && PostProcessors != null)
            {
                foreach (var postProcessor in PostProcessors)
                {
                    if (postProcessor.RequiresUpdateOnResize)
                    {
                        InvokePostProcessors();
                        break;
                    }
                }
            }
        }

        if (what == GdfConstants.NotificationDeepSceneInstantiated)
            MarkSceneComplete(update: true);
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var propName = property["name"].AsStringName();
        var usage = property["usage"].As<PropertyUsageFlags>();

        if (propName == Control.PropertyName.MouseFilter)
            usage &= ~PropertyUsageFlags.Editor;
        
        if (propName == Node.PropertyName.AutoTranslateMode)
            usage &= ~PropertyUsageFlags.Storage;
        
        if (propName == RichTextLabel.PropertyName.Text)
        {
            usage &= ~PropertyUsageFlags.Storage;
            usage |= PropertyUsageFlags.ReadOnly;
        }

        if (propName == PropertyName.UsePostProcessing)
        {
            usage |= PropertyUsageFlags.UpdateAllIfModified;
        }

        if (propName == PropertyName.PostProcessors)
        {
            if (!UsePostProcessing)
            {
                usage &= ~PropertyUsageFlags.Storage;
            }
        }
        
        property["usage"] = Variant.From(usage);
    }

    private static readonly StringName StringNameNormalFontSize = "normal_font_size";
    int? IDataQueryOptions.FontSize => GetThemeFontSize(StringNameNormalFontSize);
    bool? IDataQueryOptions.BbcodeEnabled => BbcodeEnabled;
    bool IDataQueryOptions.SupportsNullOperands => false;
    IDataContext IDataContext.ParentContext => DataContext as IDataContext;
}
