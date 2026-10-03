# Public Documentation

The repository is public and the CLI is intended to become an externally consumable developer tool.

## Initial surface

During M0001 the root `README.md` is the initial public documentation surface.

It must accurately state:

- product purpose;
- current maturity/status;
- Windows-first support;
- .NET 11 requirement;
- command name;
- which capabilities are not implemented yet.

Do not publish examples that imply the M0002 hygiene engine or M0003 installed-tool validation already exists.

## Later public documentation

When the corresponding capability exists, public documentation must cover:

- .NET tool installation;
- `hygiene --help`;
- representative commands;
- text and JSON output;
- stable exit semantics relevant to automation;
- project configuration/decision-store behavior users need to know.

A dedicated `public-docs/` tree should be added only when there is enough real content to justify it. Do not create empty public-documentation scaffolding.
