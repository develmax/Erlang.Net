# on_load callback cleanup fault correction

Source-only overlay. No Main/candidate edit, build, test, native execution, Git mutation or network operation was performed by this worker. `parent-source.json` binds the production parent file read from Main after root's NIF2 permission promotion; verify its exact hash before applying.

The review found a concrete interaction with Part293's faultable process cleanup: ObservePendingExit awaited callback.Completion before waiting actor Unwound, without a fault path. A throwing process cancellation callback therefore faulted the discarded observation task and left CallbackQuiescence unresolved. The high-level loader awaited callback.Completion outside its pending-image cleanup try, leaving that failed image staged. This predated the code-permission wrappers; it was not inferred from the successful 36-group run.

The production overlay waits actual actor unwind even after callback completion faults, then exposes the failure through the public ticket quiescence promise. The high-level loader includes completion/quiescence joins in its cleanup try, retires the pending image before rethrowing the original failure, and aggregates distinct additional cleanup failures instead of swallowing them. The manual keep=true path propagates faulted already-completed completion/quiescence tasks before publication. The existing keep=false path can retire the failed pending image after quiescence settles. No wait occurs under the registry gate: the synchronous GetResult calls follow the existing IsCompleted checks.

New HOST registration:

```csharp
Test(BeamOnLoadCallbackFaultHostNames.Test, BeamOnLoadCallbackFaultHostChecks.Run);
```

The test reuses the unchanged, hash-checked original fresh-success BEAM resource. While its actual on_load callback waits for the fixture continuation message, the host attaches a throwing cancellation callback to that process. Both manual and high-level flows must preserve the BEAM return `ok`, expose the injected host cleanup error through completion/quiescence, avoid publication, retire pending metadata, and permit restaging the same module. Runtime disposal must also retain and expose the injected cleanup error. Failure filters accept only that exact injected marker, including all aggregate members. The manual keep=true failure and later stage/retire operations have bounded waits, detecting leaked code permission.

For a red run apply only the two new test files plus the additive registration; the old production implementation must fail at bounded quiescence waiting. Its finally does not wait the broken promise and completes the observer and runtime cleanup. For green, apply production OnLoad overlay after validating the parent hash. Preserve the red failure/raw evidence, rerun the new HOST and existing on_load/lifecycle/barrier groups, and perform normal formatting/token and full validation. The manifest separates production and test paths for that sequence.

Independent source review also confirmed ProcessRuntime's three-way merge differs from published lifecycle code only at the three purge modification admission/finish sites; termination, system output transfer, actor unwind and Part293 cleanup remain intact. Ticket.Run removes thread-owned authority before deferred semaphore release; self-dispose cannot release mid-operation, competing Dispose is serialized, and child tasks cannot inherit authority. The permission API executes synchronous Func operations; returning a Task does not make a Run scope asynchronous. Pending native initialization/retirement and actual load_nif remain dormant prerequisites, not native lifecycle parity claims.
