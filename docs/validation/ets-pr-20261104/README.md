Historical initial PR116 receipt. Current source successor: [Part586](../ets-main-reconcile-20261104/README.md).

# ETS selectable backends and semantic constants — Part584, 2026-11-04

All25 ETS library exports are implemented in C# over51 shared primitives.
Original ets.erl and ets.beam remain unchanged. The installed surface is76
explicit exports plus two generated metadata exports. Selection is explicit;
there is no fallback or live state migration. Baseline: OTP-29.1.1,
ad05823719d77c8faee87348ea39513d4e2f99c5.

The correction replaces inline strings/source lines and semantic numeric values
with domain catalogs for functions/modules, atoms/errors/callbacks, dump fields
and format, continuation layout, QLC closure identities, terminal/query text and
layout. The validator also names its protocol, paths, messages and timeouts.
Identical values from different contracts remain separate. MFA arities and
ordinary tuple/argument positions remain explicit structural values.

| Connected checks | C# library | Original BEAM on .NET |
| --- | ---: | ---: |
| Whole return/exception/class/reason/stack observations | 355/355 | 355/355 |
| Stateful tables, QLC and shell callbacks | 8/8 | 8/8 |
| Terminal/regexp and native cleanup scenarios | 12/12 | 12/12 |
| Real files, DETS bridges and corrupt/header/checksum cases | 75/75 | 75/75 |
| Total | 450/450 | 450/450 |

Both backends read the official dump. Official OTP reads and validates both
backend-created dumps. The75 filesystem scenarios execute real original
file/disk_log/DETS helpers over native writable file/buffer resources; they include
multi-chunk transfers, malformed/future headers, MD5/count/footer errors,
existing-target deletion and private-table save cleanup. Persistence is not mocked.

Whole terms/stacks are compared; only the worker's own table identity is
alpha-renamed. The separate i/0 observation projects fixed header and table
presence because boot rows, PIDs and memory differ. Other selected terminal output
is compared completely. Original feature state is restored through persistent_term.

## Reproduction

The release snapshot starts at main48eff39281cd81a2938e68dfd6a93d6f08899948,
preserving the newly published file/code backends. It builds normally, without
source exclusions or replacement registrations. Build: zero warnings/errors.

```powershell
dotnet build tools/Validation/Erlang.EtsCompatibility -m:1 --artifacts-path artifacts/ets-build
pwsh -NoProfile -File tools/validate-ets-original.ps1 -Output artifacts/ets-fresh `
  -RunnerDll artifacts/ets-build/bin/Erlang.EtsCompatibility/debug/Erlang.EtsCompatibility.dll `
  -IncludeTerminal -IncludeFileSystem
py -3 tools/Validation/summarize-ets-audit.py artifacts/ets-fresh `
  artifacts/ets-build/bin/Erlang.EtsCompatibility/debug/Erlang.EtsCompatibility.dll
py -3 tools/Validation/audit-ets-literals.py .
pwsh -NoProfile -File tools/Validation/Erlang.EtsCompatibility/verify-source-bindings.ps1 `
  -RunnerDirectory artifacts/ets-build/bin/Erlang.EtsCompatibility/debug `
  -SourceManifest docs/validation/ets-pr-20261104/source-paths.json `
  -Output artifacts/ets-source-bindings.json
```

Oracle execution requires the existing admitted official OTP provider JSON at
artifacts/library376-official-compiler/official-admission.json (or -Admission).
Terminal validation uses that provider's admitted PCRE2 binary. All probes are
connected to ETS; no full local regression or readiness regeneration ran.

## Evidence and integration

[summary.json](summary.json) admits all six reports and cross-OTP dumps, binds
assembly/corpus/helper identities, and keeps FullOtpCompatibility=false.
[source-bindings.json](source-bindings.json) records84 PDB source documents with
zero differences from the tested sources. [literals.json](literals.json) checks
eight implementation/observer files; [layout-check.log](layout-check.log) records
zero files requiring formatting in the selected source closure.

The first isolated observer retained prepared-image producer leases, causing
teardown to wait for the runtime's empty-tuple resource. The validator now disposes
consumed preparations after publication; shutdown is not bypassed. Two original
badfun/badarity comparisons then reproduced a missing failed-call frame. The
shared call_fun path now preserves both ERTS frames, matching C# and the oracle.
Failed attempts are retained in the raw archive; expectations were not changed.

[source-preflight.json](source-preflight.json) passes13 passive native/focused
plans. [resource529.json](resource529.json) passes4279 dependencies.
[mfa-native-bindings.json](mfa-native-bindings.json) preserves current raw bindings;
unchanged historical evidence is read from the frozen main Git objects when absent
from the isolated export. [Semantic scope](source-plan-refresh/semantic-scope.json)
proves only existing identity/size/successor fields and dependent pins changed,
with membership, statuses, execution flags and workflow logic preserved. The
monitor freeze retains its previous parent identity. These are source admission
checks, not execution of the unrelated regression/native suites.

[admission-controls.json](admission-controls.json) retains rejected partial and
stale-assembly receipts. [raw-evidence.zip](raw-evidence.zip) contains raw ETF
observations, dumps, helper/validator sources, all bound source snapshots, frozen
tested assemblies/PDBs and earlier failure diagnostics; its digest is in
[archive-sha256.txt](archive-sha256.txt). Original Part569 filtered-build evidence
is retained [separately](../ets-port-20261104/README.md).

Full upstream/concurrency/platform/resource/scheduling/metadata and whole
startup-manifest acceptance remain open. Finite comparisons do not prove100% OTP
compatibility. Hosted CI/review/merge/package completion is not claimed by this
local receipt. See [backend selection](../../ets-backends.md).
