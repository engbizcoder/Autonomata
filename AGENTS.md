# Agent entry point

This repository contains Autonomata, a standalone .NET state-machine library. It consumes the reusable Agentic Delivery Harness without embedding harness code.

## Read before changing anything

1. `README.md` — purpose, project state, and the current human checkpoint.
2. `docs/specification/first-release.md` — proposed behavior, acceptance scenarios, QA plan, and unresolved product decisions.
3. `CONTEXT.md` — canonical domain vocabulary.
4. `docs/delivery/project-policy.md` — project-specific workflow profile, gates, and role boundaries.
5. `harness.lock.json` — exact reusable harness release and commit.

## Work sequence

1. Confirm that the current workflow checkpoint authorizes the requested role.
2. Use the pinned harness workflow and the project policy; keep role outputs and machine evidence distinct.
3. Preserve the public DSL unless an approved specification explicitly changes it.
4. Record hard-to-reverse, surprising trade-offs as ADRs after a real decision is made.
5. Stop at human specification and merge approvals.

## Guardrails

- Build a clean implementation from the approved Autonomata specification; use Automatonymous only as behavioral migration reference material.
- Keep the runtime independent of MassTransit, GreenPipes, transports, saga infrastructure, scheduling, requests, and observers.
- Treat compatibility claims as executable public-surface obligations, not naming aspirations.
- Preserve unrelated user changes and never publish, merge, or delete without explicit authority.

## Current checkpoint

The first-release specification is approved and Coder implementation is authorized. Run each remaining role fresh, retain deterministic evidence under `docs/delivery/`, and stop for human merge approval after every required gate passes.
