# erts_internal backends

The opt-in C# alternative implements all 27 Erlang library signatures from
OTP 29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`). The other 71 exports
are shared C# VM primitives. The complete selected surface is 98 exports.
The original Erlang source and BEAM remain unchanged.

The Part586 multi-scheduling gate is closed by [Part592](validation/erts-internal-part-592/README.md).
[Part593](validation/erts-internal-part-593/README.md) adds the actual native node/dist reference
inventory. Both alternatives match262/262 complete observations and59/59 scenarios;
13 physical groups and43 selected connected groups pass. All historical failures
remain in the immutable receipts. Whole-module compatibility remains **partial**:
complete upstream, platform, resource, distribution and metadata acceptance has
not been established. Passing finite observations does not certify universal100%.

## Selecting the implementation

Use `RuntimeModuleSelection.Managed("erts_internal")` in
`ProcessRuntime.CreateAsync(new RuntimeLibrarySelection(...))` for C#.
Use `RuntimeModuleSelection.Beam("erts_internal", originalBytes, dependencies)`
for the original Erlang compiled to BEAM. Manifest selection accepts the same
module name and Managed/Beam values. Selection occurs at startup.

The library retains calls to actual helper modules: `kernel_refc`,
`erts_literal_area_collector`, `error_logger`, `erts_debug`, `ets`, and
`persistent_term`, as required by the selected APIs. Supply and initialize
their real dependencies when exercising those APIs. A full BEAM startup
manifest must declare its complete import closure. The focused validator
publishes the original image explicitly after the initial runtime startup;
it does not claim whole-release manifest/boot acceptance.

The authenticated original image supplies the native-stub inventory and the
same C# primitive delegates for both implementations. C# library functions
do not execute their original BEAM bodies. Runtime statistics admissions
continue to hold their existing leases and drain protocol, then invoke the
selected C# helper when Managed is selected. Public scheduler-wall-time
dispatch also recognizes the explicit Managed choice instead of selecting an
unrelated legacy controller based only on BEAM source ownership.

## Behavior and lifetime

The port includes selective mailbox waiting, monitor flush, map-comprehension
iterators, process-code options, statistics aggregation, distributed I/O error
rethrowing, ETS privilege/fixation cleanup, process initialization, persistent
dynamic-node state, cleaner priority messages and breakpoint continuation.
Breakpoint functions retain the original lambda identity and use a runtime
stored-function recipe with an explicit `[Self, ResumeRef]` environment.
They do not capture the creating ProcessContext in a host closure.

Full reasons and source stacks are compared. Only the fresh reference in the
specific `request_system_task` error frame is projected to a token. Native
atom-index-dependent map printing is checked against the VM's actual flat-map
key tuple; a separate integer-key map retains an exact string comparison.
Earlier unprojected mismatches and the original collector's extra caller
frames remain in the immutable receipt.

## Validation and open work

Run `tools/validate-erts-internal-original.ps1` with an admitted OTP provider.
It captures new evidence, builds the focused validator and compares both
backends, retaining failures. The script refuses to overwrite an output
directory. No full regression or canonical readiness regeneration is run.

For this shared dirty checkout, `tools/Validation/Erlang.ErtsInternalCompatibility/build-scoped.ps1`
builds a fresh snapshot from the recorded base and only the owned additions
and four explicit integration edits. Initial unrelated draft build failures
and subsequent shared compiler-output contention are retained separately.

The [Part586 receipt](validation/erts-internal-part-586/README.md) contains commands, source
and binary identities, full observations, all failures, and the 17 passing
connected groups. Part592 implements real normal/dirty coordination, nested per-process blockers,
owner-death release, truthful pending/committed state and queue restoration.
Complete upstream, distributed, resource, platform, metadata and full boot
acceptance also remain. No universal 100% compatibility, merge or hosted CI
is claimed.

## Multi-scheduling behavior

The four multi_scheduling operations are real native runtime behavior. All-mode
blocks wait for current dirty CPU/IO calls to settle and park new dirty calls;
normal-only blocks leave those workers available. The normal actor barrier
quiesces previous turns and admits unbound guest execution on scheduler one.
Strongly pinned guests on other schedulers wait until unblock. Online counts
and bindings are preserved. Normal death, kill and shutdown release every nested
block owned by the process. Pending intent is not reported as a completed block.

The helper's finally path is tested with disabled and enabled internal-state
errors, including complete source stacks and restored scheduling state. The
positive node_and_dist_references query now returns a census of actual runtime
root owners under a real scheduling barrier. Disabled access, invalid tuple
markers and normal-only blocks retain native errors. See [Part593](validation/erts-internal-part-593/README.md)
for generation separation, signal/heap roots, ETS, timer, persistent storage,
compiled programs, distribution handles and remaining acceptance boundaries.
