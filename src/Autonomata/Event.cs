namespace Automatonymous;

/// <summary>Identifies an event with no data payload.</summary>
public class Event
{
    internal Event(string name, Type? dataType = null)
    {
        Name = name;
        DataType = dataType;
    }

    /// <summary>Gets the declaration name of the event.</summary>
    public string Name { get; }

    internal Type? DataType { get; }

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>Identifies an event carrying data of type <typeparamref name="TData"/>.</summary>
/// <typeparam name="TData">The event data type.</typeparam>
public sealed class Event<TData> : Event
{
    internal Event(string name) : base(name, typeof(TData))
    {
    }
}
