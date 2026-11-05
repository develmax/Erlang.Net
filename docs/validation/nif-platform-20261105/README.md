> Historical local-only checkpoint; see [hosted CI continuation](../nif-platform-ci-20261105/README.md) for later runs and fixes.

# 2026-11-05 — OTP-style NIF platform checkpoint

**Status: partially implemented; target acceptance pending.**

Objective: extend existing NIF code using the pinned OTP platform model while
preserving pre-existing uncommitted work. The request authorizes implementation;
it does not turn cross-compilation into target runtime evidence.

## Changes

- Add an LP64 Unix native loader/file/ABI adapter and explicit process-owned
  callback registration. Retain glibc namespaces on supported Linux hosts.
- Generate x64/AArch64 ELF capsules and use the actual host page size.
- Add OTP's Unix `_nif_init` fallback.
- Build Windows ARM64 varargs assets; route rejecting variadic `set_option`
  through C. Preserve the old x64 adapter fallback for existing deployments.
- Select/copy/package native assets by RID; ship separately built x64/ARM64 DLLs.
- Add permanent native/managed platform checks, a pinned-header Unix builder
  and a manual target-execution workflow. No workflow was dispatched.

## Decisions and compatibility

Reference: OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5.
[Source audit, reuse/license decision and platform matrix](../../nif-platform-portability.md)
are part of this checkpoint. No new package is added. Runtime ownership and
reviewed deployment restrictions are explicit. Full OTP platform parity,
32-bit layouts, arbitrary Mach-O binary compatibility and portable multi-runtime
isolation remain unfinished. No registered Erlang MFAs were added by this work.

## Validation

| Check | Actual result |
| --- | --- |
| Initial shared-tree focused builds | Passed, zero warnings/errors before concurrent sys edits |
| Final shared-tree focused build | Failed in unrelated `ProcessRuntime.LibrarySelection.cs`: incomplete `RuntimeManagedLibraryStage456.Sys`; preserved in `shared-final-build.log` |
| Final clean HEAD + owned-files snapshot build | Passed, zero warnings/errors; `isolated-final-build.log` |
| Focused managed platform executable on Windows x64 | 6 checks passed: admission, layout, ELF generation, missing-file classification, native adapter and actual runtime callback slot |
| Existing `Erlang.NifNativeBackend` build and no-argument managed checks | Passed; its output explicitly says native ABI/original OTP were not executed |
| Windows x64 C adapter/native smoke | Passed: tuple17, list9, empty tuple, bound, set_option rejection and formatting |
| Windows ARM64 C adapter and smoke executable | Cross-compiled; PE machine ARM64 confirmed; **not executed** |
| Native RID selection | ARM64 is selected for copying; x64 remains packaged without being copied into that target output |
| SDK layout / Python syntax / scoped Git whitespace | Recorded with final receipt |

Original OTP differential execution, target Unix compilation/execution, the full
main test suite and `tools/validate.ps1` were not run. The Unix workflow remains
unrun. Source readiness is not semantic completion and canonical readiness is
not regenerated from these partial checks.

The final six-check report is [isolated-managed-checks.json](isolated-managed-checks.json).
The complete source/artifact identities, platform classifications and native
receipt hashes are in [status.json](status.json). The isolated source snapshot
uses `git archive HEAD` for Runtime.Core/Terms/Serialization and pinned embedded
ERTS assets, overlaid with only this task's owned files. It is a validation
snapshot below artifacts, not a worktree, and never patches the shared source.

## Preservation and handoff

Before editing, record SHA256 for 1,151 existing dirty/untracked files. Do not
stage, reset, rewrite or commit them. A later comparison found an independently
appended `sys` dependency-decision entry in the existing shared dirty file;
preserve that concurrent work too. All other original snapshot files compared
identically at that comparison. The foreign `NifNativeDefaultLoader.cs` changes
were read for integration but were not edited.

The canonical latest changelog, progress, compatibility, dependency-decision and
readiness files already contain unrelated changes. Per the user's explicit
instruction to leave those files untouched, keep this English journal, decision
audit and status matrix in new scoped files. Integrate them into the canonical
journal only when its owner can reconcile the concurrent work. No new global
part number is claimed. No commit, push or package publication is performed.

Next executable step: run the manual workflow on actual macOS ARM64/x64 and
Linux ARM64 hosts, inspect failed native bindings if any, then extend integrated
OTP differential/lifecycle coverage before declaring those platforms accepted.
