using System;
using System.Collections.Generic;
using System.Diagnostics;
using GDF.Attributes.Impl;
using GDF.Debug;
using Godot;

namespace GDF.Attributes;

[HasDebugCommands]
public class Test
{
    public static void Main()
    {
        // TestBase();
        TestProfiling();
    }

    private static void TestBase()
    {
        var floatAttribute = new FloatAttribute()
        {
            BaseValue = 100
        };

        floatAttribute.CreateToken("multiply 2", 1)
            .Multiply(2);

        Console.WriteLine(floatAttribute.CurrentValue);

        floatAttribute.CreateToken("add 10", 0)
            .Add(10f)
            .SetWeight(0.5f);

        Console.WriteLine(floatAttribute.CurrentValue);

        floatAttribute.CreateToken("set 100", 3)
            .Set(100)
            .SetWeight(0.5f);

        Console.WriteLine(floatAttribute.CurrentValue);

        Console.WriteLine("====");

        var listAttribute = new ListAttribute<string>();
        listAttribute.CreateToken("add foo")
            .Set(1, "foo");
        listAttribute.CreateToken("add bar")
            .Set(1, "bar");
        listAttribute.CreateToken("add bar", -1)
            .Set(1, new List<string>() {"red", "green", "blue"});

        Console.WriteLine($"[{string.Join(",", listAttribute.CurrentValue)}]");

        var mouseModeAttribute = new Attribute<Godot.Input.MouseModeEnum>(Godot.Input.MouseModeEnum.Visible);
        mouseModeAttribute.CreateToken("Capture", 2)
            .Set(Godot.Input.MouseModeEnum.Captured);
        mouseModeAttribute.CreateToken("Confine", 0)
            .Set(Godot.Input.MouseModeEnum.Confined);

        Console.WriteLine(mouseModeAttribute.CurrentValue);
    }

    private static void TestSets()
    {
        var attributes = new AttributeSet<string>();
        
        attributes.RegisterAttribute("max_health", new FloatAttribute(100) {DefaultFunction = FloatAttribute.Functions.Multiply});
        attributes.RegisterAttribute("invulnerable", new BoolAttribute(false) {DefaultFunction = BoolAttribute.Functions.Or});
        attributes.RegisterAttribute("walk_speed", new FloatAttribute(1.8f) {DefaultFunction = FloatAttribute.Functions.Multiply});
        attributes.RegisterAttribute("can_attack", new BoolAttribute(true) {DefaultFunction = BoolAttribute.Functions.And});

        PrintAttributes();

        attributes.CreateSetModifier("Freeze", 2)
            .Modify("walk_speed", new Mod.Multiply<float>(0.2f))
            .Modify("can_attack", false);
        
        PrintAttributes();

        attributes.CreateSetModifier("Dodge", 1)
            .Modify("walk_speed", Variant.From(1.5f))
            .Modify("invulnerable", true);
        
        PrintAttributes();

        void PrintAttributes()
        {
            GD.Print($"Attributes:");
            GD.Print($"max_health = {attributes.GetValue<float>("max_health")}");
            GD.Print($"invulnerable = {attributes.GetValue<bool>("invulnerable")}");
            GD.Print($"walk_speed = {attributes.GetValue<float>("walk_speed")}");
            GD.Print($"can_attack = {attributes.GetValue<bool>("can_attack")}");
        }
    }

    private static void TestProfiling()
    {
        var floatAttribute = new FloatAttribute()
        {
            BaseValue = 100
        };

        floatAttribute.CreateToken("multiply 1.1", 1)
            .Multiply(1.1f);
        floatAttribute.CreateToken("multiply 1.2", 1)
            .Multiply(1.2f);
        floatAttribute.CreateToken("multiply 1.3", 1)
            .Multiply(1.3f);
        floatAttribute.CreateToken("multiply 1.4", 1)
            .Multiply(1.4f);

        Console.WriteLine(floatAttribute.CurrentValue);

        floatAttribute.CreateToken("add 1", 0)
            .Add(1);
        floatAttribute.CreateToken("add 2", 0)
            .Add(2);
        floatAttribute.CreateToken("add 3", 0)
            .Add(3);
        floatAttribute.CreateToken("add 4", 0)
            .Add(4);

        Console.WriteLine(floatAttribute.CurrentValue);
        
        var randToken = floatAttribute.CreateToken("randomize", 0);
        var rand = new Random();
        (floatAttribute as IAttributeModifiable<Func<float, float, float>>).Modify(randToken, (float a, float b) => a + rand.NextSingle()*100);

        var sw = new Stopwatch();
        sw.Restart();
        const int n = 100_000_000;
        for (int i = 0; i < n; i++)
        {
            floatAttribute.BaseValue += 0.001f;
            floatAttribute.Recompute();
        }
        sw.Stop();

        Console.WriteLine($"Computed {n} times in {sw.ElapsedMilliseconds} ms");
    }
}