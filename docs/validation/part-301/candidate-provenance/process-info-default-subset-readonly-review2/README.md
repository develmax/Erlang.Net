# process_info/1 logical subset independent source review

Ready as a bounded partial API; no concrete defect found in this registration overlay. This review performs no build, SDK/native execution or publication. Root separately reports three focused HOST and eight generated SDK cases passed; their logs were not independently rerun here, and eight original differential executions remain pending.

## Source contract

Pinned OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 erlang.erl default-query docs explicitly leave tuple ordering and selection variable, require a local PID, return undefined for a dead local process and include registered_name only when registered. erl_bif_info.c pi_1_keys has seventeen source-specific fields including suspending. The existing catalog removes exactly total_heap_size, heap_size, stack_size and garbage_collection; twelve logical fields remain unregistered, thirteen registered. No physical value is fabricated. Exact seventeen-field source compatibility remains incomplete. Existing UnavailableMandatoryFields naming is stronger than public documentation and must not become a compatibility claim.

The new BIF uses one existing QueryDefaultLogicalProcessInfo owner snapshot, not independent calls per field. Existing RequireProcessInfoTarget rejects non-PIDs, remote nodes and foreign dynamic CLR owners; fixed local wire identities pass, old local creation resolves undefined. The pinned process_info_bif explicitly returns undefined for an external PID belonging to this_dist_entry (old incarnation), supporting the stale-local source case. QueryProcessInfo uses requester cancellation, target owner mailbox signalling, coherent lock-protected read and termination response. Registration adds a post-await cancellation check. The current /2 registry callback expression was independently compared after whitespace removal and is unchanged.

Stable values may be compared across consecutive self snapshots only where no intervening mutation occurs: trap_exit, error_handler, priority, group_leader and dictionary. Status/current_function, reductions, queue length and links can vary with execution/signals; tests do not equate their raw values or native resource counts. Current catalog ordering is an SDK implementation choice, asserted only by HOST, not native parity.

## Eight case assessment

1. common-fields verifies all twelve key tuples and registered omission, tolerates native extra physical fields.
2. common-types checks nonnegative integer queue/reductions, boolean trap_exit, PID group_leader and list links/dictionary/suspending; it does not establish precise semantics for each member or validate current_function/initial_call/status shapes.
3. registered-name verifies /1 inclusion and /2 agreement.
4. stable-projection compares five stable fields with /2; this is deliberately weaker than all-field parity.
5. invalid-targets checks seven non-PID terms against error:badarg.
6. dead-local waits for an actual monitor DOWN before asserting undefined.
7. stale-local changes NEW_PID_EXT creation, checks actual PID type and undefined. Assumes the pinned modern external encoding, intentionally not a portable all-OTP fixture.
8. remote-target constructs a remote NEW_PID_EXT PID, checks PID type then error:badarg.

These are meaningful public semantic projections, not collection-only success. They do not exercise a live other-process /1 snapshot, post-registration unregister transitions, cancellation while waiting, or current_function/initial_call/status content. Existing internal owner-protocol evidence covers portions of that broader path; it does not turn these eight into exhaustive /1 parity. Those gaps do not block the honest bounded registration increment, but readiness must retain them and the four physical omissions.

## Integration boundary

Append the eight modules without replacing historical corpus rows; every case uses the existing isolated compiled module mechanism. Register ProcessInfoDefaultSubsetHostChecks.Run using its named HOST identifier; ensure it is actually invoked. Promote new runtime domain files plus the registry wrapper. Regenerate actual supported-mfas and measured readiness (presence +1 only if observed), and mark process_info/1 supported logical subset instead of full resource compatibility. Do not place SDK-only physical omissions or /2 resource badarg assertions in the native common corpus. Retain /2 parser/error behavior, reference MFA catalog and prior cases. Native eight-case results and exact source/head/runner receipts must be obtained independently before calling the comparison passed.
