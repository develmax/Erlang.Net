# ETS backends and remaining acceptance

The pinned baseline is OTP-29.1.1, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. All 76 explicit ETS exports have
C# bodies: 51 shared runtime primitives and 25 library exports in `EtsModule`.
The original `ets.erl` and `ets.beam` remain unchanged. The two generated
`module_info` exports are metadata, so an installed image exposes 78 entries.

**Focused acceptance passes; full OTP compatibility remains uncertified.**
Both backends pass 355 exact observations plus 8 stateful, 12 terminal/regex and
75 real filesystem/DETS/corruption scenarios. Official OTP reads both exported
dumps, and both backends read the official dump. Earlier missing-native failures
are preserved; the final run uses the concurrently supplied real writable
prim_file/prim_buffer closure. Complete upstream, platform, resource, scheduling,
metadata and whole startup-manifest acceptance remain open.

## Selection

The managed startup choice is explicit:

```csharp
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("ets")])
);
```

This installs the shared ETS primitives and the managed library layer. It does
not load every optional OTP dependency. Call the ordinary `ets:Function/Arity`
API through `runtime.Modules.Call`; `EtsModule.Invoke` also forwards the shared
primitive signatures. The default registry is unchanged.

The original startup alternative is
`RuntimeModuleSelection.Beam("ets", pinnedBeam, declaredDependencies)` with the
required explicit dependency selections. Whole startup closure, on_load and
dependency and metadata gates still apply. A manually composed runtime can install
`RuntimeBootstrapEtsLibrary.Register`, prepare the unchanged original image with
`BeamDependencySelection`, and publish it using `PublishPreparedBeam`. Prepare
images outside `InitializeModulesAsync`; publish within initialization. The
permanent validator exercises this explicit original composition and verifies
the exact source owner and all 51 captured primitive signatures.

Startup snapshots identify the requested backend, input BEAM digest, exports and
implementing assembly identities. No automatic fallback or live table/state
migration is introduced. Shared native BIFs are runtime capabilities rather than
a second copy of the library.

## Dependencies and behavior

The C# layer ports enumeration, deletion wrappers, folds/fixation/cleanup,
initialization callbacks, compiled match-spec running/repair, DETS bridges,
shell-fun conversion, dump/load options/header/count/checksum/control flow,
QLC callbacks/lazy traversal and terminal browsing. The existing runtime retains
storage, ownership/heirs, match specs, counters, cursors and concurrency.

Like the original ETS source, the port calls `lists`, `erlang`, `disk_log`,
`file`, `dets`, `qlc`, `erl_eval`, `ms_transform`, I/O, Unicode, string and regex
dependencies. Those dependencies must be explicitly composed for the paths
being used. It does not replace disk_log with a private persistence format.
Terminal regexp search adds the exact native `[{capture,none}]` mode through the
existing admitted PCRE2 engine; index/binary captures and native match-data
cleanup have connected differential probes.

## Focused verification

See the [immutable ETS receipt](validation/ets-port-20261104/README.md).
Implementation preceded the checks. The validator compares whole returned
terms, exception classes/reasons/full stacks and controlled effects against the
official admitted OTP provider. Only the worker's own table reference is
alpha-renamed in captured outcomes; no stack frame is removed. Terminal scripts
compare complete output, except the deliberately separate `i/0` projection
(fixed header and table-name presence) because boot tables, PIDs and memory
values differ between the two environments.

The test bootstrap captures the original feature keyword state and restores it
through real persistent_term calls, and loads actual original supporting
modules. The group leader is a recording test I/O device. Filesystem scenarios
use actual original disk_log/file/prim_file and DETS modules over real native
file/buffer resources. The library-only init actor implements get_argument with
no command-line arguments, enabling the unchanged original DETS startup path.

```powershell
pwsh -File tools/validate-ets-original.ps1 -CaptureOnly -Output artifacts/ets-fresh
dotnet build tools/Validation/Erlang.EtsCompatibility -m:1 --artifacts-path artifacts/ets-build
# Replay each backend with the produced validator:
dotnet <validator.dll> . artifacts/ets-fresh Managed
dotnet <validator.dll> . artifacts/ets-fresh Beam
dotnet <validator.dll> . artifacts/ets-fresh Managed browse
dotnet <validator.dll> . artifacts/ets-fresh Beam browse
# Keep the real filesystem gates explicit:
dotnet <validator.dll> . artifacts/ets-fresh Managed files
dotnet <validator.dll> . artifacts/ets-fresh Beam files
```

`-IncludeTerminal` and `-IncludeFileSystem` add those replays to the PowerShell
entry point. `audit-ets-source.py` inventories the unchanged baseline.
`summarize-ets-audit.py <capture-directory> <validator.dll>` refuses incomplete,
failed, stale-assembly or changed-corpus focused receipts; its admitted summary
still records `FullOtpCompatibility: false`.

No full regression or global readiness refresh is included. Physical resource,
scheduler, metadata, platform-wide and complete contract acceptance remain open.


The current-main publication successor is [Part584](validation/ets-pr-20261104/README.md).
It replaces inline protocol strings, source lines and semantic numbers with
owning catalogs and uses an ordinary build without draft exclusions. The old
Part569 receipt remains historical evidence of its frozen assemblies.
