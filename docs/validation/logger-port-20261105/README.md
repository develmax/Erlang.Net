# Part588: logger implementation and focused acceptance

Date: 2026-11-05. Baseline: **OTP-29.1.1**,
`ad05823719d77c8faee87348ea39513d4e2f99c5`.

All **77 explicit `logger` exports** have C# bodies and explicit Managed startup
selection. The unchanged Erlang/BEAM alternative remains available. The managed
module does not invoke original `logger`; it shares original Logger servers,
handlers, formatter/filter/proxy infrastructure and normal OTP dependencies
executed on .NET. Those support modules are not claimed as C# ports.
See the [backend/dependency contract](../../logger-backends.md).

Implementation preceded checks. Only Logger and its connected startup, ETS,
application, I/O, process and formatting dependencies were built/executed.
No whole regression or `validate.ps1` run was performed. Canonical readiness and
compatibility classifications were not regenerated from partial reports.

## Final results

| Check | Result | Immutable evidence |
| --- | --- | --- |
| Ordinary focused build, no exclusion filter | 0 warnings, 0 errors | [build log](logs/final-build.log) |
| Exact upstream export set and actual selected owners | 77/77, each backend | [Managed](final/Managed.json), [Beam](final/Beam.json) |
| Primary complete outcomes | **155/155 each** | [Managed](final/Managed.json), [Beam](final/Beam.json) |
| Backtrace depth 0 | **155/155 each** | [Managed](depth-0/Managed.json), [Beam](depth-0/Beam.json) |
| Backtrace depth 3 | **155/155 each** | [Managed](depth-3/Managed.json), [Beam](depth-3/Beam.json) |
| Unchanged upstream testcase functions | **5/5 each**, included in each corpus | `start_stop`, `compare_levels`, `format_report`, `process_metadata`, `config_sanity_check` |
| Modified corpus rejection before startup | Expected refusal observed | [negative control](logs/logger-input-rejection.log) |
| SDK C# layout, scoped source copy | 0 files need formatting | [layout log](logs/logger-style-check.log) |
| Original source and suite identities | Equal pinned Git blobs | [identities](source-identities.json) |

Reports contain current Logger source hashes, actual input BEAM hashes, compiled
assembly hashes, corpus-manifest hashes and raw outcome hashes. The complete
actual terms are also saved as [Managed ETF](final/Managed.etf) and
[Beam ETF](final/Beam.etf), with equivalent depth-profile files. The final primary
ETF files are byte-identical between the two .NET backends. OTP comparisons use
complete term values, errors, tested stacks and effects; this byte identity is not
a claim that encoding process IDs are identical to the original VM.

The [corpus manifest](final/corpus-manifest.json) binds source fixtures, compiled
fixtures, observations and the official oracle inventory. Changed inputs are
rejected. The [official admission](final/official-admission.json) and
[oracle inventory](final/oracle-inputs.json) identify the existing official
29.1.1 provider; its closure was verified before and after capture. Source and
suite identity are independently checked against the pinned reference checkout.
Original reference files were not modified. Current C# implementation snapshots
are under [source](source/).

The final intentionally invalid invocation emitted the expected hash-refusal
exception before runtime creation, then needed interruption while its failing
process was exiting. The refusal log is preserved; no clean-exit result is
inferred from that negative invocation.

## Observations and projections

The corpus includes the 100 ordered severity comparisons, report conversion,
all log/macro overloads, lazy results/ignore/crashes, primary/process/call/location
metadata precedence, default metadata, primary/handler/proxy configuration,
filters, formatter updates, module/application levels, environment handlers,
successful/invalid initialization and reconfiguration, exact pretty-print bytes,
configuration-error handler events and malformed API calls with complete stacks.

The lazy boundary witness returns a message with an invalid group leader. OTP
`try Fun(Args) of ... catch ... end` catches the evaluation of `Fun(Args)` only;
errors in the `of` body propagate. C# now preserves that boundary and the complete
`erlang:node/1` / `logger:log_allowed/4` error stack. The initial failing witness is
retained in history, not normalized into a successful diagnostic event.

