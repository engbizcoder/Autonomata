namespace Automatonymous;

/// <summary>Provides the values visible to an untyped behavior callback.</summary>
/// <typeparam name="TInstance">The state-machine instance type.</typeparam>
public class BehaviorContext<TInstance>
    where TInstance : class
{
    internal BehaviorContext(TInstance instance, Event @event)
    {
        Instance = instance;
        Event = @event;
    }

    /// <summary>Gets the caller-owned instance receiving the event.</summary>
    public TInstance Instance { get; }

    /// <summary>Gets the event being raised.</summary>
    public Event Event { get; }
}

/// <summary>Provides the values visible to a typed behavior callback.</summary>
/// <typeparam name="TInstance">The state-machine instance type.</typeparam>
/// <typeparam name="TData">The event data type.</typeparam>
public sealed class BehaviorContext<TInstance, TData> : BehaviorContext<TInstance>
    where TInstance : class
{
    internal BehaviorContext(TInstance instance, Event<TData> @event, TData data)
        : base(instance, @event) => Data = data;

    /// <summary>Gets the data supplied with the event.</summary>
    public TData Data { get; }
}
