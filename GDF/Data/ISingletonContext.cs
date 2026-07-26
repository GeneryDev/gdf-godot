using GDF.Util;
using Godot;
// ReSharper disable StaticMemberInGenericType

namespace GDF.Data;

public interface ISingletonContext<T> : IDataContext where T : SingletonNode<T>, IDataContext
{
    private static T Instance
    {
        get
        {
            if (Engine.IsEditorHint()) return null;
            return SingletonNode<T>.Instance;
        }
    }

    IDataContext IDataContext.ParentContext
    {
        get
        {
            if (RerouteSignalsStatically && !SingletonNode<T>.InstanceExists) return null;
            return Instance;
        }
    }

    void IDataContext.ConnectUpdateSignal(Callable callable)
    {
        if (RerouteSignalsStatically)
        {
            StaticSignals.Initialize();
            StaticSignals.UpdateSignalConnections.Connect(callable);
        }
        else
        {
            Instance?.ConnectUpdateSignal(callable);
        }
    }

    void IDataContext.DisconnectUpdateSignal(Callable callable)
    {
        if (RerouteSignalsStatically)
        {
            StaticSignals.Initialize();
            StaticSignals.UpdateSignalConnections.Disconnect(callable);
        }
        else
        {
            Instance?.DisconnectUpdateSignal(callable);
        }
    }

    void IDataContext.ConnectContextSignal(Callable callable)
    {
        if (RerouteSignalsStatically)
        {
            StaticSignals.Initialize();
            StaticSignals.ContextSignalConnections.Connect(callable);
        }
        else
        {
            Instance?.ConnectContextSignal(callable);
        }
    }

    void IDataContext.DisconnectContextSignal(Callable callable)
    {
        if (RerouteSignalsStatically)
        {
            StaticSignals.Initialize();
            StaticSignals.ContextSignalConnections.Disconnect(callable);
        }
        else
        {
            Instance?.DisconnectContextSignal(callable);
        }
    }

    bool IDataContext.EqualsContext(IDataContext other)
    {
        return other is ISingletonContext<T>;
    }

    private static bool RerouteSignalsStatically => SingletonNode<T>.UsageAttribute.Usage != SingletonUsage.Autoload;
    
    private static class StaticSignals
    {
        public static readonly CallableEvent UpdateSignalConnections = new();
        public static readonly CallableEvent ContextSignalConnections = new();
        private static T _connectedInstance;

        private static readonly Callable FireUpdateCallable = Callable.From(FireUpdateSignal);
        private static readonly Callable FireContextSignalCallable = Callable.From((StringName signalName, Godot.Collections.Array args) => FireContextSignal(signalName, args));

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            if (Engine.IsEditorHint()) return;

            if (RerouteSignalsStatically)
            {
                SingletonNode<T>.InstanceChangedEvent.Connect(Callable.From(StaticSignals.OnInstanceChanged));
                StaticSignals.OnInstanceChanged();
            }
        }

        public static void FireUpdateSignal()
        {
            UpdateSignalConnections.Invoke();
        }
        public static void FireContextSignal(StringName signalName, Godot.Collections.Array args)
        {
            ContextSignalConnections.Invoke(signalName, args);
        }

        public static void OnInstanceChanged()
        {
            if (_connectedInstance != null)
            {
                _connectedInstance.DisconnectUpdateSignal(FireUpdateCallable);
                _connectedInstance.DisconnectContextSignal(FireContextSignalCallable);
            }
            
            var newInstance = SingletonNode<T>.InstanceExists ? SingletonNode<T>.Instance : null;
            if (newInstance != null)
            {
                newInstance.ConnectUpdateSignal(FireUpdateCallable);
                newInstance.ConnectContextSignal(FireContextSignalCallable);
            }

            _connectedInstance = newInstance;
        }
    }
}