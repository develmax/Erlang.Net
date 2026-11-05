# Part586 — erts_internal implementation and focused acceptance

Date: 2026-11-05. Baseline: OTP-29.1.1,
`ad05823719d77c8faee87348ea39513d4e2f99c5`.

All 98 explicit exports are available in the selected C# alternative: 27
ported library functions and 71 shared native primitive signatures. Original
source/BEAM hashes are unchanged. Compatibility is partial, with a retained
failure in both backends. No full regression or canonical readiness refresh.

| Check | Actual result |
| --- | --- |
| Focused clean snapshot build, final `scoped-build-9.log` | 0 warnings, 0 errors |
| Permanent scoped-build script, `repro-build.log` | 0 warnings, 0 errors |
| Managed `erts-internal-compatibility-v7/Managed-report.json` | 262/262 primary, 34/35 scenarios |
| Original BEAM `erts-internal-compatibility-v7/Beam-report.json` | 262/262 primary, 34/35 scenarios |
| `connected/final-v7-connected.json` | 17/17 selected connected groups |
| SDK layout check on owned C# sources | 0 files need formatting |
| Final working checkout build with separate output, `final-working-build.log` | 0 warnings/errors, no source exclusions |
| Final working-tree acceptance, `erts-internal-working-acceptance/*-report.json` | both262/262 primary and34/35 scenarios |
| Earlier working checkout builds | unrelated Sys/ErrorLogger drafts and shared compiler-output contention retained |

The failure is `blocked`: official OTP returns `{error,undef}` and both .NET
alternatives return `{error,badarg}`. The missing real multi-scheduling flag
prevents `get_internal_state_blocked/1` from reaching the native debug operation.
Neither a successful export count nor the other passing observations closes it.

## Exact commands

```powershell
pwsh -NoProfile -File tools/Validation/Erlang.ErtsInternalCompatibility/build-scoped.ps1 -Output artifacts/erts-internal-repro
pwsh -NoProfile -File tools/validate-erts-internal-original.ps1 -Output artifacts/erts-internal-compatibility-v6 -CaptureOnly
dotnet artifacts/erts-internal-build/snapshot/tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll . artifacts/erts-internal-compatibility-v7 Managed
dotnet artifacts/erts-internal-build/snapshot/tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll . artifacts/erts-internal-compatibility-v7 Beam
```

The snapshot base is `20e02eb941ef3b99cb675827ef0f20663da7c04b`.
The first ad hoc snapshot included an unrelated ETS bootstrap convenience file;
the final snapshot removes it, and the validator now owns its explicit ETS
bootstrap. The permanent script independently builds the final scoped sources
without that file. `sources.json` binds the owned implementation to its tested
snapshot; `status.json` lists all 98 signatures and the separate acceptance gate.
The two final comparison commands exit 1 because the recorded failure remains.

The connected run uses a copied prebuilt Erlang.Tests harness with the final
candidate runtime, terms, serialization and stdlib assemblies, in its own
directory. It does not rebuild or execute the full test catalog. Exact selected
names and command arguments are in `connected/command.json`, and binary hashes
are in `connected/assemblies.json`. The earlier `connected.json` run predates
the final source-frame/lambda-recipe refinements; `final-connected.json` precedes the final stack traversal correction;
`final-v7-connected.json` with `final-v7-assemblies.json` is the final candidate run. These are distinct from a fresh full tests-project build.

## Preserved history and comparison boundaries

`erts-internal-compatibility` captures the initial observer error: collecting
outcomes directly in the oracle writer added writer/erl_eval/init parent frames
that were absent in the .NET workers. Version 2 uses identical real Erlang worker
functions in both environments. No stack frames are stripped from comparisons.

Version 3 closes the initial C# source-frame differences. Version 5 expands
spawn, distributed-data and process-code failures and records two sources of
nonportable identity: fresh request references and native atom index order.
Version 6 projects only the fresh request reference in its exact error-frame
field, preserving reason, arguments, source frames and error metadata. It checks
atom map printing against the actual native flat-key tuple and keeps a separate
integer-key map's complete string comparison. Neither projection affects the
retained blocked-state failure. No case is retried to obtain a passing result.

The scenarios include actual `system_flag(microstate_accounting,...)` and
`statistics(microstate_accounting)` calls, so the selected statistics helper
route is exercised in addition to direct aggregation tests.

## Remaining acceptance

Implement real `multi_scheduling` blocker semantics and rerun the retained
failure on both backends. Cover nested blockers, process death, normal and dirty
scheduler admission and restoration. Then extend full upstream, physical
resources, distribution, platforms, complete startup dependencies and metadata
acceptance. Global readiness is deliberately not regenerated from partial
reports. No commit, push, PR, merge, hosted CI or universal compatibility is
claimed for this unfinished compatibility checkpoint.

Version7 replays the unchanged version6 corpus after replacing a reference-identity
search through reconstructed stack terms with direct list traversal. This avoids
losing parent frames when owned terms reify new wrappers. Both comparisons retain
the same sole failure. A fresh build of the actual dirty working checkout with
`dotnet build tools/Validation/Erlang.ErtsInternalCompatibility/Erlang.ErtsInternalCompatibility.csproj -m:1 --nologo --artifacts-path artifacts/erts-internal-working-build`
also passes without source exclusions; its own separate replay agrees with the
scoped snapshot. The permanent-script reproduction log precedes the final small
stack traversal correction; final scoped/current-tree builds and v7 reports
cover that correction. No global regression was run.
