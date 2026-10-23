# Passive packet review: Part 451 probe1

Read-only review of ddll451_epmd.erl, ddll_tcp451.erl, Program.cs and DistributionTcp451.Initiator.cs. No VM/build/native/git/CI operation or shared edits. Root owns first actual execution. This note provides no runtime acceptance credit.

## check_pending fixture is genuine and explicitly controlled

The original-only configured discovery callback blocks the actual select_mod checker, records self() and matching Node, and delegates non-target discovery to erl_epmd. check_start enables the documented kernel epmd_module setting only in the owned original VM, spawns the actual connect_node waiter asynchronously, observes the callback PID and then waits for actual protected sys_dist state=check_pending with owner=that exact PID. It does not insert or overwrite a connection row.

The host initiates a genuine NAME against original inet_tcp_dist while that checker is blocked. It requires real sok_simultaneous for both node-name orders. check_finish receives actual monitored checker DOWN reason remarked and actual waiter true, removes the barrier/restores previous env and observes original up. Source setup-timeout latency is not automatically tested merely because the checker was replaced; keep natural stale timer behavior separate unless captured.

The runtime pre-existing Pending lease/handle changes to its real Connected state and must equal the authenticated adopted handle. ManagedCheckPendingStateClaim=false is appropriate: the SUT enum does not acquire a fictional CheckPending state. This demonstrates controlled original callback takeover plus genuine managed ownership/data transport, not full standard managed discovery/bootstrap.

## already_pending has no invented wire response

The first duplicate genuinely receives salive and is held before sending false/true. Original raw_connection is required to show up_pending; complete original row before/after the extra candidate must be equal. Extra uses the same valid node identity/flags and reads one actual socket byte: EOF or a specifically recognized connection reset/aborted error passes; any received byte or timeout fails. No status, challenge or adoption is invented.

Actual old runtime snapshot is required unchanged. Fresh common-worker data on the old socket is required while the first false or true candidate is held. False first candidate is then released/physically closed and original wait_up plus fresh data prove rollback. True first candidate is released with actual authentication and waits old controller retirement before host ACK read/adoption, preserving450's precise ordering claim. Real peer memory/DDLL ref and queue1->0 checks follow. Both false/true cases therefore demonstrate that rejection of the second candidate does not steal the current connection or cancel the first candidate.

## Tracing and observer strength

Original net_kernel send tracing filters the actual {Kernel,{accept_pending,Reply}} tuple and recipient PID. trace_finish disables tracing, waits trace_delivered(Kernel), then sends collection request to its single receive-loop tracer. This is a meaningful delivery fence; it does not fabricate a branch reply or patch original code. Trace values are saved but not asserted by the current Program. Independent acceptance should explicitly confirm real ok_pending and already_pending entries/recipient relationships in those saved trees; raw silent close alone has weaker branch attribution.

RejectExtra compares the managed full snapshot before/after outside one controller turn. Any unrelated actual current input can create an observer race, similar to449, even if extra candidate is correctly refused. If actual execution exposes it, retain the failure and serialize the exact observation window rather than relaxing queue/accepted-input invariants. Do not pre-label a real differing state as harmless without evidence. Complete original row equality can likewise reject unrelated original state progress; preserve actual rows on failure.

## Lifecycle / boundaries

check_finish restores env/barrier on normal success. Earlier check_start/check_finish exceptions do not have local after-restoration; such a fixture run must remain failed and its owned original VM must settle through Root's existing cleanup, with no claim of successful per-hook cleanup. Held candidate objects/tasks also need actual completion/cleanup receipts before acceptance; missing exceptional-path receipts are not success.

The live original checker and monitor, async connect waiter, tracer, first/extra candidates, authenticated winner, native child and original VM/EPMD must be distinguished. A connect waiter/tracer reply alone is not an independent process-DOWN receipt; source normal return is clear, but do not over-count it as another observed join.

Reuse450's exact actual owned forced-kill operation ledger, normal oracle/replacement/EPMD exits, zero native leases/channel queues and complete raw error/value comparison. Source fixtures/configuration are original-only controlled instrumentation; SUT has no discovery callback execution or full net_kernel state-machine claim. Canonical API/full-contract counts remain separate from observation rows and auxiliary traces. Final actual profile results, pinned provider/binary freeze, affected/retained regressions and exact publication-head CI are required before acceptance.
