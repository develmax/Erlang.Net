# Part 451 independent pinned-source plan: check_pending / already_pending

Read-only source analysis, pin ad05823719d77c8faee87348ea39513d4e2f99c5. No builds, native/VM execution, git/CI or shared edits. Part450 proves genuine pending lexical collisions and up/alive handoff, not the two branches below. Proposed controls are not execution credit.

## Exact OTP branches and wire outcomes

check_pending is a real net_kernel connection state during discovery/protocol selection, before any outgoing handshake NAME:

- net_kernel.erl setup_check/5:2337-2376 spawns linked CheckPid; its select_mod/2 runs each listener module select(Node), sends setup_check result, then waits to be killed. The parent inserts sys_dist connection state=check_pending, owner=CheckPid, real conn_id and waiters. Timer belongs to this actual checker.
- inet_tcp_dist.erl select/gen_select/fam_select:61-79 calls configured epmd_module:address_please(Name,Host,Family). Built-in erl_epmd.erl:270 simply calls inet:getaddr. Holding a NAME as in450 is too late: that is already pending.
- net_kernel setup/5:2278-2317 cancels checker timer, unlinks/kills CheckPid, then transitions ONLY matching owner+check_pending to pending and actual transport setup owner if select succeeds. Late result for a different current owner is ignored except old owner bookkeeping. setup_check_timeout/3:2321-2333 similarly changes only matching checker/state; stale old-checker timeout must not destroy a winner.
- accept_pending:1505-1543 always chooses incoming when state=check_pending, regardless MyNode>Other lexical order. It kills/joins the old checker with remarked, installs accepting owner as pending and returns internal ok_pending. dist_util:338 maps ok_pending to actual wire sok_simultaneous, not scheck_pending. Test both orders, especially the order where ordinary pending450 returned snok.

already_pending is an INTERNAL reply, not a wire status:

- net_kernel accept_pending:1551-1553, when state=up_pending, returns {accept_pending,already_pending} without changing current/pending owners.
- dist_util mark_pending:359-362 calls shutdown for already_pending. It does not call send_status. A second candidate closes without a status/challenge/authenticated adoption. Do NOT invent salready_pending, snok or a fake badarg wire response.
- up_pending comes from a real first duplicate while an established channel is up. First gets salive and waits for strue/sfalse. Holding it before that reply gives a genuine stable branch for the second duplicate.

## Recommended genuine check_pending oracle fixture

Use documented discovery callback selection, not fake sys_dist insertion or hot-patching original net_kernel:

1. Boot the admitted original VM normally using private foreground EPMD450. Load an ORIGINAL-ONLY fixture module exposing a bounded delegating address_please/3 barrier. The module records its actual caller self()=CheckPid/Node and waits for an explicit session release before calling erl_epmd:address_please/3.
2. Set application kernel epmd_module to that fixture only inside the owned test VM, after normal boot. net_kernel:epmd_module/0:2521-2545 reads this documented configuration; do not combine it with -epmd_module (source rejects both). inet_tcp_dist:549-553 falls back to erl_epmd for callbacks the fixture does not export. Hash the fixture and label this controlled alternative discovery configuration, not default DNS behavior.
3. Start actual original connect_node asynchronously from an auxiliary original control case so the same control server can still observe state. A synchronous connect_peer call blocks that single auxiliary server; reuse450 asynchrony carefully rather than sending two commands concurrently on one unprotected stream.
4. Wait boundedly until actual sys_dist is check_pending AND row owner equals the actual blocked callback caller. Callback may enter before parent inserts the row; do not infer state from callback readiness alone. Retain complete row, checker PID, timer/role observations and original waiter.
5. Send a real initiating NAME to original inet_tcp_dist. Require actual sok_simultaneous in BOTH ASCII lexical orders. Observe real checker death and accepting owner replacement, unchanged intended conn_id until the source transition, then complete actual cookie/ACK, runtime queue data and waiter true. No invented original response.
6. Restore original discovery env and release/settle any fixture callback/helper in finally, even on handshake failure. The checker is normally killed by incoming selection; do not wait for a reply from a dead checker. Actual stale timer can later arrive; only call it naturally tested if the actual armed timer really fires. Manual injection of setup_check_timeout is a separate explicit stale-control fault test.

