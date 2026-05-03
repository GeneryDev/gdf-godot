#if TOOLS
using System;
using Godot;
using Godot.Collections;

namespace GDF.Editor;

public static partial class EditorUtils
{
    private static int _lastSelectedSignalMethodTabIndex = 0;

    public delegate void SignalSelectedEventHandler(Dictionary signal);
    public delegate void MethodSelectedEventHandler(Dictionary method);
    
    public static void ShowSignalMethodSelector(Node node, int forceStartTab, SignalSelectedEventHandler signalCallback, MethodSelectedEventHandler methodCallback)
    {
        if (node == null) return;
        
        if (forceStartTab < 0) forceStartTab = _lastSelectedSignalMethodTabIndex;
        _lastSelectedSignalMethodTabIndex = forceStartTab;
        
        var dialog = CreateSignalMethodSelector(forceStartTab, out var searchBar, out var tree, out var submitButton, out var tabGroup);
        var theme = dialog.Theme;
        
        var signalList = new Array<Dictionary>();
        CollectMembers(node, signalList,
            s => s.GetScriptSignalList(),
            (db, name) => db.ClassGetSignalList(name),
            o => o.GetSignalList());
        
        var methodList = new Array<Dictionary>();
        CollectMembers(node, methodList,
            s => s.GetScriptMethodList(),
            (db, name) => db.ClassGetMethodList(name),
            o => o.GetMethodList());

        void PopulateTree(string filter)
        {
            var selectedHandle = tree.GetSelected()?.GetMetadata(0).AsVector2I() ?? new Vector2I(-1, 0);
            
            tree.Clear();

            var root = tree.CreateItem();
            
            if (tabGroup.GetPressedButton().GetIndex() == 0)
            {
                var methodRoot = tree.CreateItem(root);
                methodRoot.SetText(0, "Methods");

                for (var methodIndex = 0; methodIndex < methodList.Count; methodIndex++)
                {
                    var handle = new Vector2I(0, methodIndex);

                    var method = methodList[methodIndex];
                    string text = GetMethodSignatureText(method);

                    if (!string.IsNullOrEmpty(filter) &&
                        !text.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var item = tree.CreateItem(methodRoot);
                    item.SetIcon(0, theme.GetIcon(IconNameMethod, "EditorIcons"));
                    item.SetText(0, text);
                    item.SetMetadata(0, handle);
                    if (selectedHandle == handle)
                    {
                        item.Select(0);
                    }
                }
            }

            if (tabGroup.GetPressedButton().GetIndex() == 1)
            {
                var signalRoot = tree.CreateItem(root);
                signalRoot.SetText(0, "Signals");
                
                for (var signalIndex = 0; signalIndex < signalList.Count; signalIndex++)
                {
                    var handle = new Vector2I(1, signalIndex);
                    
                    var signal = signalList[signalIndex];
                    string text = GetMethodSignatureText(signal);

                    if (!string.IsNullOrEmpty(filter) &&
                        !text.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var item = tree.CreateItem(signalRoot);
                    item.SetIcon(0, theme.GetIcon(IconNameSignal, "EditorIcons"));
                    item.SetText(0, text);
                    item.SetMetadata(0, handle);
                    if (selectedHandle == handle)
                    {
                        item.Select(0);
                    }
                }
            }
            
            OnSelectionChanged();
        }
        dialog.WindowInput += OnWindowInput;

        tree.ItemSelected += OnSelectionChanged;
        tree.ItemActivated += TreeOnItemActivated;

        submitButton.Pressed += OnSubmitted;

        tabGroup.Pressed += OnTabGroupPressed;
        searchBar.TextChanged += PopulateTree;
        // This is necessary so that the tab group gets garbage collected and doesn't cause the assembly reload to fail on recompilation.
        dialog.TreeExiting += () => tabGroup.Pressed -= OnTabGroupPressed;

        PopulateTree("");
        
        dialog.Visible = false;
        EditorInterface.Singleton.GetEditorMainScreen().AddChild(dialog);
        dialog.PopupCentered();
        
        return;

        void OnWindowInput(InputEvent evt)
        {
            if (evt.IsAction("ui_cancel"))
            {
                dialog.SetInputAsHandled();
                dialog.QueueFree();
            }

            if (evt.IsAction("ui_accept") && !dialog.IsInputHandled())
            {
                dialog.SetInputAsHandled();
                if(tree.GetSelected() != null)
                    OnSubmitted();
            }
        }
        
        void TreeOnItemActivated()
        {
            tree.AcceptEvent();
            if(tree.GetSelected() != null)
                OnSubmitted();
        }
        
        void OnSelectionChanged()
        {
            submitButton.Disabled = tree.GetSelected() == null;
        }

        void OnTabGroupPressed(BaseButton btn)
        {
            _lastSelectedSignalMethodTabIndex = btn.GetIndex();
            PopulateTree(searchBar.Text);
        }

        void OnSubmitted()
        {
            var selectedHandle = tree.GetSelected()?.GetMetadata(0).AsVector2I() ?? new Vector2I(-1, 0);
            int selectedType = selectedHandle.X;
            int selectedIndex = selectedHandle.Y;
            if (selectedType == 0)
            {
                // method
                var method = methodList[selectedIndex];
                var methodName = method["name"].AsStringName();
                GD.Print($"Selected method: {method}");
                methodCallback?.Invoke(method);
            } else if (selectedType == 1)
            {
                // signal
                var signal = signalList[selectedIndex];
                GD.Print($"Selected signal: {signal}");
                signalCallback?.Invoke(signal);
            }
            dialog.QueueFree();
            // GD.Print($"Submit {tree.GetSelected()}");
        }
    }

    
    private static Window CreateSignalMethodSelector(int forceStartTab, out LineEdit searchBar, out Tree tree, out Button submitButton,
        out ButtonGroup tabGroup)
    {
        var theme = EditorInterface.Singleton.GetEditorTheme();

        var dialog = new Window()
        {
            Title = "Select Method/Signal",
            Borderless = false,
            Size = new Vector2I(500, 400),
            Theme = theme,
            Transient = true,
            Exclusive = true
        };

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
        var tabRow = new HBoxContainer();
        tabGroup = new ButtonGroup();
        var methodsButton = new Button()
        {
            Text = "Methods",
            ButtonGroup = tabGroup,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Icon = theme.GetIcon(IconNameMethod, "EditorIcons"),
            ToggleMode = true,
            ButtonPressed = forceStartTab <= 0,
            ThemeTypeVariation = "FlatMenuButton"
        };
        var signalsButton = new Button()
        {
            Text = "Signals",
            ButtonGroup = tabGroup,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Icon = theme.GetIcon(IconNameSignal, "EditorIcons"),
            ToggleMode = true,
            ButtonPressed = forceStartTab == 1,
            ThemeTypeVariation = "FlatMenuButton"
        };
        tabRow.AddChild(methodsButton);
        tabRow.AddChild(signalsButton);
        content.AddChild(tabRow);
        
        content.AddChild(searchBar = new LineEdit()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            PlaceholderText = "Filter",
            RightIcon = theme.GetIcon("Search", "EditorIcons")
        });
        
        content.AddChild(tree = new Tree()
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HideRoot = true
        });

        var buttonRow = new HFlowContainer()
        {
            Alignment = FlowContainer.AlignmentMode.Center,
        };
        vbox.AddChild(buttonRow);
        var buttonSize = new Vector2(105, 34);
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        buttonRow.AddChild(submitButton = new Button() { Text = "Add", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() {SizeFlagsHorizontal = Control.SizeFlags.ExpandFill});
        Button closeButton;
        buttonRow.AddChild(closeButton = new Button() { Text = "Close", CustomMinimumSize = buttonSize });
        buttonRow.AddChild(new Control() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        
        closeButton.Pressed += dialog.QueueFree;
        dialog.CloseRequested += dialog.QueueFree;
        
        return dialog;
    }

    private static void CollectMembers(GodotObject obj,
        Array<Dictionary> list,
        Func<Script, Array<Dictionary>> scriptGetter,
        Func<ClassDBInstance, StringName, Array<Dictionary>> classGetter,
        Func<GodotObject, Array<Dictionary>> instanceGetter)
    {
        var nameList = new Array<StringName>();

        // Add members by script
        var script = obj.GetScript().As<Script>();
        while (script != null && scriptGetter != null)
        {
            // GD.Print($"Script: {script.ResourcePath}");
            foreach (var def in scriptGetter(script))
            {
                var name = def["name"].AsStringName();
                if (nameList.Contains(name)) continue;
                list.Add(def);
                nameList.Add(def["name"].AsStringName());
            }
            script = script.GetBaseScript();
        }
        // Add members by class
        StringName className = obj.GetClass();
        while (className != null && !className.IsEmpty && classGetter != null)
        {
            // GD.Print($"Class: {className}");
            foreach (var def in classGetter(ClassDB.Singleton, className))
            {
                var name = def["name"].AsStringName();
                if (nameList.Contains(name)) continue;
                list.Add(def);
                nameList.Add(def["name"].AsStringName());
            }
            className = ClassDB.GetParentClass(className);
        }
        // Add dynamic signals and methods for this specific node
        if (instanceGetter != null)
        {
            var insertionIndex = 0;
            foreach (var def in instanceGetter(obj))
            {
                var name = def["name"].AsStringName();
                if (nameList.Contains(name)) continue;
                nameList.Add(name);
                list.Insert(insertionIndex, def);
                insertionIndex++;
            }
        }
    }
    
    
}
#endif