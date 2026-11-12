# Part568: file alternative, incomplete native acceptance

Date: 2026-11-04. Baseline: OTP29.1.1,
ad05823719d77c8faee87348ea39513d4e2f99c5. Target achieved: **no**.

The C# library contains bodies/routing for all69 non-metadata exports. Original
Erlang source and image are unchanged. This receipt preserves focused evidence
and explicit failing end-to-end checks; it does not certify100% compatibility.

## Final observations

| Corpus | C# | Original BEAM on .NET |
| --- | --- | --- |
| All-export library corpus |408 compared,0 differences,0 skipped |408 compared,0 differences,0 skipped |
| Raw complete cycle |fails |fails |
| Normal process-backed complete cycle |fails |fails |
| RAM complete cycle |fails |fails |
| Consult complete cycle |fails |fails |
| Compressed complete cycle |fails |fails |

Each complete-cycle row executes in a fresh independent runtime; later rows do
not inherit a dead file server. The first two reach missing prim_file:open_nif/2;
RAM returns an error. Consult/compression remain failed provider/dependency
closure probes, not accepted library effects. Both alternatives share these
runtime deficits. Exact errors/stacks are in the ten `*-runtime-gates-N.json`
reports, with row0=raw,1=normal,2=RAM,3=consult,4=compressed.

The passing corpus compares full terms and exception stacks. It includes all
69 signatures, malformed inputs, source guard behavior, descriptor dispatch and
copy effects, partial positional-write errors, unsigned indirect headers,
monitored process death/batches, recursive deletion with ignored child errors,
callback sendfile counts/position restoration and exact server command payloads.
A validation-only descriptor and recording server cover library protocols.
Original OTP gen/gen_server/file_server/prim_file execute on .NET for actual
whole-file reads, directory listings, working directory and file information.
These controlled devices are never installed as production replacements.

Before each observation both hosts reset the fixture's access timestamp to the
same value. Real returned file-info timestamps are compared unchanged. A previous
drift is retained in history; no outcome field or exception frame is projected.

## Evidence and identities

- [Summary](summary.json), [C# report](Managed-report.json), [BEAM report](Beam-report.json).
- [Oracle provenance](provenance.json) binds authenticated provider inputs,
  unchanged file source/image, observer sources and raw corpus/image hashes.
  The capture command checks provider closure before and after actual execution.
- `original.etf`, `runtime-gates.etf`, `exports.etf`, `file_port_cases.beam` and
  `file_port_device.beam` are original captured bytes, not inferred expectations.
- [Source bindings](source-bindings.json) and [runner package bindings](runner-bindings.json)
  identify the local source checkpoint and frozen executable dependencies.
  Shared files also contain unrelated concurrent work; no scoped Git publication
  or complete source/assembly equivalence for those other modules is asserted.
- [Admission controls](file-admission-controls.json): altered corpus bytes and
  altered source bindings are both rejected before comparison; neither produces
  a passing report. Their failure logs are retained alongside this receipt.
- [History inventory](history-files.txt) and `history.zip` retain available earlier
  failed builds/replays, acquisition-caller mistakes, source-line mismatches,
  dependency/bootstrap failures and timestamp drift. `raw-history.zip` is the
  earlier flat archive; use the structured history archive for original paths.

The identical final kernel DLL is
6C98D486AAABE73BBA966A1A498F11AE7731D70280D4111C1BCAFB82715BAB9A;
the exact runtime and runner identities are in both reports. The reference
executable is used only for development capture, not .NET production execution.

## Commands and build boundaries

```powershell
pwsh -File tools/validate-file-original.ps1 -Output artifacts/file-acceptance -CaptureOnly
dotnet build src/Otp/Managed/Erlang.Otp.Kernel -m:1 -p:BuildProjectReferences=false
dotnet build tools/Validation/Erlang.FileCompatibility -m:1 -p:BuildProjectReferences=false
dotnet artifacts/file-acceptance/runner/Erlang.FileCompatibility.dll . artifacts/file-acceptance Managed
dotnet artifacts/file-acceptance/runner/Erlang.FileCompatibility.dll . artifacts/file-acceptance Beam
# Repeat for each backend and each index 0 through 4:
dotnet artifacts/file-acceptance/runner/Erlang.FileCompatibility.dll . artifacts/file-acceptance Managed runtime-gates.etf 0
```

The two final task-owned builds report0 warnings/errors using the existing
dependency assemblies recorded in runner-bindings. One earlier complete
connected dependency build passed in file-connected-build.log. A subsequent
whole dependency build fails on unrelated concurrent DETS Compile work, retained
in file-final-build.log; it is not a final whole-tree success. The SDK style
check on isolated copies of task-owned C# files reports0 formatting changes.

The frozen artifact directory retains actual fixture timestamps and runner
files. A new location or recreated fixture requires a fresh oracle acquisition;
copying the ETF alone does not reproduce filesystem metadata. The capture script
uses a new output directory and can run both passing and failing gates; it exits
with failure while any runtime gate differs. Replays verify source/image/ETF
hashes and every selected export owner. The BEAM test uses explicit prepared
image publication; full startup manifest dependency closure is still open.

## Remaining work

Implement missing prim_file NIF and prim_buffer resource primitives and RAM driver
support with actual OTP ownership, close/GC/error/cancellation semantics. Then
replay these five independent gates for both variants and add complete mutation,
links, delayed-write/read-ahead, encoding, parser/evaluator, compression/zstd,
socket sendfile, source-stack, metadata and cross-platform acceptance. No finite
passing subset proves every contract or physical resource boundary.

Full regression/global readiness/hosted CI were deliberately not run. Existing
canonical generated reports remain historical. Only the kernel component scope
and current status documentation are updated; no API compatibility promotion,
commit, push, merge, package publication or performance advantage is claimed.

See [backend contract](../../file-backends.md) and
[English changelog](../../changelog/0104.md).