A barrier module/configuration is a supported alternate-discovery fixture but still controlled instrumentation. If forbidden for a stricter default-configuration profile, do not replace it with a timing race and claim deterministic coverage; leave that profile separate.

## Recommended genuine already_pending scenario

1. Establish an actual authenticated old channel with usable common-worker data, genuine DDLL monitor1 and captured original sys_dist owner/controller/conn_id.
2. First duplicate uses exact captured name/creation/flags, receives actual salive, and is held before true/false. Confirm actual source row state=up_pending and capture first pending_owner.
3. Open a second duplicate with the same valid NAME. Require actual physical close before any status/challenge; distinguish EOF/reset from timeout. Capture zero received protocol status bytes and no authenticated adoption. Complete original row must retain same up_pending pending_owner and old controller. Optional original-only send trace of net_kernel's exact accept_pending reply to second setup can strengthen branch attribution; this is passive tracing, not state injection.
4. Prove fresh data still traverses the old socket and DDLL queue remains1. First candidate must still be live; release it with false, join its close, observe row restored up/no pending_owner and old data again. A subsequent genuine candidate provides positive authentication/data control. This proves the second duplicate did not steal/cancel either the old connection or first candidate.
5. Keep setup timer bound explicit so a genuine first-candidate timeout cannot be mistaken for already_pending rejection. No forced sleep-based branch selection. Whole raw status/close/row/data evidence remains separate from any expected constants.

## Current managed runtime frontier

RuntimeDistributionPending/DistributionPendingState has Idle, Pending, Closed, Connected, Exiting, not CheckPending or UpPending. BeginPending reuses an entry and increments only on Idle->Pending. ReserveKernelSetup/ReserveControllerSetup enforce an actual live kernel, one reserved controller and current generation. AdoptConnectedOwned checks actual Pending, no existing Connected, matching reservation and unused controller before attaching its real queue and changing to Connected. Thus current host/native ownership can enforce a single winner, but it is not the full original net_kernel discovery/candidate state machine.

Node-controller requests capture generation and revalidate on the actual target mailbox. CompleteControllerAdoption returns badarg/not_supported for a loser; RetireControllerAdoptions replies real badarg after entry/controller retirement. Native create-dist-channel429 separately revalidates actual pending/kernel/controller and closes a refused handle. AbortPendingConnectionNative's bool means not connected; stale generation is a successful NO-OP and must not retire a newer candidate. These are real product contracts; none maps to a fabricated already_pending wire status.

Recommended own-state controls: real kernel+reserved C1, second C2 reservation refused with C1 state/generation unchanged; actual C1 termination or abort settles old entry/requests and fresh BeginPending advances generation; two real deferred adoptions have one actual winner and other real badarg reply; stale old abort/input cannot retire new winner; actual queued data/leases drain on owner/kernel death. Reuse existing controls where already sufficient. Do not add public product enums or pretend an own Pending state is literal OTP check_pending merely to name the test.

## Minimal honest acceptance

- Original controlled-discovery check_pending takeover, both lexical orders, actual row/checker/waiter, real sok_simultaneous and authenticated common-source DDLL data.
- Original real up_pending second duplicate, no invented status, actual preserved first/old owners, physical refusal, DDLL queue1 and subsequent real cleanup/positive control.
- Separate managed actual candidate/adoption/generation controls with exact own API outcomes. Original-source observation does not automatically prove our runtime executes original net_kernel/bootstrap or alternate discovery callbacks.
- Capture full raw rows/status/EOF/auth/error/value trees with explicit own-ref role bindings, distinguish source pairs from auxiliary controls. Keep fixture/config/fault-injection boundaries and failed preparations. Require final source/runtime/package/provider hashes, actual checker/first/second/socket/controller/native/EPMD settlement, zero queues/leases, affected/retained regressions and exact publication-head CI. No API/full-module or default DNS/full managed boot promotion from these few scenarios.
