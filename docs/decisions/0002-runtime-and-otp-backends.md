# Runtime ownership and OTP backends

## Status and current interpretation

Pure OTP modules remain Erlang; C# owns runtime semantics, native primitives and host integration. Original BEAM, retained managed alternatives and generated C#/IL have separate acceptance. No silent fallback or runtime dependency on the original ERTS is implied. The explicit current architecture correction supersedes early wording that put all OTP logic in C#.

The owning current rules are [AGENTS.md](../../AGENTS.md); current acceptance is recorded in the [changelog](../../CHANGELOG.md) and [remaining work](../remaining-work/INDEX.md).

## Context and original decision

4. Use ProcessContext plus async continuations and reduction checkpoints. Tasks are scheduler infrastructure; local lifecycle, signals and selective receive are separately implemented.

11. PowerShell validation and Bash commands in the development-only oracle CI workflow are non-C# test-infrastructure exceptions. Runtime, compiler, inventory, differential runner and benchmark code are C#.

## Consequences and evidence

Read these original statements in their historical checkpoint context. Preserve their limits instead of inferring current completion. [Complete source record](../history/decisions/009.md) and [all decision history](../history/decisions/index.md).
