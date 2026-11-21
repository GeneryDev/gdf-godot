using Godot;

namespace GDF;

public static class GdfLogger
{
    public static void Print(string message)
    {
        GD.Print(message);
    }
    public static void Error(string message)
    {
        GD.PrintErr(message);
    }
}