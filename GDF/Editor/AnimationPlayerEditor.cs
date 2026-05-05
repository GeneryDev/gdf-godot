using System;
using Godot;

namespace GDF.Editor;

public partial class AnimationPlayerEditor : GodotObject
{
    public static AnimationPlayerEditor Instance { get; private set; } 
    
    [Signal]
    public delegate void FrameChangedEventHandler(bool open, string animName, double time);

    private Control _editorControl;

    private Control _topBar;
    private SpinBox _timeControl;
    private OptionButton _animationPicker;
    
    public double CurrentTime
    {
        get => _timeControl?.Value ?? 0;
        set
        {
            if(_timeControl != null)
                _timeControl.Value = value;
        }
    }
    public string CurrentAnimationName
    {
        get => _animationPicker.Selected == -1 ? null : _animationPicker.GetItemText(_animationPicker.Selected);
        set
        {
            if (_animationPicker == null) return;
            for (int i = 0; i < _animationPicker.ItemCount; i++)
            {
                if (_animationPicker.GetItemText(i) == value)
                {
                    _animationPicker.Selected = i;
                    return;
                }
            }
        }
    }

    public bool IsOpen => _editorControl.IsVisible();

    private FrameInfo CurrentFrameInfo => !IsOpen ? default : new FrameInfo
    {
        Open = true,
        Time = CurrentTime,
        AnimationName = CurrentAnimationName
    };

    public bool IsValid => _editorControl != null;

    private FrameInfo _prevFrameInfo;

    public AnimationPlayerEditor()
    {
        Instance = this;
        GD.Print("Re-created animation player editor");
    }

    public AnimationPlayerEditor(Control editorControl)
    {
        SetEditorControl(editorControl);
        GD.Print("Created animation player editor!");
        GD.Print(_editorControl.GetPropertyList());
        Instance = this;
    }

    public void Validate()
    {
        if (_editorControl == null)
        {
            GD.PushWarning($"Lost {nameof(_editorControl)}!");
            return;
        }
        if (_topBar == null)
        {
            GD.PushWarning($"Lost {nameof(_topBar)}!");
            return;
        }
        if (_timeControl == null)
        {
            GD.PushWarning($"Lost {nameof(_timeControl)}!");
            return;
        }
        if (_animationPicker == null)
        {
            GD.PushWarning($"Lost {nameof(_animationPicker)}!");
            return;
        }
    }

    public void SetEditorControl(Control editorControl)
    {
        this._editorControl = editorControl;
        FindControls();
    }

    private void FindControls()
    {
        _topBar = _editorControl.GetChild<Control>(0);
        GD.Print($"topBar: {_topBar.Name}");

        foreach (var child in _topBar.GetChildren())
        {
            if (child is SpinBox spinBox)
            {
                GD.Print($"Found spin box: {spinBox.Name}");
                _timeControl ??= spinBox;
            }

            if (child is OptionButton optionButton)
            {
                GD.Print($"Found option button: {optionButton.Name}");
                _animationPicker ??= optionButton;
            }
        }
    }

    public void _Process(double delta)
    {
        var currentFrameInfo = CurrentFrameInfo;
        if (currentFrameInfo != _prevFrameInfo)
        {
            this.EmitSignal(SignalName.FrameChanged,
                currentFrameInfo.Open,
                currentFrameInfo.AnimationName,
                currentFrameInfo.Time);
            // this.FrameChanged?.Invoke(currentFrameInfo);
            _prevFrameInfo = currentFrameInfo;
        }
    }
    
    private struct FrameInfo : IEquatable<FrameInfo>
    {
        public bool Open;
        public double Time;
        public string AnimationName;

        public bool Equals(FrameInfo other)
        {
            return Open == other.Open && Time.Equals(other.Time) && AnimationName == other.AnimationName;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Open, Time, AnimationName);
        }
        
        public static bool operator ==(FrameInfo a, FrameInfo b)
        {
            return a.Open == b.Open && a.Time == b.Time && a.AnimationName == b.AnimationName;
        }

        public static bool operator !=(FrameInfo a, FrameInfo b)
        {
            return !(a == b);
        }
    }
}