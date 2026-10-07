# Agent instructions for this repository

Before working on a non-trivial change, read [`agent-docs/README.md`](agent-docs/README.md) —
it's a layered technical reference (architecture, domain model, persistence, UI/Canvas,
Automap, Export, CLI, build/test/.NET-8-port notes) written specifically so an agent doesn't
have to re-derive the codebase from scratch each session. Open only the sub-document(s)
relevant to your task; don't load the whole set into context unless you're doing a broad
architectural change.

If your change invalidates something documented there (new file format, new exporter, changed
settings system, restructured controllers, etc.), update the relevant sub-document as part of
the same change.

## Quick facts
- WinForms, `net8.0-windows`, Windows-only (not cross-platform).
- Build: `dotnet build Trizbort.csproj -c Debug`
- Test: `dotnet test Trizbort.Tests\Trizbort.Tests.csproj -c Debug` (the test project is not in
  `Trizbort.sln`; run it directly)
- No undo/redo exists anywhere in the codebase — don't assume one.
