#318: Make public group_leader/2 actually publish to an existing remote connection

Bounded source-only proposal. Pin OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. No Main/Git mutation, SDK/build/native/VM/network execution. docs/ERLANG_COMPATIBILITY.md401 and supported-mfas.json list group_leader/2 as registered with bounded evidence; this proposal closes a real semantic branch without adding another API name.

## Confirmed production gap

ProcessSpawnRegistrations routes the registered erlang:group_leader/2 to ProcessRuntime.SetGroupLeader. That method returns true immediately whenever target.Node != Node. It neither looks up a connection nor queues a control packet. This agrees with some disconnected/not-alive outcomes but is a silent no-op even when a real connected controller exists. A connected remote process retains its old group leader, so remote I/O can continue to use the wrong destination despite a true result.

Pinned bif.c4914–4970 distinguishes local target fast/fallback handling and external target distribution preparation. PREP_PENDING or PREP_CONNECTED calls erts_dsig_send_group_leader; NOT_ALIVE/NOT_CONNECTED can return true. dist.c1832–1840 emits {DOP_GROUP_LEADER,Leader,Remote}, opcode7. erlang.erl3334–3350 waits on the local fallback ref and documents signal ordering. Native dist.c2872–2883 receives that three-element control and forwards it to the destination's signal queue. Existing SDK RuntimeDistributionPending.ProcessPacket already accepts opcode7 and QueueConnectedGroupLeaderSignal queues an owned local update. Existing inbound tests are real input-only controls and do not exercise the missing public outbound path.

## Smallest useful implementation

Limit318 to ALREADY CONNECTED remote targets and current local fast path. Add an internal async ProcessRuntime guest route taking the actual ProcessContext so the registered MFA can own cancellation and output admission. Keep the existing embedding bool signature to avoid unrelated API breakage; do not use its remote no-op branch in the new guest route. Both arguments must remain Pids; reject foreign-owned in-memory local identities rather than serialize another runtime's internal object as an owned process. An external leader need not be alive or belong to the connection's node. Do not add a leader-liveness check.

In RuntimeDistributionPending, find the existing node entry under its real gate, capture connected object and ConnectionId, and call existing PublishLiveControl with the real producer/context, {7,leader,target}, producer.Self as encoder owner and producer cancellation. That existing path owns exact-generation resource acquisition, ordered output ticket, linked cancellation, encoding, bounded queue admission and release/drain. Await it from the registered MFA; do not call unowned EnqueueConnectedOutput, detach ScheduleConnectedControlSignal or return true before output ownership is reserved. On generation retirement the existing live publisher returns false and MUST NOT resend into a replacement connection. The public Erlang outcome can stay true for an accepted signal lost to disconnect, as native external behavior does not promise destination existence. Real caller cancellation/halt still propagates through existing runtime paths.

Do not implement autoconnect, pending-handshake buffering, local fallback ack protocol, generic driver ABI or broader distribution in this part. If no existing connection is found, retain and disclose the current partial behavior rather than invent an automatic connection. Public351-name presence is unchanged and is not module completion.

## Permanent HOST controls, authored RED then actual GREEN required

1. Real runtime/kernel/controller/session; invoke ACTUAL registered erlang:group_leader/2 from the producer with a real connected target. Decode controller's actual output as opcode7/Leader/Target/no payload. Historical implementation emits nothing, so this is the primary meaningful RED (bounded take timeout or empty queue), not a callback-shaped fake.
2. Two public updates from one producer preserve their observed packet order; a following already-owned SEND from that producer comes after them. Drain/decode actual queue packets, preserving identity/creation encoding.
3. Fill the real bounded output queue. Public call remains pending until space is drained, then emits exactly one control; no early success, duplicate or lost ticket.
4. Kill/cancel the actual producer while admission is blocked; join operation and prove no packet appears afterward, no leak blocks controller/resource cleanup. Do not merely abandon WaitAsync.
5. Retire/reconnect generation while blocked; old call may resolve true on retired publication, but replacement queue has no stale control. A fresh public call uses the new generation and emits once.
6. Local setter behavior/dead local target/bad argument shapes and foreign-runtime identity rejection remain meaningful controls. Do not rewrite existing incoming/suspension/timer fixtures: source audit confirmed that this pin really pauses suspended BIF/receive timers, so that tempting alternative was not a valid gap.

## Original comparison source, separate future execution

A small supplied Erlang probe run/1 assumes TWO original OTP nodes are already connected and the probe module's exact compiled binary is loaded on both. The collector owns bounded start/stop, exact pin/provenance, source/binary identities and raw output; no peer/autoconnect implementation is added to SDK. Cases: connected change observed by target query, two updates followed by same-caller probe, dead leader PID accepted, dead remote target returns true. Assertions compare stable projections while retaining raw PIDs in actual captures; PID/node renaming must be explicit, never hide ordering or backend behavior. SDK counterpart is its registered-MFA/actual-controller HOST and any later supported real transport roundtrip. Original-only observations plus HOST are separate evidence until a real same-case end-to-end SDK comparison exists.

Gates: source peer; complete317-based closure; format/token guards; targeted HOST meaningful historical RED then fixed GREEN; actual full suite/public fixture unchanged; bounded fresh original two-node collection and remote tests. No run is claimed by this document. Preserve absence of native scheduler/full distribution and the pending original interleavings disclosures.

Revision2 corrects excerpt ranges from the exact Git blob, not the modified OTP working tree. Revision1 and its hashes remain unchanged.
