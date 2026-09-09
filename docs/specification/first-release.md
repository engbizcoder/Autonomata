# Autonomata first-release specification

Status: approved on 2026-09-09; see `docs/delivery/approvals/first-release-specification.json`

## Feature story

As a .NET developer using Automatonymous as a standalone in-process state-machine library, I want a focused Autonomata package with the familiar core DSL so that I can migrate ordinary state definitions with minimal code changes and without taking dependencies on MassTransit infrastructure.

## Product intent

Autonomata is a clean, open-source .NET 10 implementation of a finite-state-machine library. Its name refers to automata theory and finite-state machines. It is a migration-friendly spiritual successor for standalone Automatonymous use, independently developed and neither affiliated with nor endorsed by MassTransit.

Migration-friendly means the approved core examples should require a package-reference change while retaining `using Automatonymous;`, plus explicitly documented adjustments where exact compatibility would create ambiguity or pull excluded infrastructure into the library. It does not mean binary compatibility or compatibility with every Automatonymous API.

## Proposed public surface

The first release includes these concepts and DSL operations:

- `AutomatonymousStateMachine<TInstance>`
- `State`
- `Event`
- `Event<TData>`
- `InstanceState`
- `Initially`
- `During`
- `DuringAny`
- `When`
- `Then`
- `ThenAsync`
- `TransitionTo`
- `Finalize`
- `Ignore`
- `RaiseEvent`

The NuGet package ID and product identity are `Autonomata`. The migration-facing DSL remains in the `Automatonymous` namespace. One internal Autonomata engine backs that public compatibility surface; there is no duplicated engine or parallel public type hierarchy. Public members use nullable annotations and XML documentation. Implementation types not required by consumer code remain internal.

## Acceptance scenarios

### Scenario: declare a familiar state machine

```gherkin
Given a consumer defines a class derived from AutomatonymousStateMachine<OrderState>
And declares State, Event, and Event<TData> properties on the machine
And configures InstanceState, Initially, During, DuringAny, When, Then, ThenAsync, TransitionTo, Finalize, and Ignore in its constructor
When the consumer project is compiled against Autonomata
Then the machine compiles using the approved public DSL and its existing Automatonymous namespace import
And no MassTransit or GreenPipes package is referenced transitively
```

### Scenario: raise an untyped initial event

```gherkin
Given an instance is in the Initial state
And the Initial behavior for Submitted records an effect and transitions to Active
When Submitted is raised against the instance
Then the effect runs exactly once
And the instance state is Active after the raise completes
```

### Scenario: expose typed event data

```gherkin
Given an instance is in Active
And the behavior for QuantityChanged carries integer data
When QuantityChanged is raised with 7
Then a Then callback can read the instance and the value 7 from its public context
And the callback runs exactly once
```

### Scenario: await asynchronous behavior in declaration order

```gherkin
Given an event behavior contains Then, ThenAsync, and TransitionTo in that order
When the event is raised
Then each effect starts only after the previous effect completes
And RaiseEvent does not complete until the asynchronous effect completes
And the transition occurs after both callbacks
```

### Scenario: use a state-specific behavior

```gherkin
Given an event has different behaviors configured in Active and Suspended
And the instance is Suspended
When the event is raised
Then only the Suspended behavior runs
```

### Scenario: use a behavior shared by states

```gherkin
Given an event has a DuringAny behavior
And the instance is in a state without a state-specific behavior for that event
When the event is raised
Then the DuringAny behavior runs
```

### Scenario: prefer explicit state behavior

```gherkin
Given an event has both a DuringAny behavior and a behavior for the current state
When the event is raised
Then only the current-state behavior runs
```

This precedence is a proposed deterministic rule and requires explicit approval.

### Scenario: ignore an event

```gherkin
Given an event is ignored in the instance's current state
When the event is raised
Then RaiseEvent completes successfully
And no callback runs
And the instance state is unchanged
```

### Scenario: reject an unhandled event

```gherkin
Given an event is neither handled nor ignored in the instance's current state
When the event is raised
Then RaiseEvent fails with a documented Autonomata exception identifying the event and state
And the instance state is unchanged
```

### Scenario: finalize an instance

```gherkin
Given the current behavior ends with Finalize
When its event is raised successfully
Then the instance state is Final
```

### Scenario: preserve state on callback failure

```gherkin
Given a behavior callback throws before its transition
When the event is raised
Then RaiseEvent propagates the failure
And the transition does not run
And effects completed before the failure are not rolled back
```

Autonomata provides ordering, not transactions. This proposed failure rule requires explicit approval.

### Scenario: migrate an approved standalone sample

```gherkin
Given a standalone Automatonymous 5.1.3 sample uses only the first-release core surface
When its package reference is changed to Autonomata
Then the sample compiles without structural DSL rewrites
And its Automatonymous namespace import remains unchanged
And its observable callbacks and state transitions match the approved behavior
```

## Human QA plan

1. Create a clean .NET 10 console or test consumer outside the library project.
2. Add the locally packed Autonomata package and verify that restore adds no runtime dependency other than the .NET framework.
3. Paste the approved Automatonymous 5.1.3 migration sample, changing only its package reference.
4. Exercise untyped and typed events through `RaiseEvent` and inspect callback order, supplied instance/data, and final instance state.
5. Exercise a state-specific behavior, a `DuringAny` fallback, an ignored event, and an unhandled event.
6. Block a `ThenAsync` callback with a controllable task and confirm that `RaiseEvent` remains incomplete until released.
7. Throw from a callback before a transition and confirm the documented error and state behavior.
8. Pack the library, inspect package metadata and contents, and confirm the independence disclaimer is visible in the README/package description.

QA must consume only the package's public API. It may not use reflection over internal types or mutate machine metadata.

## Explicitly out of scope

- MassTransit integration or types
- GreenPipes integration or types
- Message transports, consumers, endpoints, publish, send, or correlation
- Saga repositories, persistence orchestration, concurrency control, or completion policies
- Scheduling, timeouts, or delayed delivery
- Request/response DSLs
- Observer, probing, visitation, graphing, or topology APIs
- Automatonymous binary compatibility or a claim of full source compatibility
- Hierarchical states, composite events, conditional branches, exception policies, retry, activities, dependency injection, serialization helpers, and code generation unless separately specified
- Thread-safe concurrent raises against the same instance in the first release

## Assumptions

- .NET 10 means a `net10.0` target rather than multi-targeting older frameworks.
- Instance state is stored only through a configured `State`-typed property in the first release.
- Public behavior contexts are Autonomata-owned types exposing equivalent `Instance`, `Event`, and typed `Data` values; exact Automatonymous context-interface identity is not promised.
- A state-specific behavior takes precedence over `DuringAny`, which is a fallback only.
- State-machine configuration is complete after construction and treated as immutable during event execution.
- `RaiseEvent` is asynchronous for both typed and untyped events so one execution model can correctly await `ThenAsync`.
- A single event raise executes serially and deterministically.
- Callback failure preserves already-completed effects, prevents later effects and transitions, and provides no rollback.
- The library owns no persistence; the caller retains and stores the instance.
- The clean rewrite may study documented/public behavior and consumer examples, but copies no Automatonymous source.

## Approval record

The human owner approved the specification defaults on 2026-09-09, including MIT licensing, the Automatonymous 5.1.3 compatibility baseline, `net10.0`, package ID `Autonomata`, and the retained `Automatonymous` public namespace. The durable approval artifact is content-hash scoped because this repository has no commits and no configured Git author; the harness correctly cannot produce revision-bound Git evidence in that condition. Any changed behavior requires a new approval artifact.
