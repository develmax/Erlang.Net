# Evidence and compatibility classifications

## Status and current interpretation

API presence, finite comparisons and whole-contract compatibility are separate facts. Historical absence of an original oracle is a condition of that checkpoint, not a present environment claim. Preserve exact evidence, failures and source/revision bindings.

The owning current rules are [AGENTS.md](../../AGENTS.md); current acceptance is recorded in the [changelog](../../CHANGELOG.md) and [remaining work](../remaining-work/INDEX.md).

## Context and original decision

9. Maintain source inventory separately from implemented MFA coverage. Classifications are provisional until component-level reuse/port audits are done.

12. The local exact-version Erlang runtime is absent. Remote differential execution builds the pinned source in isolation; run 37938184184 built successfully and matched 16 cases before an invalid Unicode source literal. Part 009 corrects the earlier transport diagnosis: explicit UTF-8 decoding in a follow-up run still failed because erl_scan excludes U+FFFF. The runner checks the exact release, compares tagged value/error outcomes and does not normalize meaningful term differences. Part 008 passes source as Base64 UTF-8 through an ASCII command, then explicitly scans/evaluates codepoints. Partial reports distinguish infrastructure aborts from semantic failures. Complete stacktrace/side-effect/signal comparison remains a separate future track.

## Consequences and evidence

Read these original statements in their historical checkpoint context. Preserve their limits instead of inferring current completion. [Complete source record](../history/decisions/009.md) and [all decision history](../history/decisions/index.md).
