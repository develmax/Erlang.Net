# Proposed 325: retain connected LINK before busy wait

Source audit only, 2026-10-16. This is a future proposal, not an implementation, observed VM result or claim about complete distribution parity. Candidate 324 remains separate.

## Confirmed native phase

OTP29.1.1 pin ad05823719d77c8faee87348ea39513d4e2f99c5, bif.c:321-323 passes no_suspend=0, no_trap=1, connect=1 to erts_dsig_prepare for an external link. Lines345-369 insert the distributed relation before sending. Lines371-374 call erts_dsig_send_link then YIELD_RETURN only on SEND_YIELD. dist.c:1454-1457 creates the LINK tuple; 3201-3210 sends a control-only signal with msg=THE_NON_VALUE and asserts that CONTINUE cannot occur.

In dist.c:3616-3646 SEND first validates the captured connection. The fragment loop3649-3672 admits fragments when no_trap is true even if the distribution queue is already busy. LINK is a small nontrapping control; its output is appended to dep->out_queue at3695-3705 BEFORE suspension at3711-3732. Completion clears ctx->obuf at3778-3781, then returns SEND_YIELD at3798. The distribution queue owns these bytes; the suspended process list is separate. The LINK BIF does not retain a resumable not-yet-admitted LINK context. Thus at the native busy event, even async_dist=false, LINK has been enqueued. This conclusion is limited to this connected control path; it does not generalize to partially encoded/fragmented ordinary messages or a failed/retired connection.

dist.c:3297-3301 sets ignore_busy for F_ASYNC_DIST, and3711-3713 only suspends when !ignore_busy. Therefore true also enqueues this control but returns without this busy wait. In erl_process.c:14120-14141 immediate process termination reason follows the distributed relation using a no-process/no-suspend/no-trap EXIT publication on the same captured connection. The earlier LINK has already entered the queue, so it precedes this EXIT. This is source evidence, not yet a two-node measured ordering result.

## Actual SDK mismatch

Bound base is formatted actual SDK 323 focus 5DC06B0D895EEC67562DE36D36CEA0C700D95297769BEC7454FCB9BCA21AAF06, closure FEF4EF7CAF603DCBE9A395C815A10984FA8C95AD23051BAF4387B45A88509E9D. ProcessRuntime.GuestLinks.cs installs the relation then calls producer-owned PublishLiveControl for both flag values; it never checks AsyncDistribution. RuntimeDistributionPending.OwnedOutput.cs:102 links the caller cancellation token, validates actual producer liveness, and waits for bounded transport admission. DistributionOutputSequencer checks cancellation before each admission and releases the ticket on failure. A killed producer blocked on capacity can therefore lose the unpublished LINK, while the retained terminal EXIT continuation may follow. Its queue contains EXIT without the preceding LINK. Current 323 control-pump work correctly delivers kill and query while waiting, but its original busy tests do not compare these wire phases.

The SDK bounded capacity wait occurs before transport enqueue, whereas the native busy wait occurs after enqueue. A queue-capacity adaptation must not be described as native buffering parity. Also async_dist=true LINK currently waits on this same producer route; existing 320 SEND/322 EXIT support does not fix it.

## Bounded proposed fix, subject to review

Reuse the existing 320 accepted immutable connection-owned staged output record, unique ticket, resource lease and global lifecycle drain. Do not add a second worker framework or retain ProcessContext in the accepted worker. At connected LINK preparation, reserve bounded credit/ticket and snapshot the final encoded control outside the runtime gate. Revalidate generation and relation before atomically accepting the record and committing its identity. All failures before acceptance must release resources and leave no ghost relation/output; disclose system_limit on the finite SDK hard cap as an adaptation.

Both flag values need retained accepted LINK ownership. False waits on a caller-owned cancellable proxy through the existing 323 control pump. Killing it cancels only this proxy, so actual LINK remains ordered before the retained terminal EXIT. True returns once accepted, preserving existing accepted-worker order without waiting for transport capacity. A committed LINK worker must not recheck producer liveness or cancel because the producer dies. Generation retirement still drops old output without replay. Local/port/disconnected, pending/autoconnect, aliases/names, full scheduler/physical-memory behavior remain outside scope.

The exact acceptance/relation failure order and existing duplicate/unlink identity predicates require implementation review: an earlier UNLINK must suppress an uncommitted LINK, but later UNLINK/EXIT must not erase accepted LINK bytes. No relation mutation, custom ETF callback or await under the runtime gate. Existing ACK/relink/retired inbound-link revision5 safeguards remain unchanged.

## Meaningful checks

1. Same-source public guest test over old/fixed production: fill connected controller queue, generated normal link, actual inbound EXIT2 kill before drain, await fresh killed DOWN, then drain and require LINK followed by terminal EXIT with captured PIDs/reason. Current production should fail missing LINK; test must compile against old APIs. Distinguish killed DOWN from cleanup noproc.
2. Flag true: same blocked transport, require link call completes and local relation exists before drain. Kill and require the accepted LINK then EXIT, proving flag-sensitive return and shared retained ownership. False must remain blocked before kill/drain.
3. Pending-ticket order, accepted UNLINK/relink, stale ACK, receiver priority, hard-cap refusal rollback, kill-before-accept and generation retirement/halt join without replay. Keep all existing321/323 regressions.
4. Original collector extension: reuse owned three-VM STOP/busy prerequisite, but additionally query B's links after resume while B traps terminal EXIT; capture LINK-before-EXIT effect with a remote handshake or narrowly validated controller-level control observation. The current323 query/kill proof alone is insufficient. Do not infer absent LINK from an after-kill local table. First review a measurable original observation before claiming wire equality; no invented control bytes or fake busy/suspend_process prerequisite.

No Main edits, SDK/compiler/VM/native/network execution or implementation were performed. All positive/negative HOST and original comparisons above are future gates.
