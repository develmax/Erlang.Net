# Independent accepted review: Part 448 authenticated distribution / reconnect

Read-only source and completed-receipt audit of artifacts/library448-probe3 and probe-run3/probe-run4. No build, VM, native, git/CI or shared source mutation by reviewer. Passive SHA256, transcript MD5 and JSON/byte comparisons are analysis only.

## Accepted composition and honest counts

Accepted: run3 depth8 and run4 depths0/1/2, each34 passing rows, no comparison failures, actual native PhysicalCleanup=true, identical Runtime1A0592162FA6080CA2E14464074B28986C3C23C256C9D106462662EA5A3427E8. Combined136/136 observation rows; independently enumerated and hashed all136 ETF pairs:116 byte-identical/typed-exact and20 comparisons using explicitly captured own-reference pairs. Each profile has29 exact and5 protocol-only rows.

Do not call these136 full MFA contracts or new canonical tests. Each profile has26 paired common-source/protocol observations and8 auxiliary/self/constant protocol guards: peer-setup, peer-handshake-identity, peer-sees-managed, oracle-disconnect, physical-peer-node-down, peer-observes-both-absent, remote-driver-monitor-survives, physical-peer-unload. Thus the composition is104 paired observations plus32 auxiliary guards. Some paired RemotePair operations execute on the same genuine original peer via the two distinct source call paths; this distinction is preserved.

## Genuine authentication and data plane

- Independently recomputed all8 accepted connection REPLY digests and ACK digests from actual challenge/reply/ack files using the explicit synthetic fixture cookie and unsigned decimal challenges:8/8 match. Actual ACK shape17 and REPLY shape21 validated. This is not merely a boolean from the consumer.
- Four actual original peer stdout logs independently contain rejection of ddll448bad@127.0.0.1 with Invalid challenge reply. Corresponding negative authentication receipts show challenge received, no adoption, no authenticated channel or data counters.
- All8 accepted authenticated channels record actual offered/intersected flags17297788821 and packet2-to-packet4. Atom cache/fragments stay unadvertised. Per profile initial actual runtime channel has9 incoming/7 outgoing packets, reconnect6 incoming/4 outgoing; these are actual socket records plus runtime counters, not synthetic guest delivery. The observer remote/3 registered-send enters runtime output queues and incoming bodies enter its distribution input handler. No direct host runtime.Send substitutes for these data-plane replies.
- Actual peer-created reference wire is returned over distribution, then decoded by each common source worker; class/reason/all stack fields are compared. This is an actual serialized-reference payload crossing distribution, not the initial probe1 auxiliary-only foreign reference path.

## Retention, disconnect, reconnect and stale ownership

- A genuine queued creator ref is sent through distribution before disconnect and stored as an external term in the real peer worker process dictionary under held_oracle/held_managed. After reconnect, stored/1 returns that retained term through distribution; the request does not resupply the old reference wire. Returned wire exactly equals the genuine original wire; real creator queue1->0 proves monitor cancellation. Foreign rejection leaves queue1 intact.
- Actual oracle disconnect and managed socket/controller closure produce real peer nodedown observations; both names are explicitly observed absent before new connection. The peer VM remains running.
- New authenticated socket/controller has a new connection generation while the actual peer node creation remains unchanged within each profile. Recorded actual peer creations are1792751314,1792751371,1792751376,1792751381 for depths8,0,1,2. This is same-VM reconnect, not peer OS restart.
- Old handle snapshot is rejected before and after reconnect. Actual NEW input-handler actor probes the genuine old packet/old handle: current accepted packet count and queued data stay unchanged; the public input API returns ok/inert, with its raw attempt counter allowed to advance. Subsequent genuine current-channel remote reply increases AcceptedInputPackets. Thus refusal/inertness is no longer explained by a wrong caller alone.

## Actual cleanup and immutable provenance

- All8 original OS node process receipts: exit0, unforced, settled. Both privately owned foreground EPMD receipts: exit0, unforced, settled. Original workers are monitored/joined before stop ACKs.
- All8 accepted distribution channel cleanup receipts: Passed=true, ControllerJoined=true, socket disposed, actual queued bytes/packets/segments all0. Read/write pumps joined. All4 managed comparison attempts require native child PhysicalExit/PhysicalCleanup/Completion successful, real fixture disposal and ZeroLeases. Native cleanup is separate from distribution nodedown.
- Independently rehashed2685 inputs-before bindings in EACH run3/run4:0 mismatches. Both runs' before/after admitted6427-file provider inventories are equal. Both observer BEAMs byte-identical between accepted runs.
- Independently confirmed Program.cs, DistributionTcp448.cs and both Erlang sources are unchanged packet2->packet3; packet3 runner binds the fresh final package. Initial run1 preparation failure and run2 authenticated-but-zero-output timeout stay failed historical evidence. Their expected source results were not changed to make the product fix pass.

## Product scope and remaining gates

The concrete correction is real guest registered remote-name sending through ordinary/async owned queues, with source-matching REG_SEND/REG_SEND_TT. Priority+ALTACT named sends, synchronous host helper transport and auto-connect remain explicit boundaries from product-review.md. The current mandatory-only observer does not negotiate the unsupported named ALTACT partition. No physical peer restart or all-platform/full-module compatibility is claimed.

Root reports source-guard inventory1621 (one existing EOV guard updated, three existing helper files newly guarded), default API708/full118 unchanged, canonical testcatalog8824 with one new group containing ten routing controls. These global ledger/catalog claims are separate Root publication gates; the passive observer audit does not reclassify APIs.

Local authenticated/reconnect acceptance is supported within this scope. Final affected/retained regressions, boot/style/readiness, exact publication-head complete CI and package checks remain independent gates and are not pre-claimed by this review.
