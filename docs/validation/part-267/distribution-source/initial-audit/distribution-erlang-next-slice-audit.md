# Distribution: next independently runnable slice

Source-only audit, 2026-10-14. Reference is OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Main observed at d2daaab10d53a4988702c5bac4d352cf3cf00fb8. No builds, native executions, Main source changes or registration changes were performed.

## Actual present foundation

`ProcessRuntime.cs:25-29,75-89` fixes Node/NodeAtom at construction; new local PIDs default to creation zero. `RuntimeNodeQueryRegistrations.cs:9-11` derives is_alive from the constructor node string, not from a distribution lifecycle. `RuntimeNodeQuery.cs:26` accepts connected/visible/hidden queries without selecting any live channels. `RuntimeKnownNodes.cs:5-88` weakly observes foreign PID/port/reference identities; this is not a connection table. `RuntimePort.cs:3-40` wraps OS child stdio, owner links, byte counters and OS-port pressure. Its Connected property means controlling Erlang PID, not a remote node. There is no distributed controller entry, protocol transport or authenticated channel in these inspected owners.

`RuntimeReferenceResources.cs:5-36` supplies genuine typed owned reference resources, suitable for a separate future DistributionEntry resource kind. Reuse its identity/lifetime pattern, not its prepared-code payload. Process exit cleanup is in `ProcessRuntime.cs:1338` and runtime close in `:1430-1451`; a distribution owner must be retired there before a handle can escape into a replacement generation.

## Native contracts which control the design

All following anchors refer to the pinned tree, read with git show rather than the sibling checkout HEAD.

| API | Native source | Required behavior and precedence |
| --- | --- | --- |
| Handle | erts/emulator/beam/erl_node_tables.c:382-397 | Two-tuple `{ConnectionId, MagicReference}`; payload destructor identifies a DistEntry. A plain reference or tuple imitation is insufficient. APIs separately check current connection ID. |
| notification/1 | erts/emulator/beam/dist.c:4273-4329 | No caller controller entry => notsup first; mismatched handle/generation => badarg. One-shot armed notification, immediate notification for queued output; duplicate arm does not duplicate pending notice. |
| put_data/2 | dist.c:4333-4405 | List flattening/trap precedes the binary path. Invalid handle => badarg, non-input-owner => notsup. `in` increments before payload type/tail-bit validation. Decode failures may still return ok and kill the channel asynchronously. |
| set_opt/3 | dist.c:4409-4448 | No controller => notsup. Matching entry/current ID required. Only get_size boolean; successful mutation returns the PREVIOUS boolean. |
| get_opt/2 | dist.c:4452-4485 | Same ownership/generation checks, only get_size. |
| get_stat/1 | dist.c:4489-4529 | Handle/current ID required, caller need not be controller. `{ok, InPackets, OutPackets, PendingAllocatedQueueBytes}`; port input/output bytes are wrong units. |
| input_handler/2 | dist.c:4533-4555 | Invalid handle => notsup. Caller must be entry controller unless it is a port controller; handler must be internal PID, with no explicit liveness lookup at this stage. Current generation checked. |
| get_data/1 | dist.c:4559-4745 | No controller => notsup first. Exiting/empty => none. Finalizes native distribution header; consumes ONE output buffer, increments packet count, returns binary iovec or `{Size,Iovec}`. Pending memory includes buffer overhead and can change during finalization; busy recovery uses half-limit hysteresis. |
| setnode/2 | dist.c:4978-5070 | uint32 creation and valid non-noname node; not already alive; registered live net_kernel and defined monitor trap; creation >=4 and collision avoidance. Publishes local node identity atomically and makes net_kernel its lifecycle owner. |
| setnode/3 | erts/preloaded/src/erlang.erl:6077-6089; dist.c:5127-5539 | Erlang wrapper waits for RPC result for another controller PID and raises atom errors with original args. True distribution net_kernel, valid flags/creation/controller, remote existing PENDING entry, mandatory flags, controller not already attached; port requires soft-busy driver. Publication sends `{dist_ctrlr,Controller,RemoteNode,SetupPid}` to net_kernel and actual nodeup; controller exit owns disconnection. |

The dist.c comment at 5104 calls the internal helper /4, but its actual definition is `erts_internal_create_dist_channel_3` at 5127. Do not port the stale comment as an arity contract. `notify_dist_data` at dist.c:3191-3211 sends the bare atom dist_data through the system signal path.

