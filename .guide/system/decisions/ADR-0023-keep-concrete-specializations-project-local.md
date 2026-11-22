# 0023 — Keep Concrete Specializations Project-Local

## Status

Accepted.

## Context

The guide repository contained a concrete `project-types/agentic-2d-game-engine/` layer. Continuing that model would encourage central folders for increasingly specific products, vendor integrations, runtime stacks, and architectures.

That conflicts with the guide-system boundary that product repositories contain project truth.

At the same time, generic profiles alone are intentionally too broad to define concrete rules for external runtimes, services, test environments, installed applications, or domain-specific architectures.

## Decision

The central guide repository owns:

- base methodology;
- broad reusable profiles;
- generic engineering/validation models;
- planning and migration templates.

Concrete technology/runtime/architecture specializations are ordinary project-local authority.

Do not maintain a central `project-types/` catalog for concrete project archetypes.

Planning uses the generic guide to identify what must be decided, then records the concrete answer in the product repository's normal engineering, specification, architecture, or decision documents.

Profiles remain broad reusable engineering shapes and must not become feature tags.

## Consequences

- remove the existing concrete game-engine project-type guide;
- new projects do not require a central guide folder before they can specialize generic guidance;
- named vendor/runtime integrations such as Power BI remain project-local unless a vendor-independent reusable abstraction emerges;
- reusable concepts may be promoted from multiple project specializations into generic guidance when they generalize cleanly;
- the guide repository scales by reusable decision surfaces rather than by mirroring the portfolio of concrete projects.
