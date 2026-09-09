namespace Automatonymous;

/// <summary>Builds the ordered behavior for one event.</summary>
/// <typeparam name="TInstance">The state-machine instance type.</typeparam>
public class EventActivityBinder<TInstance>
    where TInstance : class
{
    private readonly List<BehaviorStep<TInstance>> _steps = [];

    internal EventActivityBinder(Event @event) => Event = @event;

    internal Event Event { get; }

    internal bool IsIgnored { get; private set; }

    internal IReadOnlyList<BehaviorStep<TInstance>> Steps => _steps;

    /// <summary>Adds a synchronous callback to the behavior.</summary>
    public EventActivityBinder<TInstance> Then(Action<BehaviorContext<TInstance>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        AddStep(BehaviorStep<TInstance>.Callback((instance, _) =>
        {
            callback(new BehaviorContext<TInstance>(instance, Event));
            return Task.CompletedTask;
        }));
        return this;
    }

    /// <summary>Adds an asynchronous callback to the behavior.</summary>
    public EventActivityBinder<TInstance> ThenAsync(Func<BehaviorContext<TInstance>, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        AddStep(BehaviorStep<TInstance>.Callback((instance, _) =>
            callback(new BehaviorContext<TInstance>(instance, Event))));
        return this;
    }

    /// <summary>Adds a transition to the target state.</summary>
    public EventActivityBinder<TInstance> TransitionTo(State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        AddStep(BehaviorStep<TInstance>.Transition(state));
        return this;
    }

    /// <summary>Adds a transition to the distinguished final state.</summary>
    public EventActivityBinder<TInstance> Finalize()
    {
        AddStep(BehaviorStep<TInstance>.FinalizeStep());
        return this;
    }

    /// <summary>Marks the event as accepted without effects or a state change.</summary>
    public EventActivityBinder<TInstance> Ignore()
    {
        if (_steps.Count != 0)
        {
            throw new InvalidOperationException("Ignore cannot be combined with behavior steps.");
        }

        IsIgnored = true;
        return this;
    }

    internal void Validate()
    {
        if (IsIgnored && _steps.Count != 0)
        {
            throw new InvalidOperationException("Ignore cannot be combined with behavior steps.");
        }
    }

    private protected void AddStep(BehaviorStep<TInstance> step)
    {
        if (IsIgnored)
        {
            throw new InvalidOperationException("Behavior steps cannot be added after Ignore.");
        }

        _steps.Add(step);
    }
}

/// <summary>Builds the ordered behavior for one typed event.</summary>
/// <typeparam name="TInstance">The state-machine instance type.</typeparam>
/// <typeparam name="TData">The event data type.</typeparam>
public sealed class EventActivityBinder<TInstance, TData> : EventActivityBinder<TInstance>
    where TInstance : class
{
    private readonly Event<TData> _event;

    internal EventActivityBinder(Event<TData> @event) : base(@event) => _event = @event;

    /// <summary>Adds a synchronous callback that can inspect the event data.</summary>
    public EventActivityBinder<TInstance, TData> Then(Action<BehaviorContext<TInstance, TData>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        AddStep(BehaviorStep<TInstance>.Callback((instance, data) =>
        {
            callback(new BehaviorContext<TInstance, TData>(instance, _event, (TData)data!));
            return Task.CompletedTask;
        }));
        return this;
    }

    /// <summary>Adds an asynchronous callback that can inspect the event data.</summary>
    public EventActivityBinder<TInstance, TData> ThenAsync(Func<BehaviorContext<TInstance, TData>, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        AddStep(BehaviorStep<TInstance>.Callback((instance, data) =>
            callback(new BehaviorContext<TInstance, TData>(instance, _event, (TData)data!))));
        return this;
    }

    /// <inheritdoc />
    public new EventActivityBinder<TInstance, TData> TransitionTo(State state)
    {
        base.TransitionTo(state);
        return this;
    }

    /// <inheritdoc />
    public new EventActivityBinder<TInstance, TData> Finalize()
    {
        base.Finalize();
        return this;
    }

    /// <inheritdoc />
    public new EventActivityBinder<TInstance, TData> Ignore()
    {
        base.Ignore();
        return this;
    }
}

internal enum BehaviorStepKind
{
    Callback,
    Transition,
    Finalize
}

internal sealed record BehaviorStep<TInstance>(
    BehaviorStepKind Kind,
    Func<TInstance, object?, Task>? Invoke,
    State? Target)
    where TInstance : class
{
    internal static BehaviorStep<TInstance> Callback(Func<TInstance, object?, Task> callback) =>
        new(BehaviorStepKind.Callback, callback, null);

    internal static BehaviorStep<TInstance> Transition(State target) =>
        new(BehaviorStepKind.Transition, null, target);

    internal static BehaviorStep<TInstance> FinalizeStep() =>
        new(BehaviorStepKind.Finalize, null, null);
}
