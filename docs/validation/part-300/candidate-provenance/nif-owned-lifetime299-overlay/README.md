# Registry-owned NIF lifetime candidate

Source only. No Main/Git changes, builds, native execution or test execution by this worker. The artifact directory's draft number is not a published part number. The replacement ModuleRegistry.cs was rebased onto actual Main Part 298 (`e037631759438f944eda934005432ca04fb516e5`); its installed-NIF snapshot changes are preserved. Its only new change is the purge retirement queue. Check every exact parent hash before applying. Add `Test(Erlang.Tests.NifLifetimeHostNames.Test, Erlang.Tests.NifLifetimeHostChecks.Run);`.

## Pinned ownership boundary

`otp/erts/emulator/beam/beam_bif_load.c` around line 2256 unloads the old instance's NIF library only after the process/code-reference purge phase; deleting a module merely makes its code old. `erl_nif.c` around line 5356 drops the module ownership and closes the dynamic library only when its resource-related reference count permits. This bounded candidate has no native resource/thread/dirty-work ownership and does not claim that broader reference-count semantics.

Raw `finish_after_on_load(false)` in beam_bif_load.c around line 922 only removes deferred export references. The purger wrapper in `erts/preloaded/src/erts_code_purger.erl` around line 168 subsequently requests `prepare_on_load`, checks processes, then completes purge; beam_bif_load.c around line 2134 moves the pending instance into the purge path. This candidate leaves our raw FinishLoaderOnLoad(false) unchanged. A full raw prepare_on_load purge path is still required before claiming general native loader lifecycle completion. Our custom high-level pending-discard API already represents a full discard decision and receives owned cleanup here.

## Concrete integration

- A reservation and its exact image are registered with the owning registry before initializer callbacks can await. Admission and commit both refuse a closing registry; admission checks before and after permission acquisition. A reservation unregisters only after binding settlement, its extra image lease and permission are released.
- Ready images remain strongly owned until retirement. Pending retirement operations are owned before scheduling. Cleanup runs on non-flowing worker threads, so no native unload callback executes on the caller's registry/runtime/code-permission monitor stack. No table is created for an image that has none.
- Completed per-image outcomes use a weak-key cache. Repeated purge/discard/shutdown joins share their result while purged images are not retained forever. Failed retirement tasks remain owned and are propagated on shutdown. The underlying table still joins admitted calls and initializer settlement and disposes its transferred owner once.
- DeleteModule does not unload. Successful purge closes the exact old image under its existing metadata protocol, queues owned retirement, then joins it outside code permission. Purge marks metadata completion before awaiting cleanup, so unload failure cannot attempt to abort an already-completed purge.
- Custom pending discard queues cleanup; high-level LoadPreparedOnLoadAsync joins it after callback quiescence and outside permission. Startup and callback cleanup failures retain their original error alongside any distinct retirement error. The Part 295 callback-fault settlement fix remains intact.
- Dispose starts native admission closure before awaiting initializer completion: a reserved ticket may otherwise hold the permission that initializer shutdown is waiting for. It still attempts native shutdown after earlier cleanup failure, cancels remaining executing contexts outside the runtime gate, joins actual drivers/native calls, and preserves failures. Ordinary disposal does not pretend it can forcibly unwind arbitrary managed bodies that ignore cancellation; closing tables prevent new native entry, and admitted native bodies must actually unwind before unload.
- Halt completion keeps its existing early/deadline behavior; halt quiescence additionally joins native shutdown. Initializer termination publishes a runtime-owned automatic shutdown obligation before scheduling it, without acquiring the registry gate from Terminate's runtime gate. Initializer death therefore retires native ownership even without a later explicit Dispose.
- Initializer/cleanup self-shutdown fails before joining its own operation. Diagnostic cleanup scope is not code-write authority. Independent actor entry clears diagnostic retirement scope, while the no-marker actor path returns the original delegate without allocating a wrapper or task.

The shutdown join ignores only the stopped caller's plain cancellation and explicit shutdown-abandon/closing reservation outcomes. Aggregate cleanup failures are not ignored. A load initializer still owns cleanup until it returns an initialized owner; failed initialization cleanup intentionally retains its code permission until actual unwind, as in the existing coordinator contract.

## Prepared HOST matrix

All rows are authored source, not passed checks. New waits are bounded to five seconds; shared actor helpers retain their existing ten-second outer watchdog.

| Scenario | Observable assertion |
| --- | --- |
| Delete then hard purge | Old owner remains; actual kill reaches held native finally; purge/unload wait until actual release |
| Purge unload failure | Old metadata and pending purge settle; one owner cleanup; same failed retirement outcome remains joinable and shutdown reports it |
| Dispose during admitted call | Disposal remains pending and owner is untouched until held finally releases |
| Dispose owner failure | Failure remains observable on repeated disposal; cleanup count remains one |
| Throwing process cancellation callback | Disposal still waits for real native unwind and retires owner, while preserving stop failure |
| Reserved but uninitialized ticket | Registry closure releases its permission and prevents subsequent native admission |
| Initializer ignores cancellation until release | Shutdown waits for initializer and a separately held failed-owner cleanup; no early permission/lease release |
| Genuine failed on_load callback | High-level discard remains pending until owned cleanup finishes; original fixture hashes checked |
| Owner cleanup attempts self-shutdown | Explicit reentry failure replaces a self-join deadlock |
| Immediate halt | Public halt can complete while native finally is held; quiescence waits for release and one cleanup |
| Initializer death | Owner cleanup occurs without explicit Dispose/Close supplying the missing work |

Cleanup boundary checks use actual registry code-modification admission and require no caller runtime/registry monitor on the cleanup thread. These are managed fake owners/callbacks and actual runtime lifecycle operations, not a new original OTP/native parity claim.

## Remaining work

Original OTP/native comparisons, full raw prepare_on_load purge, arbitrary native resources/threads/dirty work/upgrade/on_halt reference ownership, and public load_nif/error/feature-inventory exposure remain separate. This candidate does not modify the native backend or invent general ABI support. Native callback code must obey the reviewed backend's bounded capability profile. External callers synchronously waiting while holding private runtime/registry gates are outside the public API contract.
