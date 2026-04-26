using System.Collections.Generic;
using GDF.Util;
using Godot;

namespace GDF.Data;

public interface IDataContext
{
    public IDataContext ParentContext => null;
    public bool UseStringsAsVariables => true;
    public bool UseVariablesAsStrings => true;

    public virtual bool GetContextVariable(string key, string input, ref Variant output,
        IDataQueryOptions options)
    {
        return false;
    }

    public virtual bool GetContextString(string key, string input, ref string replacement,
        IDataQueryOptions options)
    {
        return false;
    }

    public virtual bool GetSubContext(string key, string input, ref IDataContext output,
        IDataQueryOptions options)
    {
        return false;
    }

    public virtual bool GetCollection(string key, string input, List<IDataContext> output,
        IDataQueryOptions options)
    {
        return false;
    }

    public StringName UpdatedSignalName => null;

    public virtual void ConnectUpdateSignal(Callable callable)
    {
        var signalName = UpdatedSignalName;
        if (!signalName.IsNullOrEmpty() && this is GodotObject obj) obj.TryConnect(signalName, callable);
    }

    public virtual void DisconnectUpdateSignal(Callable callable)
    {
        var signalName = UpdatedSignalName;
        if (!signalName.IsNullOrEmpty() && this is GodotObject obj) obj.TryDisconnect(signalName, callable);
    }

    public virtual bool EqualsContext(IDataContext other)
    {
        return this == other;
    }
}

public enum DataQueryType
{
    Expression,
    String,
    SubContext,
    Collection
}