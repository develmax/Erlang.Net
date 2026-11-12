# gen_event backends

The pinned OTP 29.1.1 `gen_event` library now has a handwritten C# implementation
and its unchanged original Erlang/BEAM alternative. Both expose the same 44
non-metadata signatures. Module metadata is still supplied by each backend's
own code image; it is not represented as interchangeable assembly/BEAM identity.

The implementation includes startup/link/monitor variants, ordered duplicate
handlers and IDs, supervised-handler links and notifications, callback catches,
external and callback-requested swaps, sync/async calls and request collections,
hibernation, system/debug/state/code-change callbacks, and logger formatting.
As in OTP, `gen`, `proc_lib`, `sys` and formatting/Unicode facilities are separate
dependencies. Porting `gen_event` does not replace those libraries with CLR
actor defaults.

## Selection

For the managed startup registration, select
`RuntimeModuleSelection.Managed("gen_event")` in `RuntimeLibrarySelection`.
An existing host can instead register `GenEventModule.Register` during module
initialization. The library is absent from the default registry unless selected.
The existing OTP host composition and required original dependencies must also
be initialized by the host.

For original Erlang execution, prepare the retained `gen_event.beam` with an
explicit `BeamDependencySelection`, then publish that image during host setup.
The ordinary `RuntimeModuleSelection.Beam` factory also requires a fully declared
dependency selection. The focused runner uses direct image publication before
any event managers are created; its original backend never registers the C#
`gen_event` implementation. Each exported MFA is checked for its actual source
owner and the original image hash. There is no fallback or state migration.

Consumers call the same MFA in either case:

```csharp
Term started = await runtime.Modules.Call(context, "gen_event", "start");
Pid manager = (Pid)((TupleTerm)started).Items[1];
await runtime.Modules.Call(context, "gen_event", "add_handler",
    manager, Term.A("my_handler"), Nil.Value);
await runtime.Modules.Call(context, "gen_event", "sync_notify",
    manager, Term.A("an_event"));
await runtime.Modules.Call(context, "gen_event", "stop", manager);
```

Callback modules can be original Erlang/BEAM or registered C# implementations.
The manager's handler state remains an Erlang term and callbacks run in the
manager process, preserving mailbox and link ownership.

## Focused acceptance

[Part580](validation/part-580/README.md) compares **105 complete observations per
backend** against the admitted original OTP provider. These include ten unchanged
`gen_event_SUITE` functions, callback failures, optional callbacks, request
collections, complete exception stacks, formatting and system state. The status
test checks the parent PID relationship instead of comparing unrelated process
numbers. The rest of each returned observation is compared as a complete term;
exception frames are not removed by the comparator.

The runner boots actual original `init` and `code_server` on .NET for the original
formatting dependencies. Erlang is only the development oracle; production BEAM
execution uses this .NET runtime. Dependencies and loaded assemblies are hashed.

```powershell
pwsh -File tools/validate-gen-event-original.ps1 -Output artifacts/gen-event-new
```

Use `-CaptureOnly` for acquisition and `-RunnerDll` for a separately built runner.
`summarize-gen-event-audit.ps1` rejects partial, failed, changed-source and
changed-assembly reports. Depth profiles0,1,2,8,32 pass105 observations per backend. Depth3 passes105/105 for Beam and104/105 for Managed: an unchanged upstream zero-timeout test receives an already queued reply. The raw failure is retained and scheduling equivalence remains unproved. Use `-BacktraceDepth` to capture a profile and the runner argument `depth=N` to replay it. The full repository regression and canonical readiness
refresh were deliberately not run. Finite same-platform observations do not
certify universal distribution, resource, scheduling or metadata equivalence.

Semantic constants live beside their GenEvent owner: API/protocol/callback names,
log/status fields, source positions, tuple layouts and resource limits are distinct.
Run `py -3 tools/Validation/audit-gen-event-literals.py .` to guard the logic files.
Validation owns separate names, expected contract counts and operation deadlines.
