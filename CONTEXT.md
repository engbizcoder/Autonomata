# Autonomata State Machines

Autonomata describes standalone finite-state machines whose behavior is declared as an event-driven DSL and executed against caller-owned instances.

## Language

**State machine**:
A definition of the states, events, and behaviors governing one kind of instance.
_Avoid_: Saga, workflow engine, orchestrator

**Instance**:
Caller-owned data whose current state and other domain values evolve when the state machine raises an event.
_Avoid_: Saga instance, consumer

**State**:
A named condition in which an instance can accept configured events. `Initial` and `Final` are distinguished states.
_Avoid_: Status, stage

**Event**:
A named occurrence raised directly against an instance. An event may be untyped or carry typed data.
_Avoid_: Message, command, transport event

**Behavior**:
The ordered effects configured for an event in a particular state or across all states.
_Avoid_: Pipeline, consumer

**Transition**:
A behavior effect that changes an instance from its current state to a configured target state.
_Avoid_: Move, state update

**Finalization**:
A transition into the distinguished `Final` state.
_Avoid_: Completion policy, saga completion

**Ignored event**:
An event explicitly accepted for a state with no effects and no state change.
_Avoid_: Dropped message, swallowed event

**Unhandled event**:
An event with no matching behavior or ignore declaration for the instance's current state.
_Avoid_: Missing consumer

**Migration compatibility**:
The ability for standalone Automatonymous code within the approved core surface to move to Autonomata with only package, namespace, and documented edge adjustments.
_Avoid_: Drop-in compatibility, fork compatibility
