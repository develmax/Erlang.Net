# Selectable io implementations

Baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

The complete explicit API has 53 exports: 52 library functions implemented in C#
and the shared native `printable_range/0`. The original `io.erl` and `io.beam`
remain unchanged. The managed implementation never loads or calls original io
library bodies; calls to `io_lib`, `erl_scan`, `erl_parse`, `unicode` and
`net_kernel` are ordinary upstream dependencies.

## Selection

Use `RuntimeModuleSelection.Managed("io", IoLibrary.RequiredDependencies)` with
explicit selections for those dependencies and their own required closure.
Alternatively select `RuntimeModuleSelection.Beam("io", originalBytes, imports)`.
Both use the same `io:Function/Arity` API. `ErlangLibrary Include="io"` supports
`Backend="Managed"` or `Backend="Beam"` through the existing MSBuild manifest
mechanism; Beam requires the original image and its SHA256. Backend selection is
at startup, with no automatic fallback or live state migration.

For a host that explicitly publishes its library closure during initialization,
install `RuntimeBootstrapIoLibrary.Register(runtime, registry)` and then
`IoLibrary.Register(registry)` in `InitializeModulesAsync`. This removes only the
authenticated legacy bootstrap `io:format/2` body. Caller-installed replacements
are refused. The normal runtime's default limited I/O profile is unchanged until
the full module is explicitly selected.

## Implemented behavior

The C# implementation owns output and error metadata, default input/output and
password routing, character/line/password/options/geometry requests, fread,
read and scanner/parser composition, atomic request batches, nested flattening,
registered-name resolution, monitor-based device death, selective receive,
demonitor/flush and the legacy Unicode I/O request/reply conversion branches.
Generated source frames retain the pinned private/public call sites and respect
the configured backtrace depth. I/O servers and parsers retain their independent
contracts; the library does not replace an actual terminal or file server.

## Focused verification

`py -3 tools/Validation/audit-io-source.py` checks the exact retained source and
all 53 signatures. `pwsh -File tools/validate-io-original.ps1 -Output NEW_DIRECTORY`
captures an admitted official-OTP corpus, compiles the unchanged upstream suite
and compares both backends. The validator checks source/corpus hashes and every
export's actual backend owner. `-BacktraceDepth N` acquires a separate depth
profile; the runner's optional `scenarios` argument selects only stateful and
upstream cases.

Observations compare complete values, exception classes/reasons/stacks and
device messages. Fixture-owned device/self PIDs and fresh error references are
alpha-renamed. Captured startup feature state is restored through actual
`persistent_term` primitives so the unchanged scanner/formatter dependencies
observe the same declared library profile. No return value, source frame or
error reason is discarded. Selected upstream test bodies remain unchanged.

See [the implementation receipt](validation/io-port-20261105/README.md) for actual
commands, results, prior failures and identities. Finite focused acceptance does
not certify all scheduling/resource/platform/distributed-old-peer interleavings,
the entire upstream terminal/filesystem suite or a complete release-manifest
startup. Full regression and canonical readiness regeneration are excluded by
the user's explicit scope.
