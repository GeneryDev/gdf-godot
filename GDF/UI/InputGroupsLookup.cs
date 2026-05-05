using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace GDF;

public static partial class InputGroups
{
    private static string[] _all;
    
    public static string[] GetAll()
    {
        if (_all == null)
        {
            var list = new List<string>();
            var inputGroupsClass = typeof(InputGroups);
            var fields =
                inputGroupsClass.GetFields(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(string)) continue;
                if (!field.IsLiteral) continue;
                list.Add((string)field.GetRawConstantValue());
            }

            _all = list.ToArray();
        }

        return _all;
    }
}