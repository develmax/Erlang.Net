# File library alternatives

The C# alternative implements the 69 non-metadata exports of pinned OTP 29.1.1
`file.erl`. Original `file.erl` and `file.beam` remain unchanged. This is an
incomplete compatibility checkpoint: the full filesystem stack is not yet usable
for all operations in either alternative on this runtime.

## Selection and ownership

Select before startup using the existing library selection API:

```csharp
RuntimeModuleSelection managed = RuntimeModuleSelection.Managed("file", dependencies);
RuntimeModuleSelection original = RuntimeModuleSelection.Beam("file", pinnedFileImage, dependencies);
```

Include the chosen dependency modules in the same `RuntimeLibrarySelection`.
The direct upstream imports are `epp`, `erl_eval`, `erl_posix_msg`, `erlang`,
`filename`, `gen_server`, `gen_tcp`, `io`, `io_lib`, `lists`, `prim_file`,
`proplists`, `ram_file`, `raw_file_io` and `socket`. Their own dependencies and
native providers must also be configured. The managed module does not silently
provide or substitute those dependencies. The standalone validator uses explicit
original-image publication for the BEAM path and startup selection for C#;
complete BEAM startup-manifest closure has not been accepted here.

The C# library is in `Erlang.Otp.Kernel`; the runtime composition bundle now ships
that assembly. Startup installs the shared native filename-encoding primitive
and the selected C# wrappers only for an explicit managed choice. The original
alternative executes the original image on .NET. There is no ERTS production
dependency, fallback, live hot switch or state migration.

Both choices use the same MFA interface. `ProcessRuntime.LibrarySelection`
records managed startup identity and `ModuleRegistry.SourceOwner` distinguishes
managed bodies from a prepared original image. The validator checks every export
owner and records actual assembly and corpus hashes. Native filename encoding
remains a runtime primitive, not a manually ported library function.

## Implemented library behavior

The port includes deep filename/mode conversion, option guards and raw routing,
file-server tracing tags, monitored file-request messages, Latin1 I/O requests,
module-dispatched descriptors, batched positional I/O, all copy variants,
big-endian indirect reads, recursive deletion, file-info updates, path searches,
consult/eval/script streams, error formatting and socket/port/callback sendfile
dispatch with chunking and position restoration. Upstream notices are retained.

The implementation reuses existing runtime primitives and selected OTP modules;
it does not replace the file library with direct `System.IO` calls. The validation
host uses filesystem timestamp setters to establish identical fixture input
state before each oracle/runtime observation; compared output terms and stacks
are not normalized.

## Focused evidence and unfinished acceptance

The [Part568 receipt](validation/part-568/README.md) records 408 exact observations
per backend, with zero differences. These include full exception stacks,
descriptor algorithms/effects, recursive deletion dispatch, monitored processes,
sendfile callback results and real Windows whole-file reads, directory listings,
file information and working directory access through original OTP servers.
An explicit validation-only descriptor and recording server exercise library
protocols; they are not production filesystem implementations.

Five additional full-cycle scenarios run in independent runtimes per backend:
raw, normal, RAM, consult and compressed. All five currently fail in both
alternatives. In particular `prim_file:open_nif/2` is not bound by the existing
four-operation native provider; RAM driver support also fails. Consult and
compressed must be re-evaluated after primitive/provider and dependency closure.
Full file-info mutation, links, delayed I/O, compression/zstd, parser/evaluator
effects, native socket sendfile and all resource/ownership/error branches still
need end-to-end acceptance. Other operating systems, physical resource limits,
metadata and universal source-stack equivalence are not certified.

This is not 100% compatibility. No API ledger classification or canonical global
readiness report is promoted. Full regression was deliberately not run.

## Reproduction

Build only the connected validator:

```powershell
dotnet build tools/Validation/Erlang.FileCompatibility -m:1
pwsh -File tools/validate-file-original.ps1 -Output artifacts/file-fresh `
  -RunnerDll tools/Validation/Erlang.FileCompatibility/bin/Debug/net10.0/Erlang.FileCompatibility.dll
```

The command intentionally fails while the runtime gates differ. `-CaptureOnly`
performs authenticated oracle acquisition without executing .NET comparisons.
Use a fresh output directory. Each gate can also be replayed separately with
`runtime-gates.etf` and its zero-based row index. The runner verifies the baseline,
source/BEAM/ETF provenance and every file export owner before comparing outcomes.
Do not replay against recreated fixture metadata; regenerate the oracle for a
new location. Earlier failed attempts remain under `artifacts/file-*`.

The next work is implementing the missing `prim_file` NIF and `prim_buffer`
resource operations and the RAM driver with OTP ownership/error semantics,
then replaying these focused full-cycle gates and expanding the authentic
upstream file contract corpus. No full regression is required by this task.
