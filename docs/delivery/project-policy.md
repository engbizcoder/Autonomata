# Autonomata delivery policy

Status: active for the approved first release

Harness: `engbizcoder/agentic-delivery-harness` `v0.1.1` at commit `3f041a9c6263174242a841ee19f6b8aa372708e9`, as pinned by `harness.lock.json`.

## Profile

Use the **Standard** workflow for the first release because the change establishes public library behavior and a migration contract:

1. Specifier produces the story, acceptance scenarios, QA plan, assumptions, and exclusions; a human approves the exact revision.
2. Coder creates the .NET solution, library, tests, and implementation on the run-scoped feature branch.
3. Cleaner performs behavior-preserving maintainability work and reports changed-code coverage and complexity.
4. Architect performs a read-only boundary review.
5. Hardener targets mutations in transition selection, ordering, state persistence, and error behavior.
6. QA verifies the approved examples strictly through the public package surface.
7. A human approves the exact revision before merge.

The dedicated Hardener is included even though it is optional in Standard because a state-machine engine can look well covered while tests fail to distinguish incorrect transition selection or ordering.

## Role boundaries

- Specifier may inspect references but does not create production or test code.
- Coder may implement only approved behavior and may not broaden the compatibility claim.
- Cleaner preserves all observable behavior.
- Architect reports violations; fixes return to a fresh Coder execution.
- Hardener may strengthen tests and mutation configuration without changing runtime behavior.
- QA uses only public types and public event-raising operations.

Each role invocation is fresh. Typed handoffs identify input revisions, output artifacts, commits, evidence, assumptions, and unresolved questions.

## Product boundaries

- Runtime target: .NET 10 (`net10.0`).
- Runtime dependencies: none by default. Any proposed runtime package requires a purpose, licensing check, replacement seam, and removal-cost assessment.
- Package and product identity: `Autonomata`; migration-facing public DSL namespace: `Automatonymous`.
- One internal implementation backs the compatibility namespace; parallel public engine hierarchies are out of scope.
- The package owns only an in-process state-machine DSL and executor.
- MassTransit, GreenPipes, transports, saga persistence, correlation, scheduling, request/response, activities tied to those systems, and observer APIs stay outside the runtime and compatibility promise.
- Automatonymous source may inform observable compatibility tests but no source is copied or mechanically translated.

## Deterministic gates

The Coder must turn these proposed commands into executable repository scripts or their direct equivalents during scaffolding:

```text
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
dotnet format --verify-no-changes
dotnet test with repository-configured coverage thresholds
dotnet stryker with repository-configured critical mutation scope
dotnet pack --no-build --configuration Release
```

Ordinarily, gate evidence is captured from a clean `agent/<run-id>` branch at the exact revision under review. This unborn repository cannot satisfy the pinned harness's Git adapter, which requires resolvable `HEAD` and branch revisions. Until a human configures Git author identity and creates the first commit, role handoffs and command evidence are retained under `docs/delivery/` with file hashes and explicit `gitRevision: null`; no claim of harness-enforced revision binding is made. Package inspection confirms `net10.0`, package identity, documentation, symbols/source-link policy, and absence of forbidden runtime dependencies.

## Architecture rules

- Separate immutable machine definition metadata from per-raise execution context.
- Keep instance state access behind an internal seam selected by `InstanceState` configuration.
- Keep behavior selection deterministic and independent of registration collection iteration accidents.
- Expose no transport or persistence abstraction from the core library.
- Test compatibility through consumer-style public API examples, not internal implementation types.

## Execution configuration

The pinned harness release provides deterministic workflow, fake-agent, journal, gate, approval, and Git adapters. It does not ship a reviewed live provider-and-sandbox composition or a consumer installation CLI. A live run therefore remains blocked until a human separately approves a provider, sandbox capability envelope, credentials, and cost budget. The lock file is the supported adoption mechanism demonstrated by the harness's first consumer.

## Current gate

The specification is approved by a durable content-hash record. Coder implementation is authorized. Human merge approval remains mandatory after all deterministic gates and role assessments pass; with no Git revision or base commit, merge cannot occur yet.
