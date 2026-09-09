using Automatonymous;
using Xunit;

namespace Autonomata.Tests;

public sealed class StateMachineTests
{
    [Fact]
    public async Task Familiar_dsl_executes_initial_behavior_and_transition()
    {
        var machine = new OrderMachine();
        var instance = new OrderState();

        await machine.RaiseEvent(instance, machine.Submit);

        Assert.Equal(["submitted"], instance.Effects);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public async Task Typed_event_exposes_instance_event_and_data()
    {
        var machine = new OrderMachine();
        var instance = new OrderState { CurrentState = machine.Active };

        await machine.RaiseEvent(instance, machine.QuantityChanged, 7);

        Assert.Equal(7, instance.Quantity);
        Assert.Same(machine.QuantityChanged, instance.LastEvent);
    }

    [Fact]
    public async Task Typed_async_behavior_is_awaited_and_can_transition()
    {
        var machine = new TypedAsyncMachine();
        var instance = new OrderState();

        await machine.RaiseEvent(instance, machine.Start, 42);

        Assert.Equal(42, instance.Quantity);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public async Task Typed_ignore_accepts_data_without_effect_or_state_change()
    {
        var machine = new TypedAsyncMachine();
        var instance = new OrderState { CurrentState = machine.Active };

        await machine.RaiseEvent(instance, machine.IgnoreMe, "ignored data");

        Assert.Empty(instance.Effects);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public async Task Typed_finalize_persists_distinguished_final_state()
    {
        var machine = new TypedFinalizeMachine();
        var instance = new OrderState();

        await machine.RaiseEvent(instance, machine.Complete, 23);

        Assert.Same(machine.Final, instance.CurrentState);
    }

    [Fact]
    public async Task Async_behavior_is_awaited_in_declaration_order()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var machine = new OrderedMachine(release.Task);
        var instance = new OrderState();

        var raise = machine.RaiseEvent(instance, machine.Start);
        await machine.AsyncStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(raise.IsCompleted);
        Assert.Equal(["before", "async-start"], instance.Effects);
        Assert.Null(instance.CurrentState);

        release.SetResult();
        await raise;

        Assert.Equal(["before", "async-start", "async-end"], instance.Effects);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public async Task State_specific_behavior_wins_over_during_any_fallback()
    {
        var machine = new OrderMachine();
        var active = new OrderState { CurrentState = machine.Active };
        var suspended = new OrderState { CurrentState = machine.Suspended };

        await machine.RaiseEvent(active, machine.Refresh);
        await machine.RaiseEvent(suspended, machine.Refresh);

        Assert.Equal(["active"], active.Effects);
        Assert.Equal(["fallback"], suspended.Effects);
    }

    [Fact]
    public async Task Ignore_accepts_event_without_effect_or_state_change()
    {
        var machine = new OrderMachine();
        var instance = new OrderState { CurrentState = machine.Active };

        await machine.RaiseEvent(instance, machine.Cancel);

        Assert.Empty(instance.Effects);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public async Task State_specific_ignore_wins_over_during_any_behavior()
    {
        var machine = new IgnorePrecedenceMachine();
        var instance = new OrderState { CurrentState = machine.Active };

        await machine.RaiseEvent(instance, machine.Refresh);

        Assert.Empty(instance.Effects);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public async Task Unhandled_event_identifies_event_and_state()
    {
        var machine = new OrderMachine();
        var instance = new OrderState { CurrentState = machine.Suspended };

        var exception = await Assert.ThrowsAsync<UnhandledEventException>(
            () => machine.RaiseEvent(instance, machine.Cancel));

        Assert.Same(machine.Cancel, exception.Event);
        Assert.Same(machine.Suspended, exception.State);
        Assert.Contains("Cancel", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Suspended", exception.Message, StringComparison.Ordinal);
        Assert.Same(machine.Suspended, instance.CurrentState);
    }

    [Fact]
    public async Task Finalize_transitions_to_distinguished_final_state()
    {
        var machine = new OrderMachine();
        var instance = new OrderState { CurrentState = machine.Active };

        await machine.RaiseEvent(instance, machine.Complete);

        Assert.Same(machine.Final, instance.CurrentState);
    }

    [Fact]
    public async Task Callback_failure_preserves_prior_effects_and_stops_later_steps()
    {
        var machine = new FailureMachine();
        var instance = new OrderState();

        var exception = await Assert.ThrowsAsync<TestFailureException>(
            () => machine.RaiseEvent(instance, machine.Fail));

        Assert.Equal("expected", exception.Message);
        Assert.Equal(["before"], instance.Effects);
        Assert.Null(instance.CurrentState);
    }

    [Fact]
    public async Task Typed_event_preserves_payload_identity_through_ordered_callbacks()
    {
        var payload = new QuantityUpdate(17);
        var machine = new TypedPayloadMachine();
        var instance = new OrderState();

        await machine.RaiseEvent(instance, machine.Update, payload);

        Assert.Same(payload, machine.ObservedPayload);
        Assert.Equal(["first:17", "second:17"], instance.Effects);
        Assert.Same(machine.Active, instance.CurrentState);
    }

    [Fact]
    public void Convention_names_and_populates_declared_machine_members()
    {
        var machine = new OrderMachine();

        Assert.Equal("Active", machine.Active.Name);
        Assert.Equal("Submit", machine.Submit.Name);
        Assert.Equal("QuantityChanged", machine.QuantityChanged.Name);
        Assert.Equal("Initial", machine.Initial.Name);
        Assert.Equal("Final", machine.Final.Name);
        Assert.Equal("Active", machine.Active.ToString());
        Assert.Equal("Submit", machine.Submit.ToString());
    }

    private sealed class OrderState
    {
        public State? CurrentState { get; set; }

        public List<string> Effects { get; } = [];

        public int Quantity { get; set; }

        public Event? LastEvent { get; set; }
    }

    private sealed class OrderMachine : AutomatonymousStateMachine<OrderState>
    {
        public OrderMachine()
        {
            InstanceState(instance => instance.CurrentState!);

            Initially(
                When(Submit)
                    .Then(context => context.Instance.Effects.Add("submitted"))
                    .TransitionTo(Active));

            During(
                Active,
                When(QuantityChanged).Then(context =>
                {
                    context.Instance.Quantity = context.Data;
                    context.Instance.LastEvent = context.Event;
                }),
                When(Refresh).Then(context => context.Instance.Effects.Add("active")),
                When(Cancel).Ignore(),
                When(Complete).Finalize());

            DuringAny(When(Refresh).Then(context => context.Instance.Effects.Add("fallback")));
        }

        public State Active { get; private set; } = null!;

        public State Suspended { get; private set; } = null!;

        public Event Submit { get; private set; } = null!;

        public Event Refresh { get; private set; } = null!;

        public Event Cancel { get; private set; } = null!;

        public Event Complete { get; private set; } = null!;

        public Event<int> QuantityChanged { get; private set; } = null!;
    }

    private sealed class OrderedMachine : AutomatonymousStateMachine<OrderState>
    {
        public OrderedMachine(Task release)
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Start)
                    .Then(context => context.Instance.Effects.Add("before"))
                    .ThenAsync(async context =>
                    {
                        context.Instance.Effects.Add("async-start");
                        AsyncStarted.SetResult();
                        await release;
                        context.Instance.Effects.Add("async-end");
                    })
                    .TransitionTo(Active));
        }

        public TaskCompletionSource AsyncStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public State Active { get; private set; } = null!;

        public Event Start { get; private set; } = null!;
    }

    private sealed class FailureMachine : AutomatonymousStateMachine<OrderState>
    {
        public FailureMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Fail)
                    .Then(context => context.Instance.Effects.Add("before"))
                    .Then(_ => throw new TestFailureException("expected"))
                    .Then(context => context.Instance.Effects.Add("after"))
                    .TransitionTo(Active));
        }

        public State Active { get; private set; } = null!;

        public Event Fail { get; private set; } = null!;
    }

