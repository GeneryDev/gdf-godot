using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GDF.Debug;
using GDF.Util;
using Godot;
using Godot.Collections;
using Array = System.Array;

namespace GDF.Resources;

public static class ResourceLibrarySystem
{
    public static CallableEvent LibrariesUpdated = new();
    
    public static readonly List<IResourceLibrary> Libraries = new();
    
    private static bool _editorLibraryInitialized;
    
    public static void RegisterLibrary(IResourceLibrary library)
    {
        if (Libraries.Contains(library)) return;
        Libraries.Add(library);
        LibrariesUpdated.Invoke();
    }

    private static void InitializeLibraryFromAssembly()
    {
        if (_editorLibraryInitialized) return;
        _editorLibraryInitialized = true;
        
        var assembly = Assembly.GetAssembly(typeof(GdfConstants));
        if (assembly == null) return;
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<LibraryAccessibleInEditorAttribute>() == null) continue;
            if (type.IsAbstract)
            {
                GD.PrintErr($"Invalid {nameof(LibraryAccessibleInEditorAttribute)} on type {type}: Type must not be abstract.");
                continue;
            }
            if (!typeof(IResourceLibrary).IsAssignableFrom(type))
            {
                GD.PrintErr($"Invalid {nameof(LibraryAccessibleInEditorAttribute)} on type {type}: Type must extend ResourceLibrary.");
                continue;
            }

            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                GD.PrintErr($"Invalid {nameof(LibraryAccessibleInEditorAttribute)} on type {type}: Type must have an empty constructor.");
                continue;
            }

            var library = (IResourceLibrary)constructor.Invoke(Array.Empty<object>());
            Libraries.Add(library);
        }
    }

    public static IResourceLibrary GetLibraryByTypeString(string libraryTypeString)
    {
        if (string.IsNullOrEmpty(libraryTypeString)) return null;
        
        if (Engine.IsEditorHint()) InitializeLibraryFromAssembly();
        
        foreach (var library in Libraries)
        {
            if (library.GetLibraryTypeString() != libraryTypeString) continue;
            return library;
        }

        return null;
    }

    public static string GetAllTypeStringsCommaSeparated()
    {
        if (Engine.IsEditorHint()) InitializeLibraryFromAssembly();
        
        var sb = new StringBuilder();
        foreach (var library in Libraries)
        {
            if(sb.Length > 0)
                sb.Append(',');
            sb.Append(library.GetLibraryTypeString());
        }

        return sb.ToString();
    }

    public static void ConfigurePropertyAsIdArray(Dictionary property, string libraryTypeString)
    {
        var library = GetLibraryByTypeString(libraryTypeString);
        property["hint"] = (int)PropertyHint.TypeString;
        property["hint_string"] =
            $"{Variant.Type.StringName:D}/{PropertyHint.EnumSuggestion:D}:{library.GetAllIdsCommaSeparated()}";
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class LibraryAccessibleInEditorAttribute : Attribute;