PID/group-leader/timestamp markers are produced only after the observer validates
the original process/time relationship. Explicit PID/time metadata stays exact.
For report maps, `maps:to_list/1` has unspecified enumeration order: the observer
compares exact format fragments with their corresponding key/value arguments as
associations. It does not reorder list reports, error stacks or pretty-print
bytes. The unchanged upstream `format_report` testcase passes without that map
projection. No arbitrary stack frame, reference or error is discarded.

## Reproduction

From the repository root, with a fresh output directory:

```powershell
dotnet build tools/Validation/Erlang.LoggerCompatibility/Erlang.LoggerCompatibility.csproj -m:1 --artifacts-path artifacts/logger-final-clean
pwsh -File tools/validate-logger-original.ps1 -Output artifacts/logger-fresh -RunnerDll artifacts/logger-final-clean/bin/Erlang.LoggerCompatibility/debug/Erlang.LoggerCompatibility.dll
pwsh -File tools/validate-logger-original.ps1 -Output artifacts/logger-depth0-fresh -BacktraceDepth 0 -RunnerDll artifacts/logger-final-clean/bin/Erlang.LoggerCompatibility/debug/Erlang.LoggerCompatibility.dll
pwsh -File tools/validate-logger-original.ps1 -Output artifacts/logger-depth3-fresh -BacktraceDepth 3 -RunnerDll artifacts/logger-final-clean/bin/Erlang.LoggerCompatibility/debug/Erlang.LoggerCompatibility.dll
```

Actual final capture directories were `artifacts/logger-20261105-08`,
`artifacts/logger-depth-0-latest-20261105` and
`artifacts/logger-depth-3-latest-20261105`. The final validator command for each was:

```powershell
dotnet artifacts/logger-final-clean/bin/Erlang.LoggerCompatibility/debug/Erlang.LoggerCompatibility.dll . CORPUS_DIRECTORY Managed
dotnet artifacts/logger-final-clean/bin/Erlang.LoggerCompatibility/debug/Erlang.LoggerCompatibility.dll . CORPUS_DIRECTORY Beam
```

The SDK layout tool was applied to an isolated copy of only the Logger C# files
and validator, then copied back, to preserve other concurrently edited sources.
The final normal build and the reported acceptance use those formatted files.

## Preserved failures

[History](history/) and [logs](logs/) preserve earlier captures and attempts:

- Concurrent `gen_server`, `supervisor`, `sys`, Kernel and runtime draft compilation
  failures. Early exclusion-profile attempts are historical; final ordinary builds
  use no exclusions and supersede those build gates.
- The initial source-wrapper registration lacked the startup preloaded owner.
  The complete selectable module now uses the standard preloaded admission.
- Corpus decoding/encoding of functions required the actual runtime factories and
  an explicit encoding owner. The final raw ETF evidence is successfully saved.
- Missing Logger supervisor/proxy, ETS wrappers and formatter/I/O helpers in the
  initial harness. Final tests run the actual original services and helpers.
- Incorrect C# filter argument packing, non-canonical external filter functions,
  missing configuration-error location metadata and the overly broad lazy catch.
- Raw map iteration order in the initial map-report observer. The explicit
  association contract and the unmodified upstream testcase are both retained.

Initial 149/154-case passing subsets and all failing attempts remain historical;
only the final source/assembly-bound reports above establish this checkpoint.

## Limits and publication

This is complete module/API implementation and focused Windows acceptance,
**not universal 100% OTP certification**. The entire upstream suite, distribution
and remote group leaders, all overload/resource/failure/scheduling interleavings,
other platforms, complete invalid-environment/error boundaries, module metadata
and whole release-manifest startup need separate acceptance. The public API count
and finite tests do not prove those properties. No performance claim is made.

No commit, push, PR, merge or hosted-CI result is claimed. The checkout contained
pre-existing interleaved uncommitted ports and shared selector/registration work;
isolating a coherent publication snapshot remains a delivery gate. No unrelated
work was reverted or bundled, and no package was published.
