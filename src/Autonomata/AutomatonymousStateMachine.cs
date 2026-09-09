using System.Linq.Expressions;
using System.Reflection;

namespace Automatonymous;

/// <summary>Defines and executes a standalone finite-state machine for <typeparamref name="TInstance"/>.</summary>
/// <typeparam name="TInstance">The caller-owned instance type.</typeparam>
public abstract class AutomatonymousStateMachine<TInstance>
    where TInstance : class
{
    private readonly Dictionary<(State State, Event Event), ConfiguredBehavior<TInstance>> _stateBehaviors = [];
    private readonly Dictionary<Event, ConfiguredBehavior<TInstance>> _anyBehaviors = [];
    private IStateAccessor<TInstance>? _stateAccessor;
    private bool _frozen;

    /// <summary>Initializes a new machine and conventionally populates declared State and Event properties.</summary>
    protected AutomatonymousStateMachine()
    {
        Initial = new State(nameof(Initial));
        Final = new State(nameof(Final));
        InitializeDeclaredProperties();
    }

    /// <summary>Gets the distinguished initial state. A null stored state is interpreted as Initial.</summary>
    public State Initial { get; }

    /// <summary>Gets the distinguished final state.</summary>
    public State Final { get; }

    /// <summary>Declares a state property using the familiar Automatonymous syntax.</summary>
    protected void State(Expression<Func<State>> stateExpression)
    {
        EnsureConfigurable();
        DeclareProperty(stateExpression, property => new State(property.Name));
    }

    /// <summary>Declares an untyped event property using the familiar Automatonymous syntax.</summary>
    protected void Event(Expression<Func<Event>> eventExpression)
    {
        EnsureConfigurable();
        DeclareProperty(eventExpression, property => new Event(property.Name));
    }

    /// <summary>Declares a typed event property using the familiar Automatonymous syntax.</summary>
    protected void Event<TData>(Expression<Func<Event<TData>>> eventExpression)
    {
        EnsureConfigurable();
        DeclareProperty(eventExpression, property => new Event<TData>(property.Name));
    }

    /// <summary>Configures the instance property used to read and write state.</summary>
    protected void InstanceState(Expression<Func<TInstance, State>> stateExpression)
    {
        EnsureConfigurable();
        ArgumentNullException.ThrowIfNull(stateExpression);

        if (stateExpression.Body is not MemberExpression { Member: PropertyInfo property } ||
            property.PropertyType != typeof(State) ||
            property.SetMethod is null)
        {
            throw new ArgumentException("InstanceState requires a writable State property expression.", nameof(stateExpression));
        }

        _stateAccessor = new PropertyStateAccessor<TInstance>(stateExpression.Compile(), property);
    }

    /// <summary>Begins an untyped event behavior.</summary>
    protected EventActivityBinder<TInstance> When(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (@event.DataType is not null)
        {
            throw new ArgumentException("Use the typed When overload for a typed event.", nameof(@event));
        }

        return new EventActivityBinder<TInstance>(@event);
    }

    /// <summary>Begins a typed event behavior.</summary>
    protected EventActivityBinder<TInstance, TData> When<TData>(Event<TData> @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new EventActivityBinder<TInstance, TData>(@event);
    }

    /// <summary>Configures an untyped event to be ignored.</summary>
    protected EventActivityBinder<TInstance> Ignore(Event @event) => When(@event).Ignore();

    /// <summary>Configures a typed event to be ignored.</summary>
    protected EventActivityBinder<TInstance, TData> Ignore<TData>(Event<TData> @event) => When(@event).Ignore();

    /// <summary>Configures behaviors in the distinguished initial state.</summary>
    protected void Initially(params EventActivityBinder<TInstance>[] behaviors) => During(Initial, behaviors);

    /// <summary>Configures behaviors for a specific state.</summary>
    protected void During(State state, params EventActivityBinder<TInstance>[] behaviors)
    {
        EnsureConfigurable();
        ArgumentNullException.ThrowIfNull(state);
        AddBehaviors(behaviors, behavior => _stateBehaviors.Add((state, behavior.Event), behavior));
    }

    /// <summary>Configures fallback behaviors used when the current state has no specific behavior.</summary>
    protected void DuringAny(params EventActivityBinder<TInstance>[] behaviors)
    {
        EnsureConfigurable();
        AddBehaviors(behaviors, behavior => _anyBehaviors.Add(behavior.Event, behavior));
    }

    /// <summary>Raises an untyped event and awaits all configured effects.</summary>
    public Task RaiseEvent(TInstance instance, Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (@event.DataType is not null)
        {
            throw new ArgumentException("Typed events must be raised with event data.", nameof(@event));
        }

        return RaiseEventCore(instance, @event, null);
    }

    /// <summary>Raises a typed event and awaits all configured effects.</summary>
    public Task RaiseEvent<TData>(TInstance instance, Event<TData> @event, TData data)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return RaiseEventCore(instance, @event, data);
    }

    private async Task RaiseEventCore(TInstance instance, Event @event, object? data)
    {
        ArgumentNullException.ThrowIfNull(instance);
        _frozen = true;
        var accessor = _stateAccessor ?? throw new InvalidOperationException("InstanceState must be configured before raising events.");
        var state = accessor.Get(instance) ?? Initial;

        if (!_stateBehaviors.TryGetValue((state, @event), out var behavior) &&
            !_anyBehaviors.TryGetValue(@event, out behavior))
        {
            throw new UnhandledEventException(@event, state);
        }

        if (behavior.IsIgnored)
        {
            return;
        }

        foreach (var step in behavior.Steps)
        {
            switch (step.Kind)
            {
                case BehaviorStepKind.Callback:
                    await step.Invoke!(instance, data).ConfigureAwait(false);
                    break;
                case BehaviorStepKind.Transition:
                    accessor.Set(instance, step.Target!);
                    break;
                case BehaviorStepKind.Finalize:
                    accessor.Set(instance, Final);
                    break;
                default:
                    throw new InvalidOperationException("Unknown behavior step.");
            }
        }
    }

    private void AddBehaviors(
        EventActivityBinder<TInstance>[] behaviors,
        Action<ConfiguredBehavior<TInstance>> add)
    {
        ArgumentNullException.ThrowIfNull(behaviors);
        foreach (var binder in behaviors)
        {
            ArgumentNullException.ThrowIfNull(binder);
            binder.Validate();
            add(new ConfiguredBehavior<TInstance>(binder.Event, binder.IsIgnored, [.. binder.Steps]));
        }
    }

    private void InitializeDeclaredProperties()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var property in GetType().GetProperties(flags))
        {
            if (property.GetIndexParameters().Length != 0 || property.SetMethod is null || property.GetValue(this) is not null)
            {
                continue;
            }

            object? value = property.PropertyType == typeof(State)
                ? new State(property.Name)
                : property.PropertyType == typeof(Event)
                    ? new Event(property.Name)
                    : CreateTypedEvent(property);

            if (value is not null)
            {
                property.SetValue(this, value);
            }
        }
    }

    private static object? CreateTypedEvent(PropertyInfo property)
    {
        if (!property.PropertyType.IsGenericType || property.PropertyType.GetGenericTypeDefinition() != typeof(Event<>))
        {
            return null;
        }

        return Activator.CreateInstance(
            property.PropertyType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [property.Name],
            culture: null);
    }

    private void DeclareProperty<TMember>(
        Expression<Func<TMember>> expression,
        Func<PropertyInfo, TMember> factory)
        where TMember : class
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (expression.Body is not MemberExpression { Member: PropertyInfo property } ||
            property.PropertyType != typeof(TMember) ||
            property.SetMethod is null)
        {
            throw new ArgumentException("A writable state-machine property expression is required.", nameof(expression));
        }

        if (property.GetValue(this) is null)
        {
            property.SetValue(this, factory(property));
        }
    }

    private void EnsureConfigurable()
    {
        if (_frozen)
        {
            throw new InvalidOperationException("State-machine configuration cannot change after event execution begins.");
        }
    }
}

internal sealed record ConfiguredBehavior<TInstance>(
    Event Event,
    bool IsIgnored,
    IReadOnlyList<BehaviorStep<TInstance>> Steps)
    where TInstance : class;

internal interface IStateAccessor<TInstance>
    where TInstance : class
{
    State? Get(TInstance instance);

    void Set(TInstance instance, State state);
}

internal sealed class PropertyStateAccessor<TInstance>(
    Func<TInstance, State> getter,
    PropertyInfo property) : IStateAccessor<TInstance>
    where TInstance : class
{
    public State? Get(TInstance instance) => getter(instance);

    public void Set(TInstance instance, State state) => property.SetValue(instance, state);
}
