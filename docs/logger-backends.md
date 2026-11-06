# Logger backends

Baseline: Erlang/OTP **29.1.1**, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`.

`LoggerModule` implements all **77 explicit `logger` exports** in C#.
The pinned original `logger.erl` and `logger.beam` remain unchanged in
`src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/`.

## Selection

Use the existing startup library selection API or its library manifest equivalent:

```csharp
RuntimeModuleSelection.Managed("logger")
RuntimeModuleSelection.Beam("logger", originalLoggerBeam, declaredDependencies)
```

The first choice registers `LoggerModule` from `Erlang.Otp.Kernel`; the second
executes the retained upstream BEAM in our .NET runtime. Select the common
dependencies explicitly when constructing a complete startup manifest. Existing
module ownership, native admission and dependency validation still apply.
The runtime selection snapshot records the backend, exports, image identity and
assembly identity. There is no automatic fallback, live switch or state migration.

As with the original API, the complete Logger service must be started before
configuration and logging: `logger_server` owns configuration, and `logger_sup`
starts the handler watcher and proxy. A bare API registration does not replace
Kernel startup. The legacy host subset used by `proc_lib` remains available;
its registration does not overwrite an already installed complete `logger`.

## Implementation boundary

The C# module owns severity/log/macro overload dispatch, lazy evaluation and
failure handling, metadata precedence/defaults, message construction and remote
forwarding decisions, configuration API control flow, module/application levels,
report conversion, `i` output, environment initialization and reconfiguration.
External filter functions use the runtime's canonical function entries, so they
can cross process, persistent-term and configuration boundaries.

The support modules `logger_server`, `logger_config`, `logger_backend`,
`logger_proxy`, `logger_olp`, handlers, watcher, supervisor, formatter and filters
remain shared original Erlang executed on .NET. So do application/controller,
I/O, collection and formatting dependencies. These dependencies are **not**
claimed as C# ports. The managed `logger` does not invoke the original `logger`.
No external ERTS process is needed in production; official OTP is the development
oracle. No logging package was added. Preserve upstream Apache-2.0 notices.

## Focused validation

Run the permanent oracle/dual-backend validator:

```powershell
pwsh -File tools/validate-logger-original.ps1 -Output artifacts/logger-fresh
```

Use a fresh output directory. `-CaptureOnly` captures official OTP observations;
`-RunnerDll` runs an already built validator. `-BacktraceDepth` captures and replays
a particular error-stack profile. The corpus manifest binds the fixture sources,
compiled fixtures, observations and oracle input inventory; changed inputs are
refused before execution. Reports include source/assembly hashes and raw ETF
outcomes. Both backend owners and the exact upstream export set are checked.

The corpus exercises all log/macro overloads, full handler events, lazy results,
metadata precedence, primary/handler/proxy configuration, filters, module and
application levels, initialization, reconfiguration, pretty-print bytes and
errors with complete stacks. Five unchanged `logger_SUITE` functions are included:
`start_stop`, `compare_levels`, `format_report`, `process_metadata` and
`config_sanity_check`. The copied suite has the pinned Git blob identity
`d0b33cec226806ae2ecde4d4804b1062f207fc1b`.

Event observers validate PID/group-leader/timestamp provenance before replacing
those process/time-dependent values with explicit markers. Explicit metadata
overrides remain exact. Because `maps:to_list/1` has unspecified order, the map
report observer compares exact format fragments paired with their original
key/value arguments as associations. List reports, error stacks and pretty-print
bytes are not reordered. The unchanged upstream report testcase is also replayed
without that projection.

See the [implementation receipt](validation/logger-port-20261105/README.md) for
actual results, failed attempts and input identities. API completeness and finite
Windows observations do not certify universal 100% OTP compatibility. The complete
upstream suite, distributed nodes/remote group leaders, overload/failure/resource
interleavings, cross-platform behavior and full release-manifest startup remain
separate acceptance work. The user's focused-check instruction excludes the full
repository regression and canonical-readiness regeneration from a partial report.
