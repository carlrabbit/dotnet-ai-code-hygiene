# Repository Role Model

Repository role prevents confusing capability implementation with capability usage.

| Role | Meaning |
|---|---|
| `capability-provider` | Builds a reusable framework, tool, runtime, engine, compiler, SDK, generator, validator, or authoring system. |
| `capability-consumer` | Uses a capability to build a product, game, content set, application, or authored artifact. |
| `mixed-dogfood` | Builds a capability and includes bounded consumer-style samples or dogfood projects. |
| `application-product` | Builds an end-user application or product. |
| `documentation-only` | Maintains documentation without implementation ownership. |

Core rule:

```text
Do not confuse implementing a capability with using that capability.
```
