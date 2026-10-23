# Part 450 independent exact-pin source plan

Passive source analysis only. Pin ad05823719d77c8faee87348ea39513d4e2f99c5. No VM/build/native/git/CI operation or shared edits. Accepted449 covers fresh incoming v6 and graceful actual peer restart, not simultaneous/alive handoff or forced death. The cases below remain proposals, not compatibility credit.

## Exact source state decisions

lib/kernel/src/net_kernel.erl:1499-1577 accept_pending:

- No active/pending entry: allocate real new_connection, record pending/owner and return ok (modern static name), giving status sok.
- state=pending AND local MyNode>OtherNode: reject the incoming candidate nok_pending, giving snok. The existing outgoing pending owner remains selected.
- state=pending with local MyNode<OtherNode, or state=check_pending regardless ordering: select incoming. The original old setup owner is exit(OldOwner,remarked) and its EXIT is joined before the incoming owner replaces it; return ok_pending gives sok_simultaneous. Atom comparison must follow Erlang ordering, not randomized atom IDs. Simple ASCII node names with shared host admit an unambiguous ordinal crosscheck.
- state=up: retain current owner/controller, set pending_owner to the new accepting setup process and state=up_pending. Return up_pending gives salive.
- state=up_pending: another candidate is already_pending and is shut down; do not silently replace the first candidate.

lib/kernel/src/dist_util.erl:314-388 maps those results to statuses. recv_status:1035-1087 handles initiating-side snok by waiting to be killed by its local net_kernel, not treating it as authenticated success. sok_simultaneous continues normal challenge. salive requires replying strue or sfalse according to is_pending(Kernel,Node), which tests whether this setup PID remains the real local connection owner (net_kernel:1580-1585). Modern N messages need no legacy n/complement exchange.

## Alive false / true ordering is precise

- Accepting do_alive sends salive and reads only strue/sfalse (dist_util:1094-1111). sfalse shuts down candidate before any challenge authentication/adoption. Existing connection remains; pending_own_exit at net_kernel:1791-1810 removes failed candidate and restores up/pending_owner undefined. Prove continuing old data and unchanged old controller/generation, not just EOF on candidate.
- strue permits normal challenge/reply. In dist_util:238-243 recv_challenge_reply authenticates FIRST. Only a valid cookie enters wait_pending. A true reply followed by wrong digest must reject candidate while old authenticated channel still works; it must not retire old controller early.
- After valid digest, wait_pending requests actual old controller retirement. net_kernel:1588-1603 exit(OldCtrlr,wait_pending); actual old controller exit triggers up_pending_nodedown:1928-1944, which allocates a new real connection ID, makes candidate owner/state pending, clears old controller and pending_owner, then sends {Kernel,pending}. Only after receiving this fence does dist_util send candidate ACK and run connection/adoption.
- The source guarantee is old controller retirement before new ACK/adoption. Separate TCP socket readers may observe buffered EOF and ACK in different host scheduling order; do not infer network receive chronology is the VM source transition order. Capture actual controller death/generation and gate local runtime adoption on its actual old owner join. Never clear leases or reuse generation merely because a candidate has authenticated.

## Real collision fixture without fabricating pending state

Use the actual original node's initiated connection to a controlled managed listener. Hold acceptance after receiving that genuine initial NAME, while original outgoing owner is really pending. Start the competing managed initiating socket to the actual original inet_tcp_dist listener with the same captured local name/creation. This naturally exercises original accept_pending while its own actual setup is pending; do not inject fake sys_dist rows or a fake response. Retain genuine original connection state/owner observation and both raw sockets.

Run two fresh bounded scenarios using ASCII names for opposite lexical order:

1. Original MyNode>ManagedNode: original rejects competing incoming with snok; select its existing original-initiated socket, complete its real cookie exchange and data plane. The losing managed outbound candidate must not be adopted or cancel the winning pending state.
2. Original MyNode<ManagedNode: original kills/joins its own outgoing owner and sends sok_simultaneous on the managed-initiated candidate. Complete that winning socket; the abandoned incoming listener socket must close/retire without ghost adoption/data.

