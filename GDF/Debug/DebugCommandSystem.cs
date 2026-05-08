using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GDF.Data;
using GDF.UI;
using GDF.Util;
using Godot;
using Godot.Collections;

namespace GDF.Debug;

[HasDebugCommands]
[GlobalClass]
public partial class DebugCommandSystem : SingletonNode<DebugCommandSystem>
{
    [Export] public Array<DebugCommandMacro> Macros = new();
    
#if DEBUG
    private const int MaxHistory = 50;
    
    private static readonly System.Collections.Generic.Dictionary<string, DebugCommandDefinition> Definitions = new();

    private static readonly HashSet<string> ToggledCommands = new();
    private static readonly System.Collections.Generic.Dictionary<string, Node> ShownScreens = new();

    private static readonly List<string> History = new();

    private DebugLogger _debugLogger;
    public static DebugLogger DebugLogger => Instance._debugLogger;

    public override void _EnterTree()
    {
        base._EnterTree();
        _debugLogger ??= new();
        OS.AddLogger(_debugLogger);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        OS.RemoveLogger(_debugLogger);
    }

    public static bool IsCommandToggled(string id)
    {
        return ToggledCommands.Contains(id);
    }

    public static bool ToggleCommand(string id)
    {
        if (!ToggledCommands.Add(id))
        {
            ToggledCommands.Remove(id);
            return false;
        }
        else
        {
            return true;
        }
    }

    public static void SubmitCommand(string command, bool appendToHistory = false)
    {
        command = command.Trim();
        if (string.IsNullOrEmpty(command)) return;
        
        if (appendToHistory)
            AppendToHistory(command);
        
        if (!Definitions.TryGetValue(command, out var def))
        {
            GD.Print($"No such command '{command}'");
            return;
        }

        try
        {
            def.TriggerAction();
        }
        catch (Exception ex)
        {
            GD.Print($"Exception while running debug command '{command}':\n{ex}");
        }
        GD.Print($"Executed debug command '{command}'");
    }

    private static void AppendToHistory(string command)
    {
        if (History.Count > 0 && History[^1] == command) return;
        History.Add(command);
        while (History.Count > MaxHistory)
            History.RemoveAt(0);
    }

    public static List<string> GetCommandHistory()
    {
        return History;
    }

    public override void _Ready()
    {
        PopulateDefinitions();
        
        SubmitCommand("help");
    }

    private static void PopulateDefinitions()
    {
        PopulateDefinitionsFromAssembly();
    }

