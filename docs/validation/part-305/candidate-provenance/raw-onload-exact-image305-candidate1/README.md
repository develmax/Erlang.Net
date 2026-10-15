# Exact-image process-code prerequisite, candidate 305

Artifact-only isolated implementation. No Main edit, public MFA, privileged purger process, pending on_load purge session, raw false change, native execution or original rerun. This is boundary 1/2 from `raw-onload-purge303-rebase-audit1`; the stage/fun fence and saved-old session remain future work.

`baseline.json` records actual source bytes copied from Main, including its uncommitted303 source. Its published HEAD is not a claim that those working tree changes were already committed. `snapshot-resource-repair.json` supplements the initial narrow source snapshot with 119 tracked resources required by existing resource guards. `promotion-map.json` binds every overlay parent and result; future promotion must verify its own explicit HEAD and those parents.

## Concrete implementation

* `ProcessCodeRequest.Image` is a captured immutable `ModuleCodeImage` reference. Module queries keep a null image and resolve ordinary old code when the target processes the request, preserving the existing public behavior.
* `ProcessRuntime.CodeChecks.cs` retains the existing five-argument module-query signature and reply/cancellation protocol. The internal image-query route selects the captured image instead. Self queries use that same distinction; dead-target replies remain false.
* `ProcessRuntime.CodePurge.cs` extracts the original scan, monitor, kill, DOWN and actual `Unwound` waits. Existing `PurgeOldCode` delegates to this engine with its prepared image; it still owns the existing prepare/finish/finally protocol. The per-process helper is internal solely as an exact-target runtime primitive, with no new Erlang exposure.
* Snapshot and per-process state checks use short runtime-monitor sections. Awaiting mailbox replies, DOWN and Unwound retains neither the registry monitor nor the code writer. This helper does not itself establish a no-new-entry fence: its caller must already own the appropriate prepared-image protocol. It cannot substitute for the missing pending on_load stage/fun fence.

## HOST cases

1. Queue image and public module requests while the target owns the original image lease and cannot process control messages. In a deliberately private fixture transition, replace the old slot under `ModuleRegistry.Apply`; release the target. The image request returns true for the original image while the unchanged public module request sees the replacement and returns false. This fixture models private displacement; it does not implement `prepare_on_load`.
2. Scan a real active image lease and a separate receiving process whose dictionary holds a real compiler-created local fun without executing it. Kill the active process, wait through cleanup and actual unwind, leave the idle fun holder alive, and invoke the retained fun successfully. Cleanup acquires a fresh code writer through `ModuleRegistry.Apply`, exposing an accidental retained-writer deadlock.
3. Kill a process while its uncancellable host await still retains the image lease. Its public completion is already settled, but `Unwound` is not. Invoke the per-process async helper inline: its incomplete return proves it reached the actual Unwound wait before releasing the target. Hold cleanup at another owned barrier; verify the drain remains incomplete until cleanup and unwind finish. Then scan the remaining idle holder and verify it survives.

Each scenario has bounded waits, and outer finally blocks release owned host barriers. No heap scan or function-holder killing is invented. Pinned `beam_bif_load.c:1149-1224` scans execution/continuation pointers rather than idle fun values; immutable raw blob bindings live in the prior rebase audit. The SDK continues to model active execution with image leases and does not claim native physical stackdump equivalence.

## Actual validation and retained failure

Initial target build: zero warnings/errors; new HOST passed. SDK whitespace plus existing custom layout formatting on eight paths retained identical Roslyn token kinds/text.

The first broader focus run failed the unchanged original on_load resource-count assertion because the isolated source snapshot excluded non-beam resource files. The original failure log and exact owned process termination receipt are retained. The candidate resource closure was repaired from tracked Main bytes, without changing any assertion. The isolated focus runner now catches failures and returns exit1 instead of leaving an unhandled failing process alive; its original source is retained.

After repair, target build again had zero warnings/errors. All 14 managed focus groups passed: the new exact-image HOST and the previous 13 caller/admission/lifetime/tail/loading/purge/initialization groups. No broad full suite, native backend, original OTP execution, or runtime parity claim is attached to this candidate. One added HOST registration would increase the current full count by one; it adds no supported public MFA.
