# Autonomata

Autonomata is a small, standalone .NET state-machine library for declaring and executing finite-state machines in process. Its name comes from _automata theory_, the mathematical foundation for finite-state machines.

The project is an independent clean rewrite for developers migrating ordinary standalone code from Automatonymous 5.1.3. The NuGet package and assembly are named `Autonomata`, while the migration-facing API deliberately remains in the `Automatonymous` namespace.

> Autonomata is an independent project. It is not affiliated with, maintained by, or endorsed by MassTransit or the Automatonymous maintainers.

## Status and scope

The first-release implementation targets .NET 10 and covers this focused DSL:

- `AutomatonymousStateMachine<TInstance>`, `State`, `Event`, and `Event<TData>`
- `InstanceState`, `Initially`, `During`, and `DuringAny`
- `When`, `Then`, `ThenAsync`, `TransitionTo`, `Finalize`, and `Ignore`
- asynchronous typed and untyped `RaiseEvent`

The runtime has no external package dependencies. It intentionally excludes MassTransit, GreenPipes, transports, consumers, saga infrastructure or repositories, persistence orchestration, scheduling, requests, observers, hierarchical states, composite events, retry policies, and thread-safe concurrent raises against the same instance. See the [approved first-release specification](docs/specification/first-release.md) for the exact contract.

## Installation

Autonomata is **not currently published to NuGet.org**. Build a local package from this repository:

```bash
git clone https://github.com/engbizcoder/Autonomata.git
cd Autonomata
./scripts/restore.sh
./scripts/build.sh
./scripts/pack.sh
```

The package is written to `artifacts/packages/Autonomata.0.1.0.nupkg`. From a .NET 10 consumer project, reference that local package:

```bash
dotnet add package Autonomata \
  --version 0.1.0 \
  --source /absolute/path/to/Autonomata/artifacts/packages
```

For source-based development, add a project reference instead:

```bash
dotnet add reference /absolute/path/to/Autonomata/src/Autonomata/Autonomata.csproj
```

## Minimal example

The instance owns its state and domain data. A null state is treated as the distinguished `Initial` state.

```csharp
using Automatonymous;

public sealed class Order
{
    public State? State { get; set; }
    public List<string> History { get; } = [];
    public int Quantity { get; set; }
}

public sealed class OrderMachine : AutomatonymousStateMachine<Order>
{
    public OrderMachine()
    {
        // These explicit declarations are optional because writable State/Event
        // properties are also discovered and named by convention.
        State(() => Active);
        Event(() => Submitted);
        Event(() => QuantityChanged);
        Event(() => Cancelled);
        Event(() => Completed);

        InstanceState(order => order.State!);

        Initially(
            When(Submitted)
                .Then(context => context.Instance.History.Add("submitted"))
                .ThenAsync(async context =>
                {
                    await Task.Yield();
                    context.Instance.History.Add("validated");
                })
                .TransitionTo(Active));

        During(
            Active,
            When(QuantityChanged).Then(context =>
                context.Instance.Quantity = context.Data),
            Ignore(Cancelled),
            When(Completed).Finalize());
    }

    public State Active { get; private set; } = null!;
    public Event Submitted { get; private set; } = null!;
    public Event<int> QuantityChanged { get; private set; } = null!;
    public Event Cancelled { get; private set; } = null!;
    public Event Completed { get; private set; } = null!;
}

public static class Program
{
    public static async Task Main()
    {
        var machine = new OrderMachine();
        var order = new Order();

        await machine.RaiseEvent(order, machine.Submitted);
        await machine.RaiseEvent(order, machine.QuantityChanged, 7);
        await machine.RaiseEvent(order, machine.Cancelled); // accepted, no effects
        await machine.RaiseEvent(order, machine.Completed);

        Console.WriteLine(order.Quantity);                // 7
        Console.WriteLine(order.State == machine.Final); // True
    }
}
```

`Event<TData>` supplies strongly typed data through `context.Data`; both typed and untyped contexts expose `context.Instance` and `context.Event`.

## Execution rules

Behavior selection is deterministic. A behavior or ignore declaration for the instance's exact current state wins. `DuringAny` is only a fallback when no state-specific declaration exists; the two are never combined.

Steps run serially in declaration order, and `RaiseEvent` does not complete until every `ThenAsync` callback completes. A transition or finalization changes the instance state only when execution reaches that step.

If no behavior or ignore declaration matches, `RaiseEvent` throws `UnhandledEventException`, which identifies the event and current state. If a callback throws, the same failure is propagated, later callbacks and transitions do not run, and earlier effects are not rolled back. Autonomata provides ordering, not transactions; callers own persistence and recovery.

## Migrating from Automatonymous 5.1.3

For code limited to the supported standalone surface, replace the package reference with `Autonomata` and keep:

```csharp
using Automatonymous;
```

Representative 5.1.3 state/event declarations, callbacks, transitions, ignores, finalization, and event raising are covered by package-only migration QA. This is source-oriented compatibility for the documented subset—not binary compatibility or a claim that every Automatonymous API is implemented.

Notable boundaries and adjustments:

- The instance state property is `State`-typed and configured with `InstanceState`.
- `RaiseEvent` is asynchronous for typed and untyped events and should be awaited.
- Callback contexts are Autonomata-owned public types with equivalent `Instance`, `Event`, and typed `Data` values; exact historical context-interface identity is not promised.
- Infrastructure-coupled Automatonymous and MassTransit features are intentionally unavailable.

## Development

Install the .NET SDK selected by [`global.json`](global.json), then run the repository gates from its root:

```bash
./scripts/restore.sh    # locked restore
./scripts/build.sh      # Release build
./scripts/test.sh       # test suite
./scripts/format.sh     # formatting verification
./scripts/coverage.sh   # tests with the 90% line-coverage threshold
./scripts/mutation.sh   # focused mutation gate (80% break threshold)
./scripts/pack.sh       # local NuGet and symbol packages
```

The approved specification, architecture decision, role reports, and deterministic gate evidence are retained under [`docs/`](docs/). Autonomata is licensed under the [MIT License](LICENSE).
