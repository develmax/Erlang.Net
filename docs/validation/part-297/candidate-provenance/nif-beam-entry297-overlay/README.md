# Exact-image BEAM named-entry candidate

Source only. This worker did not modify Main, build, run tests, use a native Erlang runtime, or change Git state. Apply only after checking parent-source.json against the combined Part 296 source. Add `Test(Erlang.Tests.NifBeamEntryHostNames.Test, Erlang.Tests.NifBeamEntryHostChecks.Run);` to the existing HOST registrations.

## Identity and ownership

BeamExecutionIdentity is an immutable image/owner pair. Every standalone BeamModule owns a separate private image and itself as owner. Every PreparedBeamImage owns its image and itself as owner from construction through staging, pending callback, publication, old-code execution and captured closure invocation. BeamOnLoadTicket reuses that image; it never replaces the prepared owner. Exported bodies and closures consequently do not retain a completed callback ticket or ProcessHandle through their execution owner.

Prepared bodies capture their prepared object, not a module-name lookup. Publication uses RegisterSource with that exact owner and installs the prepared image. Pending execution receives the prepared identity explicitly. The admission coordinator accepts exact pending.Prepared alongside the legacy exact ticket owner, and current-image admission additionally recognizes exact module sourceOwners references. Existing body/owner snapshots are still revalidated at commit. Descriptor.SourceOwner stays null for BEAM, preserving source-fun relinking boundaries: Compiler's source tail route checks Owner:ModuleDefinition, and AdmitSourceFunction's source-owner type gate cannot relink a source ModuleDefinition to a BEAM prepared object.

All public APIs keep their signatures. Existing interpreter and closure-factory overloads remain available and create independent standalone identities. Production callers use explicit identity overloads. No interpreter or closure route borrows CurrentCodeImage by module name.

## Entry and return ledger

| Route | Image and entry behavior | Prepared HOST |
| --- | --- | --- |
| Exported MFA | Prepared export delegate -> explicit identity Execute -> lease/flow -> entry hook | Published local run/0 and closure run/0 |
| Standalone MFA | BeamModule-owned identity -> same entry hook | Same-name loaded image cannot override standalone closure |
| Non-tail local call | Save BEAM continuation + function scope, enter exact-image target, native result resumes saved caller | make/1 fake binding reached from genuine run/0 |
| Local tail call | Dispose replaced function scope, enter target without pushing continuation | run/0 -> loop/2 |
| call_fun2 | Existing known-target guard -> captured identity ExecuteLocal | Genuine generated closure entry overridden |
| Escaped closure | Immutable code + captured identity + copied environment; reacquire exact image lease at invocation | Old/current closures return different bindings |
| Pending callback | Exact ticket image with stable prepared owner -> same entry hook | Synthetic pending callback entry captures genuine same-image closure |
| Return, external tail, external last | Restore function scope with existing continuation; never re-enter resumed caller | Existing BEAM regression groups remain required |

Function entry checks an already-created binding table without creating one. A native admission is held across the actual awaited body, including cancellation finally blocks. Native return writes x0 and uses the same continuation restoration as ordinary return. Non-tail callers remain on the function-frame stack, tail entries replace their frame, and all remaining scopes unwind in finally. Existing instruction validation, supported imports, register checks and diagnostic budgets are retained. This does not broaden unsupported reachable BEAM instruction validation or general external-import support.

## Evidence boundaries

All new bindings are managed fake internal bindings. The HOSTs hash-check the existing pinned original local/closure BEAM bytes, but their new override behavior is managed-only evidence, not an original OTP/native NIF comparison. The pending closure test injects OnLoadEntryLabel into the managed linked-code record after preparation; raw fixture bytes remain unchanged. Its fake callback makes a genuine closure from that exact prepared code before publication, then the escaped closure executes after publication with the same image/owner. Both pending/current owner recognition invoke actual coordinator admission and must reach Reload rather than OwnerMismatch/UnknownImage. A genuinely compiled on_load closure fixture compared with original OTP remains future work.

The owned-drain case sends an actual kill signal to a fake native body, holds its finally behind a barrier, and requires native-owner cleanup to remain pending until release. All waits are five seconds and release barriers run in finally.

Remaining prerequisites: native library ABI and loader callbacks, load_nif BIF and public error contract, purge/on_load failure/runtime shutdown native-owner retirement, module_info(native)/system_info inventory, genuinely compiled pending closure fixture, and original OTP native comparisons. This candidate makes none of those completion claims.
