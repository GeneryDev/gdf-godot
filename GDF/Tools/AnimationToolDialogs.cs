using Godot;

namespace GDF.Tools;

internal static class AnimationToolDialogs
{
#if TOOLS
    public static Window NewAnimationWindow(out LineEdit nameField, out OptionButton inheritOptions,
        out Button okButton, out Button cancelButton)
    {
        var theme = EditorInterface.Singleton.GetEditorTheme();

        var dialog = new Window()
        {
            Title = "New Animation",
            Borderless = false,
            Size = new Vector2I(350, 160),
            Theme = theme,
            Transient = true,
            Exclusive = true
        };
        dialog.CloseRequested += dialog.QueueFree;

        var bg = new PanelContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        dialog.AddChild(bg);

        bg.AddThemeStyleboxOverride("panel", theme.GetStylebox("panel", "PopupPanel"));

        var vbox = new VBoxContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        bg.AddChild(vbox);

        var content = new VBoxContainer()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(content);
        content.AddChild(new Label()
        {
            Text = "New Animation Name:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        content.AddChild(nameField = new LineEdit()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = "CodeEdit",
            SelectAllOnFocus = true
        });
        var inheritRow = new HBoxContainer()
        {
            CustomMinimumSize = new Vector2(1, 29)
        };
        content.AddChild(inheritRow);
        inheritRow.AddChild(new Label()
        {
            Text = "Inherit From:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        inheritRow.AddChild(inheritOptions = new OptionButton()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });

        var buttonRow = new HFlowContainer()
        {
            Alignment = FlowContainer.AlignmentMode.Center
        };
        vbox.AddChild(buttonRow);
        var buttonSize = new Vector2(105, 34);
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(okButton = new Button() { Text = "OK", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(cancelButton = new Button() { Text = "Cancel", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        return dialog;
    }

    public static Window ChangeAnimationInheritanceWindow(out OptionButton workingOptions,
        out OptionButton inheritOptions, out Button okButton, out Button cancelButton)
    {
        var theme = EditorInterface.Singleton.GetEditorTheme();

        var dialog = new Window()
        {
            Title = "Change Animation Inheritance",
            Borderless = false,
            Size = new Vector2I(350, 160),
            Theme = theme,
            Transient = true,
            Exclusive = true
        };
        dialog.CloseRequested += dialog.QueueFree;

        var bg = new PanelContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        dialog.AddChild(bg);

        bg.AddThemeStyleboxOverride("panel", theme.GetStylebox("panel", "PopupPanel"));

        var vbox = new VBoxContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        bg.AddChild(vbox);

        var content = new VBoxContainer()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(content);
        content.AddChild(new Label()
        {
            Text = "Animation:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        content.AddChild(workingOptions = new OptionButton()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        var inheritRow = new HBoxContainer()
        {
            CustomMinimumSize = new Vector2(1, 29)
        };
        content.AddChild(inheritRow);
        inheritRow.AddChild(new Label()
        {
            Text = "...should inherit from:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        inheritRow.AddChild(inheritOptions = new OptionButton()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });

        var buttonRow = new HFlowContainer()
        {
            Alignment = FlowContainer.AlignmentMode.Center
        };
        vbox.AddChild(buttonRow);
        var buttonSize = new Vector2(105, 34);
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(okButton = new Button() { Text = "OK", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(cancelButton = new Button() { Text = "Cancel", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        return dialog;
    }

    public static Window TrackBrowserWindow(out LineEdit searchBar, out Tree tree, out Control propertyDrawer,
        out CheckBox importedBox, out Button closeButton)

    {
        var theme = EditorInterface.Singleton.GetEditorTheme();

        var dialog = new Window()
        {
            Title = "Track Browser",
            Borderless = false,
            Size = new Vector2I(500, 400),
            Theme = theme,
            Transient = true,
            Exclusive = true
        };
        dialog.CloseRequested += dialog.QueueFree;

        var bg = new PanelContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        dialog.AddChild(bg);

        bg.AddThemeStyleboxOverride("panel", theme.GetStylebox("panel", "PopupPanel"));

        var vbox = new VBoxContainer()
        {
            AnchorLeft = 0, AnchorTop = 0,
            AnchorRight = 1, AnchorBottom = 1
        };
        bg.AddChild(vbox);

        var content = new VBoxContainer()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(content);
        content.AddChild(searchBar = new LineEdit()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            PlaceholderText = "Search"
        });
        content.AddChild(tree = new Tree()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HideRoot = true
        });

        propertyDrawer = new MarginContainer()
        {
            // SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        content.AddChild(propertyDrawer);
        propertyDrawer.AddThemeConstantOverride("margin_top", 10);
        propertyDrawer.AddThemeConstantOverride("margin_bottom", 10);
        propertyDrawer.AddThemeConstantOverride("margin_left", 10);
        propertyDrawer.AddThemeConstantOverride("margin_right", 10);

        var propertyContainer = new VBoxContainer();
        propertyDrawer.AddChild(propertyContainer);
        propertyContainer.AddChild(new Label()
        {
            Text = "Track Properties:"
        });
        propertyContainer.AddChild(importedBox = new CheckBox()
        {
            Text = "Imported"
        });

        var buttonRow = new HFlowContainer()
        {
            Alignment = FlowContainer.AlignmentMode.Center
        };
        vbox.AddChild(buttonRow);
        var buttonSize = new Vector2(105, 34);
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(closeButton = new Button() { Text = "Close", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        return dialog;
    }
#endif
}