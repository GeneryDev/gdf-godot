using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace GDF.Data.Parameterized;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
public class ParameterizedDataContextAttribute : Attribute
{
    public string Id;

    public ParameterizedDataContextAttribute(string id)
    {
        Id = id;
    }
}

public static class ParameterizedDataContexts
{
    private static readonly Dictionary<string, Definition> Definitions = new();
    private static bool _initialized = false;
    
    public static string[] GetAllIds()
    {
        Initialize();
        return Definitions.Keys.ToArray();
    }

    public static bool IsValidId(string id)
    {
        return Definitions.ContainsKey(id);
    }

    public static IDataContext Create(string id)
    {
        if (GetDefinitionOrNull(id) is not { } def) return null;
        
        return def.Constructor();
    }
    public static IDataContext Create(string id, Godot.Collections.Dictionary parameters)
    {
        if (GetDefinitionOrNull(id) is not { } def) return null;
        
        return parameters == null ? def.Constructor() : def.ParameterizedDictionaryConstructor(parameters);
    }
    public static IDataContext Create(string id, Godot.Collections.Array parameters)
    {
        if (GetDefinitionOrNull(id) is not { } def) return null;
        
        return parameters == null ? def.Constructor() : def.ParameterizedArrayConstructor(parameters);
    }
    public static IDataContext CreateV(string id, params Variant[] parameters)
    {
        if (GetDefinitionOrNull(id) is not { } def) return null;
        
        return parameters == null ? def.Constructor() : def.ParameterizedSystemArrayConstructor(parameters);
    }

    private static Definition? GetDefinition(string id)
    {
        if (GetDefinitionOrNull(id) is { } def) return def;
        GD.PrintErr($"No such parameterized data context id '{id}'");
        return null;
    }

    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetPropertyList(string id)
    {
        if (GetDefinitionOrNull(id) is not { } def) return new();

        return def.PropertyList;
    }

    private static Definition? GetDefinitionOrNull(string id)
    {
        Initialize();
        if (!Definitions.TryGetValue(id, out var def))
        {
            return null;
        }

        return def;
    }

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Definitions.Clear();
        var assembly = Assembly.GetAssembly(typeof(GdfConstants));
        if (assembly == null) return;
        foreach (var type in assembly.GetTypes())
        {
            foreach (var attr in type.GetCustomAttributes<ParameterizedDataContextAttribute>())
            {
                if (!typeof(IDataContext).IsAssignableFrom(type))
                {
                    GD.PrintErr($"Invalid ParameterizedDataContext attribute on type {type}: Type must extend {nameof(IDataContext)}");
                    continue;
                }
                string id = attr.Id;
                if (Definitions.TryGetValue(id, out var existing))
                {
                    GD.PrintErr($"Duplicate ParameterizedDataContext ID '{id}': {existing.Type} and {type}");
                    continue;
                }
                var definition = new Definition()
                {
                    Type = type,
                    Attribute = attr
                };
                InitializeDefinition(ref definition, id);
                Definitions[attr.Id] = definition;
            }
        }
    }

    private static void InitializeDefinition(ref Definition definition, string id)
    {
        var type = definition.Type;
        var initializationMethod = typeof(ParameterizedDataContexts)
            .GetMethod(
                nameof(InitializeDefinitionGeneric),
                genericParameterCount: 1,
                bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new Type[] { typeof(Definition).MakeByRefType(), typeof(string) },
                modifiers: null
                );

        initializationMethod = initializationMethod.MakeGenericMethod(type);

        var methodParams = new object[] { definition, id };
        initializationMethod.Invoke(null, methodParams);

        definition = (Definition)methodParams[0];
    }

    private static void InitializeDefinitionGeneric<T>(ref Definition definition, string id) where T : IDataContext, new()
    {
        definition.Constructor = () => new T();
        
        var fieldInitializerMethod = typeof(ParameterizedDataContexts)
            .GetMethod(
                nameof(InitializeFieldDefinitionGeneric),
                genericParameterCount: 2,
                bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new Type[] { typeof(FieldInfo) },
                modifiers: null
            );

        var propertyNames = new List<string>();
        var propertySetters = new List<Action<BoxedContext<T>, Variant>>();

        definition.PropertyList = new();

        foreach (var fieldInfo in typeof(T).GetFields())
        {
            string name = fieldInfo.Name.ToSnakeCase();
            propertyNames.Add(name);

            var fieldDef = (Definition.Field<T>)fieldInitializerMethod!
                .MakeGenericMethod(typeof(T), fieldInfo.FieldType).Invoke(null, new object[] { fieldInfo })!;
            propertySetters.Add(fieldDef.Setter);

            definition.PropertyList.Add(new Godot.Collections.Dictionary()
            {
                {"name", name},
                {"type", Variant.From(fieldDef.VariantType)}
            });
        }

        definition.ParameterizedDictionaryConstructor = (dict) =>
        {
            var boxed = new BoxedContext<T>()
            {
                Value = new T()
            };

            if (dict != null)
            {
                for (int i = 0; i < propertyNames.Count; i++)
                {
                    string name = propertyNames[i];
                    var setter = propertySetters[i];

                    if (!dict.TryGetValue(name, out var rawValue)) continue;
                    setter(boxed, rawValue);
                }
            }
            
            return boxed.Value;
        };

        definition.ParameterizedArrayConstructor = (arr) =>
        {
            var boxed = new BoxedContext<T>()
            {
                Value = new T()
            };

            if (arr != null)
            {
                int arrCount = arr.Count;
                for (var i = 0; i < propertySetters.Count && i < arrCount; i++)
                {
                    var setter = propertySetters[i];

                    var rawValue = arr[i];
                    setter(boxed, rawValue);
                }
            }
            
            return boxed.Value;
        };

        definition.ParameterizedSystemArrayConstructor = (arr) =>
        {
            var boxed = new BoxedContext<T>()
            {
                Value = new T()
            };

            if (arr != null)
            {
                int arrCount = arr.Length;
                for (var i = 0; i < propertySetters.Count && i < arrCount; i++)
                {
                    var setter = propertySetters[i];

                    var rawValue = arr[i];
                    setter(boxed, rawValue);
                }
            }
            
            return boxed.Value;
        };
    }

    private static Definition.Field<TType> InitializeFieldDefinitionGeneric<TType, [MustBeVariant]TField>(FieldInfo field) where TType : IDataContext, new()
    {
        return new Definition.Field<TType>()
        {
            Setter = (boxed, value) =>
            {
                field.SetValueDirect(__makeref(boxed.Value), value.As<TField>());
            },
            VariantType = Variant.From((TField)default).VariantType
        };
    }

    private struct Definition
    {
        public Type Type;
        public ParameterizedDataContextAttribute Attribute;
        
        public Godot.Collections.Array<Godot.Collections.Dictionary> PropertyList;

        public Func<IDataContext> Constructor;
        public Func<Godot.Collections.Dictionary, IDataContext> ParameterizedDictionaryConstructor;
        public Func<Godot.Collections.Array, IDataContext> ParameterizedArrayConstructor;
        public Func<Variant[], IDataContext> ParameterizedSystemArrayConstructor;

        public struct Field<TType> where TType : IDataContext, new()
        {
            public Action<BoxedContext<TType>, Variant> Setter;
            public Variant.Type VariantType;
        }
    }

    private class BoxedContext<T> where T : IDataContext, new()
    {
        public T Value;
    }
}