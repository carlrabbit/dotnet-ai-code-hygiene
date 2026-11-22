# Dotnet Library Engineering Profile

Common engineering blocks:

- .NET solution;
- shared build configuration;
- editorconfig;
- explicit project test strategy;
- validation tiers, targets, and execution loci;
- package-consumer qualification at release readiness when public packages are active;
- public API validation when public API compatibility matters;
- release validation when release-ready.

## Test strategy

The profile does not prescribe a test pyramid or require unit tests as the default correctness mechanism.

A project may choose integration-first testing when correctness materially depends on framework, persistence, serialization, filesystem, process, native, installed-runtime, or service boundaries.

Under an integration-first policy, use unit tests where isolated validation is materially cheaper, more exhaustive, or more diagnostic. Do not add unit tests merely to mirror implementation structure.

When integration correctness depends on a concrete external runtime or service, record that runtime as a validation target and keep the technology-specific provisioning, isolation/reset, invocation, evidence, and availability rules in project-local engineering authority.

## Packages

When public packages are active, **release qualification** validates the exact package candidate through an isolated consumer path. Ordinary feature milestones use focused and real-boundary integration tests; they do not automatically need packed-package consumption. Bring consumer qualification forward only when packaging itself is the milestone outcome or deferral is materially risky.
