# Public purge timing correction

Source-only revision of immutable `nif-owned-lifetime299-overlay`, whose 15 files remain unchanged. Apply this two-path overlay after the original overlay, before formatting/building. The manifest records exact parent and revised source hashes. The author's build, HOST, original and native execution status remains false.

`ModuleRegistry.FinishPurge` already owns and queues retirement while completing metadata. `ProcessRuntime.PurgeOldCode` now returns that metadata/reference completion result without awaiting the unload callback. An asynchronous owner failure is retained on the retirement task and reported by explicit internal ownership joins and runtime shutdown/quiescence; it is not converted into an Erlang purge BIF error.

The revised HOST holds a native-body finally first and verifies purge still waits for actual code-reference unwind. It then releases that finally, holds owner cleanup independently, and requires the public purge result and old/pending metadata completion before releasing cleanup. The internal retirement task remains pending during the held cleanup. The failure case requires public `true`, shared pending internal joins, cleanup failure on the explicit join, and the same retained failure on shutdown, with one cleanup invocation.

The custom high-level pending-discard API continues to join owned cleanup. It is separate from the raw OTP finish_after_on_load primitive, which is unchanged. No claim that an unload-joining public purge is a stronger equivalent contract remains: the externally observable public timing follows the pinned asynchronous unload boundary.

Exact original evidence and remaining native-library exclusion requirement are recorded in `nif-lifetime-otp2911-bindings-audit1`. The process-wide native lifecycle fence is only proposed in `native-lifecycle-fence-proposal.md`; no native backend file is changed by this revision.