    private sealed class IgnorePrecedenceMachine : AutomatonymousStateMachine<OrderState>
    {
        public IgnorePrecedenceMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            During(Active, When(Refresh).Ignore());
            DuringAny(
                When(Refresh)
                    .Then(context => context.Instance.Effects.Add("fallback"))
                    .TransitionTo(Suspended));
        }

        public State Active { get; private set; } = null!;

        public State Suspended { get; private set; } = null!;

        public Event Refresh { get; private set; } = null!;
    }

    private sealed class TypedPayloadMachine : AutomatonymousStateMachine<OrderState>
    {
        public TypedPayloadMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(
                When(Update)
                    .Then(context =>
                    {
                        ObservedPayload = context.Data;
                        context.Instance.Effects.Add($"first:{context.Data.Quantity}");
                    })
                    .ThenAsync(context =>
                    {
                        context.Instance.Effects.Add($"second:{context.Data.Quantity}");
                        return Task.CompletedTask;
                    })
                    .TransitionTo(Active));
        }

        public State Active { get; private set; } = null!;

        public Event<QuantityUpdate> Update { get; private set; } = null!;

        public QuantityUpdate? ObservedPayload { get; private set; }
    }

    private sealed class TypedAsyncMachine : AutomatonymousStateMachine<OrderState>
    {
        public TypedAsyncMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            State(() => Active);
            Event(() => Start);
            Event(() => IgnoreMe);

            Initially(
                When(Start)
                    .ThenAsync(context =>
                    {
                        context.Instance.Quantity = context.Data;
                        return Task.CompletedTask;
                    })
                    .TransitionTo(Active));

            During(Active, Ignore(IgnoreMe));
        }

        public State Active { get; private set; } = null!;

        public Event<int> Start { get; private set; } = null!;

        public Event<string> IgnoreMe { get; private set; } = null!;
    }

    private sealed class TypedFinalizeMachine : AutomatonymousStateMachine<OrderState>
    {
        public TypedFinalizeMachine()
        {
            InstanceState(instance => instance.CurrentState!);
            Initially(When(Complete).Finalize());
        }

        public Event<int> Complete { get; private set; } = null!;
    }

    private sealed class TestFailureException(string message) : Exception(message);

    private sealed record QuantityUpdate(int Quantity);
}
