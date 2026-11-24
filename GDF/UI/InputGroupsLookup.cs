using System.Collections.Generic;
using System.Reflection;
using GDF.Attributes.Impl;

// ReSharper disable once CheckNamespace
namespace GDF.Input;

public partial class InputGroups
{
    private Dictionary<string, InputStack> _all;
    
    public Dictionary<string, InputStack> GetAll()
    {
        if (_all != null) return _all;
        
        _all = new Dictionary<string, InputStack>();
        var inputGroupsClass = typeof(InputGroups);
        var fields =
            inputGroupsClass.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (field.FieldType != typeof(InputStack)) continue;
            _all[field.Name] = (InputStack)field.GetValue(this);
        }
        return _all;
    }

    public InputStack Get(string name)
    {
        return GetAll()[name];
    }
}