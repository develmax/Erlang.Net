# Runtime ownership and OTP backends

## Status and current interpretation

The OTP library architecture has two target implementations per library. The pinned original preserves upstream algorithms in Erlang and executes as BEAM in .NET. The managed implementation provides the same library algorithms in C#. Select the backend independently per module at startup. Compare both implementations against pinned OTP and record their acceptance separately. C# also provides shared runtime semantics, native primitives and host integration. Generated C#/IL from the Erlang compiler has separate acceptance.

The owning current rules are [AGENTS.md](../../AGENTS.md); current acceptance is recorded in the [changelog](../../CHANGELOG.md) and [remaining work](../remaining-work/INDEX.md).

## Context and original decision

4. Use ProcessContext plus async continuations and reduction checkpoints. Tasks are scheduler infrastructure; local lifecycle, signals and selective receive are separately implemented.

11. PowerShell validation and Bash commands in the development-only oracle CI workflow are non-C# test-infrastructure exceptions. Runtime, compiler, inventory, differential runner and benchmark code are C#.

## Consequences and evidence

Read these original statements in their historical checkpoint context. Preserve their limits instead of inferring current completion. [Complete source record](../history/decisions/009.md) and [all decision history](../history/decisions/index.md).
