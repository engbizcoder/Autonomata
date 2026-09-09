namespace Automatonymous;

/// <summary>Thrown when an event has no behavior or ignore declaration for the current state.</summary>
public sealed class UnhandledEventException : InvalidOperationException
{
    internal UnhandledEventException(Event @event, State state)
        : base($"The event '{@event.Name}' is not handled in state '{state.Name}'.")
    {
        Event = @event;
        State = state;
    }

    /// <summary>Gets the unhandled event.</summary>
    public Event Event { get; }

    /// <summary>Gets the state in which the event was raised.</summary>
    public State State { get; }
}