## Recommended slice and candidate path

Create isolated `D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/distribution-controller-foundation-prototype`. First deliver a bounded HOST foundation, unregistered in CoreModules: `RuntimeDistribution` owns an entry table, separate typed handle resource, per-connection generation, live controller process attachment, input-owner identity, immutable finalized output buffers, one-shot notification and packet accounting. Keep each namespace-level type in its own file; semantic protocol names/limits in separate owner files. One explicit lock order must cover table/entry/resource admission, process exit, enqueue and dequeue. Notifications execute after releasing these locks through the existing mailbox system-signal path. Snapshot/close invalidates old generations without allowing handle serialization to confer authority.

Smallest meaningful successful HOST surface: attach a controller to an explicitly HOST-only channel; get/set get_size (return previous value), transfer input handler, arm notification, consume an actual immutable finalized-buffer queue, expose real packet counts and separately recorded queue allocation, then close/reconnect/stale handle rejection. Injecting arbitrary bytes is a HOST queue test, not an Erlang distribution encoder or authenticated connected transport. Do not register Erlang MFAs or change nodes(connected)/is_alive for this foundation.

First honest Erlang API promotion should be get_opt/2 + set_opt/3 only AFTER the channel setup and owner mechanism is accessible through actual setnode lifecycle (or a real alternate distribution adapter), with differential successful owner calls and stale/wrong-owner calls. Two generic notsup/badarg wrappers alone do not complete these APIs. get_stat must wait for actual accounting including allocated output-buffer overhead. get_data/notification need a real distribution encoder plus enqueue/signal race tests. put_data needs a distribution packet decoder and actual signal routing; setnode/2 requires local identity migration/creation across PIDs, refs, timers, resources and ETF factories, and kernel exit semantics. setnode/3 needs pending-to-connected creation, mandatory flags and controller/port lifetime. These are prerequisites, not honest zero-counter substitutes.

## Concrete native probe route

Use the pinned process-controller transport example `lib/kernel/examples/gen_tcp_dist/src/gen_tcp_dist.erl` with real two native nodes and TCP endpoints. Instrument the configurable output loop, following `lib/kernel/test/erl_distribution_SUITE.erl:2965-2979,3040-3068`: default get_size false, set true returns false, get true, set false returns true, set true again; validate returned iovec with iolist_size, then send genuine buffers through gen_tcp. This existing upstream route provides actual handles, controllers, queue traffic and an input handler. It is preferable to inventing a fake net_kernel or labelling a string-named runtime connected.

Separate deterministic raw-ETF summaries from nondeterministic physical byte/packet observations. Each probe must preserve actual raw stdout/stderr, exit code, source/BEAM hashes, pinned build and controller PID/checkpoint/DOWN evidence. No native execution has occurred in this audit.

Suggested probes: (1) ordinary caller/no handle checks for precise notsup versus badarg precedence; (2) true controller get_size old-value sequence and invalid option/value; (3) unrelated process same genuine handle; (4) real empty drain and idempotent re-arm, then one genuine remote message wakes controller; (5) get_size iolist-size agreement with actual transmitted bytes; (6) real input-handler transfer, former versus new owner; (7) controller exit and reconnect, retain old handle and reject generation reuse; (8) statistics monotonic packet deltas and pending drain without asserting OS byte counters or host allocation equals ERTS allocation; (9) invalid/non-byte-aligned input and channel teardown, recording the counter/exit outcome rather than assuming no mutation. Native hypotheses must be frozen before execution.

Native `erts_internal:new_connection/1` exposes a pending `{ConnectionId,DHandle}` (erts_internal.erl:728-732), but raw use bypasses net_kernel bookkeeping. Reserve direct internal probes for explicit lifecycle contrast tooling; the default semantic evidence should use the existing transport handshake. No socket/epmd/CT suite was started here.

## Evidence and reuse scope

No external runtime library is proposed. Port the pinned ownership/state algorithms to C# with .NET immutable buffers and locks; preserve original attribution in the future repository notice decision. Existing OS ports, typed resources and mailboxes are owner foundations, not interchangeable distribution implementations. Before production implementation, record this reuse decision in docs/dependency-decisions.md and review identity migration with the process/ETF owners.
