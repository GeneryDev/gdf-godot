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
    
    static ISingletonContext()
    {
        if (Engine.IsEditorHint()) return;

        if (RerouteSignalsStatically)
        {
            SingletonNode<T>.InstanceChangedEvent.Connect(Callable.From(StaticSignals.OnInstanceChanged));
            StaticSignals.OnInstanceChanged();
        }
    }

    private static class StaticSignals
    {
        public static readonly CallableEvent UpdateSignalConnections = new();
        public static readonly CallableEvent ContextSignalConnections = new();
        private static T _connectedInstance;

        private static readonly Callable FireUpdateCallable = Callable.From(() => FireUpdateSignal());
        private static readonly Callable FireContextSignalCallable = Callable.From(() => FireContextSignal());

        public static void FireUpdateSignal()
        {
            UpdateSignalConnections.Invoke();
        }
        public static void FireContextSignal()
        {
            ContextSignalConnections.Invoke();
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