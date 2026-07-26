using GDF.Util;
using Godot;

namespace GDF.UI;

[GlobalClass]
[SingletonUsage(SingletonUsage.Autoload)]
public partial class GdfViewportResizer : SingletonNode<GdfViewportResizer>
{
	[Signal]
	public delegate void UserSettingsChangedEventHandler();
	
	// Controls by how many times the base viewport size the window must be resized
	// for the UI to readjust to the new scale.
	// A value of 1.0f means the first rescale happens at 2x base size.
	// A value of 0.5f means the first rescale happens at 1.5x base size.
	// A value of 0.25f means the first rescale happens at 1.25x base size.
	// etc.
	[Export(PropertyHint.Range, "0.1,1,0.01")]
	public float ScalingStepSize = 1.0f;

	[Export] public float MinScale = 1.0f;

	private Vector2I _baseSize = new(1280, 720);
	private GdfViewportUserSettings _userSettings = new();
	private bool _settingSignalsConnected = false;

	[Export]
	public GdfViewportUserSettings UserSettings
	{
		get => _userSettings;
		set
		{
			if (_userSettings == value) return;
			DisconnectUserSettingSignals();
			_userSettings = value;
			if (IsInsideTree())
				ConnectUserSettingSignals();
		}
	}

	public override void _Ready()
	{
		_baseSize = new Vector2I(
			ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32(),
			ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32()
		);
		var window = GetWindow();
		window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
		window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand; // Overridden by settings
		window.ContentScaleStretch = Window.ContentScaleStretchEnum.Fractional;
		
		GetViewport().SizeChanged += () => CallDeferred(MethodName.UpdateLastUsedWindowMode);
		window.SizeChanged += () => CallDeferred(MethodName.OnWindowResized);
		CallDeferred(MethodName.UpdateMajor);
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		ConnectUserSettingSignals();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		DisconnectUserSettingSignals();
	}

	private void OnUserSettingChanged(string propertyName, Variant newValue)
	{
		if (!IsInsideTree()) return;
		UpdateMajor();
		EmitSignalUserSettingsChanged();
	}

	public void ToggleFullscreen()
	{
		UserSettings.FullscreenMode = UserSettings.FullscreenMode == default
			? UserSettings.LastUsedFullscreenMode
			: default;
	}

	private void OnViewportSizeChanged()
	{
		CallDeferred(MethodName.UpdateLastUsedWindowMode);
	}

	private void UpdateLastUsedWindowMode()
	{
		var windowMode = DisplayServer.Singleton.WindowGetMode();
		if (windowMode is not (DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen))
		{
			if (UserSettings.LastUsedWindowedMode != windowMode)
			{
				// windowed/maximized mode changed
				UserSettings.LastUsedWindowedMode = windowMode;
				OnWindowedMaximizedModeChanged();
			}

			if (UserSettings.FullscreenMode != default)
			{
				// got kicked out of full screen mode
				UserSettings.FullscreenMode = default;
				OnFullscreenModeExited();
			}
		}
		else
		{
			if (UserSettings.LastUsedFullscreenMode != windowMode)
			{
				// fullscreen mode changed
				UserSettings.LastUsedFullscreenMode = windowMode;
				OnFullscreenModeChanged();
			}
		}
	}

	private void OnWindowResized()
	{
		// GD.Print($"Window resized: {GetWindow().Size}");
		UpdateMinor();
	}

	private void OnWindowedMaximizedModeChanged()
	{
		// GD.Print("Windowed/Maximized mode changed");
		UpdateMajor();
	}

	private void OnFullscreenModeExited()
	{
		// GD.Print("Fullscreen mode exited");
		UpdateMajor();
	}

	private void OnFullscreenModeChanged()
	{
		// GD.Print("Fullscreen mode changed");
		UpdateMajor();
	}

	private void UpdateMinor()
	{
		// Change parameters of how the inside of the window is drawn. Called when the window is resized,
		// and after major updates.
		var window = GetWindow();
		var windowSize = window.Size;
		var resolution = UserSettings.Resolution;
		
		if (UserSettings.Resolution == default)
		{
			// Expand
			resolution = windowSize;
		}

		float scale = Mathf.Min(
			Mathf.Min(
				Mathf.Max(MinScale, Mathf.Floor((float)resolution.X / _baseSize.X / ScalingStepSize) * ScalingStepSize),
				Mathf.Max(MinScale, Mathf.Floor((float)resolution.Y / _baseSize.Y / ScalingStepSize) * ScalingStepSize)
			),
			4
		);
		window.ContentScaleFactor = scale;
		window.ContentScaleSize = resolution;
	}

	private void UpdateMajor()
	{
		// Change parameters of the window. Should only be called on singular detected changes such as settings changes,
		// which cannot be triggered as a side effect of this function (to avoid calling this function on loop)
		var window = GetWindow();
		
		var mode = UserSettings.FullscreenMode;
		if (mode == default) mode = UserSettings.LastUsedWindowedMode;

		bool expand = UserSettings.Resolution == default;
		
		window.ContentScaleAspect = expand ? Window.ContentScaleAspectEnum.Expand : Window.ContentScaleAspectEnum.Keep;

		if (DisplayServer.Singleton.WindowGetMode() is not DisplayServer.WindowMode.Fullscreen)
			window.Unresizable = false;
		
		// GD.Print($"Set mode {mode} via UpdateMajor");
		DisplayServer.Singleton.WindowSetMode(mode);

		if (mode is DisplayServer.WindowMode.Windowed)
			window.Unresizable = !expand;
		
		if (UserSettings.Resolution != default &&
		    mode is not (DisplayServer.WindowMode.Maximized or DisplayServer.WindowMode.Fullscreen
			    or DisplayServer.WindowMode.ExclusiveFullscreen))
		{
			// GD.Print($"Set size {UserSettings.Resolution} via UpdateMajor");
			GetWindow().Size = UserSettings.Resolution;

			if (mode is DisplayServer.WindowMode.Windowed)
			{
				var estimatedDecorationSize = new Vector2I(0, 24);
				// Note: GetSizeWithDecorations returns inaccurate values if the window size is larger than the screen it's in (seems like it's clamped to the screen size),
				// so instead using a hard-coded decoration size.
				if (IsBiggerThanScreen(GetWindow().Size + estimatedDecorationSize))
				{
					// Align with top left of screen if bigger than the screen (such that the title bar is visible and user is able to drag it)
					GetWindow().Position = DisplayServer.ScreenGetPosition(DisplayServer.WindowGetCurrentScreen()) + estimatedDecorationSize;
				}
				else
				{
					// Center if possible otherwise
					GetWindow().MoveToCenter();
				}
			}
		}

		CallDeferred(MethodName.UpdateMinor);
	}

	private static bool IsBiggerThanScreen(Vector2I size)
	{
		var currentScreenSize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
		return size.X > currentScreenSize.X || size.Y > currentScreenSize.Y;
	}

	private void ConnectUserSettingSignals()
	{
		_userSettings?.TryConnect(GdfViewportUserSettings.SignalName.SettingChanged, new Callable(this, MethodName.OnUserSettingChanged));
		_settingSignalsConnected = true;
	}

	private void DisconnectUserSettingSignals()
	{
		_userSettings?.TryDisconnect(GdfViewportUserSettings.SignalName.SettingChanged, new Callable(this, MethodName.OnUserSettingChanged));
		_settingSignalsConnected = false;
	}
}