If using two original OS nodes as a baseline, ensure both actually reach pending under a controlled handshake transport barrier before releasing their competing NAMEs. Mere concurrent tasks/sleeps do not establish a collision. check_pending is a different source state and must not be mislabeled as the lexical pending branch. Retain actual selected setup/controller IDs, statuses and one authenticated winning data path; count fresh-setup source events from actual oracle results rather than assuming every losing setup emits nodedown.

## Alive oracle/control cases

With a real established authenticated channel and genuine DDLL pending monitor1:

- Duplicate candidate using exact current local name/creation receives salive, replies sfalse, settles candidate. Prove original old-channel controller/generation unchanged, original still connected and genuine old-channel guest request/reply succeeds; DDLL queue stays1.
- Duplicate salive/strue candidate sends wrong fixture-cookie digest. Original closes candidate; old channel remains connected and usable, monitor1 untouched. This directly tests authenticate-before-retire ordering.
- Duplicate salive/strue with correct digest: actual old controller exits wait_pending, new connection generation established, new ACK verified and actual data reaches correct new runtime owner. Old handle/input through correct new handler stays inert/refused. Local DDLL monitor1 survives handoff and a genuine ref return removes it to0. Do not replace source wait_pending with an arbitrary delay or manual pre-auth old close.
- Optional second duplicate while up_pending is held must follow already_pending refusal; bounded separate fixture, no unsupported product API invented. Broader simultaneous handoff races/TLS remain separate.

## Forced actual peer OS death

Extend genuine449 restart with an explicit planned kill of only the admitted owned ORIGINAL peer process tree. Preserve immutable command/PID/start identity and exact target ownership; never kill shared epmd, all Erlang processes or the surviving managed runtime/native child. Capture actual descendant/process settlement through the existing owned execution mechanism.

A deliberate forced-death case must distinguish its expected operation from accidental teardown failure: mark PlannedTermination=true/ActualForcedTermination=true, retain actual observed exit code/signal, old process tree joined, and require it is gone. Do not blanket relax all old-node exit0/normal cleanup checks; oracle, replacement, private epmd and managed native child must still finish normally. Windows TerminateProcess exit representation is not a portable guessed Unix signal code. The old killed VM cannot deliver a post-death DDLL unload notification; do not fabricate one.

Before kill, retain actual old-peer DDLL ref at a surviving actor and a genuine local pending DDLL monitor/port. After kill, require actual surviving nodedown/absence, retired old distribution controller/generation, local creator/port alive and queue1. Boot a genuinely new same-name original OS VM with actual changed PID/creation; accept or initiate authenticated channel according to the measured case. Old external ref node/creation/words and whole ETF remain frozen while live remote entry creation refreshes. Old ref rejected in fresh peer while its genuine new monitor stays1; new ref returns and cancels0. Surviving local ref also returns through the replacement VM and cancels its real original queue1->0. Peer in-memory dictionary is lost across forced death, unlike same-VM handoff.

## Events / acceptance boundaries

net_kernel:493-506 documents nodeup before new connection signals, nodedown after old signals and node disappearance, and old nodedown before new nodeup for the same node since OTP23. Measure events and actual visibility/generations; do not sort observed events into the expected order after capture. Not all failed pre-pending handshakes emit nodedown (dist_util:312).

Keep exact pinned source/observer/provider/native package hashes, raw challenge/digest/status/data ETF and full error frames. Compare common-source original-vs-managed values with explicit own-reference role bindings; distinguish auxiliary identity/state/setup controls from paired full values. Preserve every refused/failed preparation. Accepted lifecycle requires actual winner/loser socket joins, setup/controller retirement, no queued data or native leases, normal replacement/oracle/EPMD cleanup, unchanged final source/binaries and all affected/retained/final exact-head CI gates. No full-module, performance, forced-native-teardown or all-platform claims follow from this narrow cohort.
