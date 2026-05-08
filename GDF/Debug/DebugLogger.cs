using System.Collections.Generic;
using System.Text;
using GDF.Data;
using Godot;
using Godot.Collections;

namespace GDF.Debug;

public partial class DebugLogger : Logger, IDataContext
{
    [Signal]
    public delegate void UpdatedEventHandler();
    
    public const int Capacity = 100;
    private readonly List<string> _loggedLines = new();

    private StringBuilder _sb = new();
    private string _cachedText = null;
    
    public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType,
        Array<ScriptBacktrace> scriptBacktraces)
    {
        base._LogError(function, file, line, code, rationale, editorNotify, errorType, scriptBacktraces);
    }

    public override void _LogMessage(string message, bool error)
    {
        base._LogMessage(message, error);
        AddLogLine(message);
    }

    private void AddLogLine(string message)
    {
        _loggedLines.Add(message);
        _cachedText = null;
        TrimToCapacity();
        EmitSignalUpdated();
    }

    private void TrimToCapacity()
    {
        while (_loggedLines.Count > Capacity)
        {
            _loggedLines.RemoveAt(0);
        }
    }

    public string GetLogText()
    {
        if (_cachedText == null)
        {
            _sb.Clear();
            foreach (string line in _loggedLines)
            {
                _sb.Append(line);
            }

            _cachedText = _sb.ToString();
            _sb.Clear();
        }

        return _cachedText;
    }

    public StringName UpdatedSignalName => SignalName.Updated;

    public bool GetContextString(string key, string input, ref string replacement, IDataQueryOptions options)
    {
        switch (key)
        {
            case "log_text":
            {
                replacement = GetLogText();
                return true;
            }
        }

        return false;
    }
}