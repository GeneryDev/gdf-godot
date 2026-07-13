using Godot;

namespace GDF.Debug;

public struct DebugCommandArgumentParser
{
    private static char[] Whitespace = new[] { ' ', '\t', '\n', '\r' }; 
    
    public string Raw;
    public int CurrentIndex;
    public string Error;
    public int PrevIndex;

    public DebugCommandArgumentParser(string raw, int currentIndex = 0)
    {
        Raw = raw;
        CurrentIndex = 0;
    }

    public string GetRemainder()
    {
        StepOverWhitespace();
        if (CurrentIndex == 0) return Raw;
        return Raw[CurrentIndex..];
    }

    public bool ReadWord(out string output)
    {
        output = null;
        StepOverWhitespace();
        PrevIndex = CurrentIndex;
        if (ReachedEnd())
        {
            Error = "Unexpected end of input";
            return false;
        }
        int wordEndIndex = Raw.IndexOfAny(Whitespace, CurrentIndex);
        if (wordEndIndex < 0) wordEndIndex = Raw.Length;
        output = Raw[CurrentIndex..wordEndIndex];
        CurrentIndex = wordEndIndex;
        StepOverWhitespace();
        return true;
    }

    public bool PeekWord(out string output, out int length)
    {
        var copy = this;
        if (copy.ReadWord(out output))
        {
            length = copy.CurrentIndex - this.CurrentIndex;
            return true;
        }
        else
        {
            length = 0;
            return false;
        }
    }

    public bool ReadInt(out int output)
    {
        PrevIndex = CurrentIndex;
        if (PeekWord(out var word, out int length))
        {
            if (int.TryParse(word, out output))
            {
                CurrentIndex += length;
                return true;
            }
            else
            {
                Error = "Invalid integer number: " + word;
                return false;
            }
        }
        else
        {
            Error = "Unexpected end of input";
            output = default;
            return false;
        }
    }

    public bool PeekInt(out int output, out int length)
    {
        var copy = this;
        if (copy.ReadInt(out output))
        {
            length = copy.CurrentIndex - this.CurrentIndex;
            return true;
        }
        else
        {
            length = 0;
            return false;
        }
    }

    public bool ReadFloat(out float output)
    {
        PrevIndex = CurrentIndex;
        if (PeekWord(out var word, out int length))
        {
            if (float.TryParse(word, out output))
            {
                CurrentIndex += length;
                return true;
            }
            else
            {
                Error = "Invalid floating-point number: " + word;
                return false;
            }
        }
        else
        {
            Error = "Unexpected end of input";
            output = default;
            return false;
        }
    }

    public bool PeekFloat(out float output, out int length)
    {
        var copy = this;
        if (copy.ReadFloat(out output))
        {
            length = copy.CurrentIndex - this.CurrentIndex;
            return true;
        }
        else
        {
            length = 0;
            return false;
        }
    }

    public bool ReadString(out string output)
    {
        StepOverWhitespace();
        PrevIndex = CurrentIndex;
        if (ReachedEnd())
        {
            Error = "Unexpected end of input";
            output = default;
            return false;
        }

        var firstChar = Raw[CurrentIndex];
        if (firstChar is '"' or '\'')
        {
            // quoted string

            int startIndex = CurrentIndex; //incl.
            int endIndex = -1; // excl.
            var escaped = false;
            for (int i = startIndex + 1; i < Raw.Length; i++)
            {
                var ch = Raw[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == firstChar)
                {
                    endIndex = i + 1;
                    break;
                }
            }

            if (endIndex == -1)
            {
                output = default;
                Error = "Unterminated string";
                return false;
            }

            output = Raw[(startIndex + 1)..(endIndex - 1)].StripEscapes();

            CurrentIndex = endIndex;
            StepOverWhitespace();
            return true;
        }
        else
        {
            return ReadWord(out output);
        }
    }

    public bool PeekString(out string output, out int length)
    {
        var copy = this;
        if (copy.ReadString(out output))
        {
            length = copy.CurrentIndex - this.CurrentIndex;
            return true;
        }
        else
        {
            length = 0;
            return false;
        }
    }

    private bool IsWhitespace(char ch)
    {
        foreach (char whitespace in Whitespace)
        {
            if (whitespace == ch) return true;
        }

        return false;
    }

    public bool StepOverWhitespace()
    {
        bool stepped = false;
        while (CurrentIndex < Raw.Length)
        {
            if (IsWhitespace(Raw[CurrentIndex]))
            {
                CurrentIndex++;
                stepped = true;
            }
            else
            {
                break;
            }
        }

        return stepped;
    }

    public bool ReachedEnd()
    {
        return CurrentIndex >= Raw.Length;
    }

    public void PrintError()
    {
        int index = CurrentIndex;
        if (Error != null)
        {
            GD.PrintErr($"Error parsing command: {Error} at column {index}:\n{Raw}\n{" ".PadLeft(index)}^ here");
        }
        else
        {
            // what now?
        }
    }

    public void PrintCustomError(string customError)
    {
        int index = PrevIndex;
        if (customError != null)
        {
            GD.PrintErr($"Error parsing command: {customError} at column {index}:\n{Raw}\n{" ".PadLeft(index)}^ here");
        }
        else
        {
            // what now?
        }
    }
}