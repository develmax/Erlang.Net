Historical Part569 receipt. Current-main publication successor: [Part584](../ets-pr-20261104/README.md).

# ETS C# implementation and focused acceptance — 2026-11-04

All 25 ETS library exports are implemented in C#, composing 51 shared C# runtime
primitives: 76 explicit public exports, plus two generated module_info exports.
Original ets.erl and ets.beam remain unchanged. Backend selection is explicit;
there is no automatic fallback or live table migration.

Pinned oracle: OTP-29.1.1, commit
ad05823719d77c8faee87348ea39513d4e2f99c5. Implementation preceded the checks.

| Connected scope | Managed | Original BEAM on .NET |
| --- | ---: | ---: |
| Whole returned terms and exception classes/reasons/stacks | 355/355 | 355/355 |
| Stateful tables, QLC and shell callbacks | 8/8 | 8/8 |
| Terminal interaction and regexp/native cleanup | 12/12 | 12/12 |
| Real files, DETS transfers and corrupt/header/checksum cases | 75/75 | 75/75 |
| Total observations/scenarios | 450/450 | 450/450 |

Both backends read the official OTP dump. Official OTP successfully loads both
backend-created dumps and checks their data. The filesystem group includes
48 option/size/verification roundtrips, multi-chunk real ETS↔DETS transfers,
malformed/future headers, checksum/count/footer errors, existing-target deletion
and private-table save cleanup. These run unchanged original disk_log, file and
DETS helpers over real native file/buffer resources; persistence is not mocked.
The original init get_argument protocol is supported by the library-only
bootstrap with no command-line arguments.

The early filesystem failures at missing prim_file:del_file_nif are retained as
historical failures. The final run integrates the real writable file/buffer
implementation supplied concurrently in this checkout. This task does not claim
authorship or independent whole-module certification of that shared prerequisite.

The observer compares full terms and stacks. Only the worker's own table identity
is alpha-renamed. Terminal i/0 deliberately compares its fixed header and table
presence because boot rows, PIDs and memory differ; other selected terminal
output is compared completely. Feature keywords are captured from the oracle
and restored through actual persistent_term calls in the test bootstrap.

## Reproduce the accepted scope

The ordinary shared checkout contains unrelated unfinished DETS C# sources.
The accepted build uses the retained build-only filter and registration snapshot,
excluding those drafts without editing their sources. Real DETS validation uses
the unchanged original Erlang implementation. This is not an ordinary clean
whole-checkout build or a publication-head receipt.

```powershell
dotnet build tools/Validation/Erlang.EtsCompatibility -m:1 `
  --artifacts-path artifacts/ets-build-569-guards `
  -p:CustomAfterMicrosoftCommonTargets=D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/ets-port-v9/isolated.targets
pwsh -NoProfile -File tools/validate-ets-original.ps1 `
  -Output artifacts/ets-final-569-guards `
  -RunnerDll artifacts/ets-build-569-guards/bin/Erlang.EtsCompatibility/debug/Erlang.EtsCompatibility.dll `
  -IncludeTerminal -IncludeFileSystem
py -3 tools/Validation/summarize-ets-audit.py artifacts/ets-final-569-guards `
  artifacts/ets-build-569-guards/bin/Erlang.EtsCompatibility/debug/Erlang.EtsCompatibility.dll
```

Use fresh output names when reproducing. The build completed with zero warnings
and errors. Scoped SDK layout verification is retained; no full regression or
global readiness refresh ran.

## Evidence and limits

[summary.json](summary.json) admits all six backend reports and cross-OTP dumps,
binds tested assembly identities, and explicitly records FullOtpCompatibility=false.
[provenance.json](provenance.json) records the admitted official provider and
dependency inputs. [source-audit.json](source-audit.json) inventories the pinned
source surface. [admission-controls.json](admission-controls.json) proves the
admitter rejects partial and stale-assembly receipts.

[source-bindings.json](source-bindings.json) binds 48 selected source documents
to the tested portable PDBs. All ETS, regex, file/buffer and validator sources in
this selection match. Two shared selection files changed concurrently after the
build (LibrarySelectionNames.cs and ProcessRuntime.LibrarySelection.cs); their
compiled and current checksums are both recorded. Therefore this receipt binds
the frozen tested assemblies, not all current checkout bytes. The temporary
managed registration snapshot is preserved separately.

[raw-evidence.zip](raw-evidence.zip) preserves raw ETF observations, helpers,
dumps, reports, prior failed filesystem receipts, observer/validator sources,
current selected source snapshots and frozen tested runner assemblies/PDBs.
Snapshots under current-source are explicitly current bytes; the two divergent
selection files are not represented as compiled source. The archive hash is in
[archive-sha256.txt](archive-sha256.txt). The source-binding script checks a
selected closure, not every source file in all loaded assemblies.

Full upstream ETS tests, broader concurrency/scheduling, platform/resource limits,
metadata and whole startup-manifest acceptance remain open. Finite differential
checks do not establish universal 100% OTP compatibility. No commit, push, CI,
merge or package release is claimed while the shared prerequisites and current
integration snapshot remain unisolated.

See the [backend contract](../../ets-backends.md) for API selection and dependencies.