    private static void PopulateDefinitionsFromAssembly()
    {
        var assembly = Assembly.GetAssembly(typeof(GdfConstants));
        if (assembly == null) return;
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<HasDebugCommandsAttribute>() == null) continue;
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attr in method.GetCustomAttributes<DebugCommandAttribute>())
                {
                    RegisterDebugCommand(method, attr);
                }
            }
        }
    }

    private static void RegisterDebugCommand(MethodInfo method, DebugCommandAttribute attr)
    {
        string id = attr.Id;
        if (id == null)
        {
            GD.PrintErr($"{nameof(DebugCommandAttribute)} Id cannot be null! In type {method.DeclaringType}, method {method.Name}");
            return;
        }

        if (Definitions.ContainsKey(id))
        {
            GD.PrintErr($"Duplicate definition of '{id}' debug command.");
            return;
        }

        Type[] expectedParamTypes;
        Type expectedReturnType;
        switch (attr.Type)
        {
            case DebugCommandType.Trigger:
                expectedParamTypes = Type.EmptyTypes;
                expectedReturnType = typeof(void);
                break;
            case DebugCommandType.Toggle:
                expectedParamTypes = new Type[] { typeof(bool) };
                expectedReturnType = typeof(void);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var paramsMatch = true;
        var actualParams = method.GetParameters();
        if (actualParams.Length != expectedParamTypes.Length) paramsMatch = false;
        else
        {
            for (var i = 0; i < expectedParamTypes.Length; i++)
            {
                if (expectedParamTypes[i] != actualParams[i].ParameterType)
                {
                    paramsMatch = false;
                    break;
                }
            }
        }

        if (!method.IsStatic)
        {
            GD.PrintErr($"Invalid debug command {id}. Method must be static. In type {method.DeclaringType}, method {method.Name}");
            return;
        }

        if (!paramsMatch)
        {
            GD.PrintErr($"Invalid debug command {id}. Method has parameter types: ({actualParams.Select(p => p.ParameterType.FullName).ToArray().Join(", ")}), but expected ({expectedParamTypes.Select(t => t.FullName).ToArray().Join(", ")}) for type {attr.Type}. In type {method.DeclaringType}, method {method.Name}");
            return;
        }

        if (method.ReturnType != expectedReturnType)
        {
            GD.PrintErr($"Invalid debug command {id}. Method has return type: ({method.ReturnType.FullName}), but expected ({expectedReturnType.FullName}) for type {attr.Type}. In type {method.DeclaringType}, method {method.Name}");
            return;
        }
        
        Action triggerAction;

        switch (attr.Type)
        {
            case DebugCommandType.Trigger:
                triggerAction = method.CreateDelegate<Action>();
                break;
            case DebugCommandType.Toggle:
                var callDelegate = method.CreateDelegate<Action<bool>>();
                triggerAction = () =>
                {
                    callDelegate(ToggleCommand(id));
                };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Definitions[id] = new DebugCommandDefinition()
        {
            Method = method,
            Attribute = attr,
            TriggerAction = triggerAction
        };
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (Macros == null) return;
        if (@event is not InputEventKey kEvt) return;
        if (kEvt is not { Echo: false, Pressed: true }) return;
        foreach (var macro in Macros)
        {
            if (macro?.Shortcut == null) continue;
            if (macro.RequireKeyPressed != Key.None && !Godot.Input.IsKeyPressed(macro.RequireKeyPressed)) continue;
            if (@event.IsMatch(macro.Shortcut))
            {
                SubmitMacro(macro);
            }
        }
    }

    private static void SubmitMacro(DebugCommandMacro macro)
    {
        if (string.IsNullOrEmpty(macro.Command)) return;
        SubmitCommand(macro.Command);
    }


    [DebugCommand("help")]
    public static void Help()
    {
        GD.Print("Help!!!!");
    }
    [DebugCommand("console")]
    public static void ShowConsole()
    {
        ShowScreen($"{GdfConstants.PluginRoot}/scenes/debug/debug_console.tscn");
    }

    public static void CloseConsole()
    {
        CloseScreen($"{GdfConstants.PluginRoot}/scenes/debug/debug_console.tscn");
    }

    public static void ToggleScreen(string scenePath)
    {
        if(!IsScreenShowing(scenePath)) ShowScreen(scenePath);
        else CloseScreen(scenePath);
    }

    public static bool IsScreenShowing(string scenePath)
    {
        return ShownScreens.TryGetValue(scenePath, out var node) && IsInstanceValid(node);
    }

    public static void ShowScreen(string scenePath)
    {
        if (ShownScreens.TryGetValue(scenePath, out var existing))
        {
            if (IsInstanceValid(existing))
            {
                GD.Print($"Screen {scenePath} already showing!");
                return;
            }
        }
        var scene = GD.Load<PackedScene>(scenePath);
        if (scene == null) return;
        var treeRoot = Instance.GetTree().Root;
        var instantiated = scene.GdfInstantiate();
        if (instantiated is Screen screen)
            instantiated = screen.ToPlaceholder();
        ShownScreens[scenePath] = instantiated;
        treeRoot.AddChild(instantiated);
    }

    public static void CloseScreen(string scenePath)
    {
        if (!ShownScreens.TryGetValue(scenePath, out var existing)) return;
        if (!IsInstanceValid(existing)) return;
        
        existing.QueueFree();
        ShownScreens.Remove(scenePath);
    }

    public static bool AutocompleteCommand(string command, out string suggested)
    {
        suggested = null;
        if (command.Length == 0) return false;
        foreach ((var id, var def) in Definitions)
        {
            if (id.Length > command.Length && id.StartsWith(command))
            {
                suggested = id;
                return true;
            }
        }

        return false;
    }

    private struct DebugCommandDefinition
    {
        public MethodInfo Method;
        public DebugCommandAttribute Attribute;
        public Action TriggerAction;
    }
#endif
}