# Part584 — complete application library alternative

**Status:** implementation complete, focused Windows acceptance passed for both
backends. Universal OTP compatibility remains unproven. Original Erlang/BEAM is
retained unchanged.

Baseline: OTP29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
[Source inventory](source-inventory.json) verifies the original source bytes and
enumerates all **39 non-module_info exports**, including `behaviour_info/1`.
[Backend selection and boundaries](../../application-backends.md).

## Final results

| Scope | Managed | Original BEAM on .NET |
| --- | --- | --- |
| API outcomes, full reasons and stacks, depth8 | 101/101 | 101/101 |
| Connected scenarios at depth8 | 19/19 | 19/19 |
| API outcomes at depth0 | 101/101 | 101/101 |
| API outcomes at depth1 | 101/101 | 101/101 |
| API outcomes at depth3 | 101/101 | 101/101 |
| API outcomes at depth32 | 101/101 | 101/101 |

[Machine-readable matrix](summary.json) names all ten exact reports. Each
`final-depth-*` directory contains the admitted corpus, effects, exports, helper
BEAMs and oracle provenance. API-only depth profiles deliberately run zero of
the19 connected scenarios; their report retains the expected scenario count
and explicitly sets `ApiOnly: true`. They are separate from full depth8
acceptance.

The19 scenarios include real application lifecycle/supervisor/group-leader
queries, persistent env and bulk config, permissions, serial/concurrent diamond
dependencies, startup failure rollback, optional/missing dependencies and40
applications with repeated roots (including a DAG above the flat-map size).
Serial return order is exact. Concurrent success order is projected to app
membership because completion order is unspecified.

Five scenarios call unchanged upstream `application_SUITE` functions:
`get_env`, `set_env`, `set_env_persistent`, `persistent_env`, `set_env_errors`.
The suite and its real `appinc1.app`/`appinc2.app` resources are preserved under
the validator's `Upstream` directory. Their Git blobs are checked against the
pinned reference before capture. No controller, master, file service or CT
result is replaced with a fake response.

The primary fixture boots original `init`, `code_server` and
`application_controller`. Original controller/master/starter/dist_ac/gen_server
and support libraries execute on .NET. C# `application` does not fall back to
original `application`. Ownership checks verify every selected export.
The oracle is the admitted official Windows OTP release, checked against its
execution manifest before and after capture and before replay.

## Build, style and reproducibility

An ordinary dependency build passed with **zero warnings/errors**, without
source exclusions:

```powershell
dotnet build tools/Validation/Erlang.ApplicationCompatibility -m:1 --nologo
```

See [the successful complete build](logs/application-build-024.log). Earlier
focused builds with `BuildProjectReferences=false` used existing dependency
binaries while unrelated ports were being edited; they are labeled in their
logs. The ordinary build above supersedes those preparation gates. The SDK
layout tool checks only copies of this part's files in a temporary layout root,
protecting unrelated dirty sources. [Final layout check](logs/application-style-final.log)
reports zero files needing formatting. Subsequent validator-only formatting
leaves the accepted application source bytes unchanged; the formatted validator
is separately rebuilt and smoke-replayed, as recorded in `post-format/`.

Exact capture/replay commands (from repository root):

```powershell
pwsh -NoProfile -File tools/validate-application-original.ps1 -Output artifacts/application/final-depth-8
dotnet tools/Validation/Erlang.ApplicationCompatibility/bin/Debug/net10.0/Erlang.ApplicationCompatibility.dll . artifacts/application/final-depth-8 Managed
dotnet tools/Validation/Erlang.ApplicationCompatibility/bin/Debug/net10.0/Erlang.ApplicationCompatibility.dll . artifacts/application/final-depth-8 Beam

# Repeat capture with -BacktraceDepth 0, 1, 3 or 32 and a fresh output directory.
# Replay those directories with the final argument: api-only
```

Use a fresh artifacts directory for replay. Do not write new reports into these
immutable snapshot directories. Hash admission refuses changed corpus/helper/
source/fixture/provider inputs; [the corrupted-corpus control](negative-controls.json)
passes by requiring refusal before runtime creation.

[Accepted dependency binary hashes](test-run-binaries.json), report assembly
hashes/MVIDs and source hashes identify the observed builds. The
`source-inventory.json` integration hashes are a shared-working-tree snapshot,
not a claim that every concurrently edited module has independent acceptance.
[Source snapshot](source-snapshot.zip) preserves this part's application and
validation sources. The official reference checkout was read only.

## Retained failures and unrun scope

`history/` and `logs/` preserve initial attempts. The first complete replay was
97/98 plus13/17 on each backend because original `erl_posix_msg` was omitted
from the support closure. Adding that real module corrected missing-file and
optional-dependency observations. Initial startup ownership lacked preloaded
registration; suite imports required explicit admission; upstream env tests
required their real `.app` fixtures and code-server path. Initial concurrent
supervisor/gen_server/sys/io/logger compilation failures, output-copy contention
and the alternate-output validator build failure are retained. No test
expectation was weakened to absorb these failures.

**Unrun:** full regression, `tools/validate.ps1`, canonical readiness
regeneration, complete application_SUITE, distributed takeover/failover/node
loss, complete release/kernel boot, cross-platform/resource-exhaustion and
module_info equivalence. The user explicitly requested related checks only.
Default/global readiness and compatibility classifications are not promoted
from these finite results. No performance advantage is claimed.

No commit, push, PR, merge, hosted CI or package publication is claimed. The
shared dirty checkout contains independently evolving library/runtime work;
publication requires an isolated, source-bound completion receipt.
