# Part 448 independent source plan: genuine distribution handshake and reconnect

Source-only analysis, no VM/build/native/git/CI or shared source mutation. All cases below remain unexecuted until Root captures actual evidence. Authority is pinned OTP29.1.1 source.

## Exact v6 initiating-client protocol

inet_tcp_dist.erl:143,495 starts a TCP socket in packet2 mode. Each handshake message has a two-byte unsigned big-endian length prefix excluding that prefix. ReadExactly bounded length; TCP reads do not preserve packets.

1. Send NAME: ASCII N (0x4e), flags uint64BE, actual local creation uint32BE, node-name byte length uint16BE, node-name Latin1 bytes. Static valid name@host. dist_util.erl:694-710. This differs from CHALLENGE by the extra challenge field in step3.
2. Receive STATUS: ASCII s plus status Latin1 text. Ordinary fresh connection must say ok. Reject unexpected not_allowed/nok. Do not silently interpret alive or ok_simultaneous as success; alive requires another protocol exchange and indicates the previous connection was not fully retired. dist_util.erl:1035-1111.
3. Receive CHALLENGE: N, flags uint64BE, challenge uint32BE, server creation uint32BE, node-name byte length uint16BE, exact server node-name bytes. dist_util.erl:713-725,934-964. Validate full bounds/name, actual original identity/creation, and required flags. Negotiated flags are the intersection (adjust_flags:147), expanding Mandatory25Digest equivalence where necessary.
4. Send REPLY: r (0x72), freshly generated own challenge uint32BE, exactly16 digest bytes MD5(cookie Latin1 bytes concatenated with server challenge unsigned decimal ASCII). No separator, no binary challenge encoding. dist_util.erl:727-735,548-549.
5. Receive ACK: a (0x61), exactly16 bytes; validate MD5(cookie bytes concatenated with own challenge unsigned decimal ASCII). Reject digest mismatch; do not AdoptConnected before this authenticated fence. dist_util.erl:989-1032.
6. Switch framing to uint32BE packet4 only after ACK. inet_tcp_dist.erl:110-122 and dist_util.erl:502-544. Nonempty distribution packets without atom cache start112 (p), then versioned ETF control, then versioned ETF payload if that opcode has a payload. A zero-length packet is a tick, not an ETF term. Service/echo ticks without creating a busy echo loop; preserve serialization of socket writes and bound pending reads.

Use only a dedicated synthetic session cookie, never export a real user cookie, challenge-derived digest or private credentials in review reports. Raw transcripts must obey the project's secret handling convention. Successful verified digest and subsequent actual guest ACKs are the evidence; a simulated returned ACK is not.

## Minimal capability profile

- Existing DistributionPassThroughFlags.Mandatory is explicit OTP mandatory25/26 baseline: extended refs, fun/newfun tags, extended pids/ports, export pointer, bit binaries, new floats, UTF8 atoms, maps, big creation, handshake23, unlink-id and V4 node containers. Source lib/kernel/include/dist.hrl and erts/emulator/beam/dist.h.
- Add small-atom tags0x4000 if the ETF serializer emits119, and Mandatory25Digest0x04000000 as the preferred shorthand in addition to explicit mandatory bits. Correction to early analysis: dist.h lists the digest in DEFAULT, not DFLAG_DIST_MANDATORY. Runtime DistributionCapabilityProfile429.Preferred includes it. Do not claim the digest itself is unconditionally mandatory in the current source.
- Published0x1 is optional; including it makes normal nodes() visibility easier to assert. Without it the connection is hidden and nodes([hidden,visible]) must be used.
- DO NOT advertise atom-cache-header0x2000 or fragments0x800000. The actual codec has neither cross-packet cache nor fragment reassembly. Omit SendSender, Alias, Spawn, ExitPayload and alternative-action flags unless needed and actually exercised. DDLL reference requests/replies need only ordinary registered send6 and send2.
- A larger default ERTS flags word is not an acceptable advertisement of implemented capabilities. Record offered/received/intersected flags and verify unsupported flags stay off in the intersection.

## Runtime adoption and actual packet ownership

RuntimeDistributionPending.Connected.cs:7-90: BeginPending(remoteNode), then actual live controller actor adopts the handle via AdoptConnected(handle, controller, negotiatedFlags, actualChallengeCreation). A genuine registered local kernel actor and public setnode/2 admission must precede adoption. Adoption rejects self-node/unsupported/missing flags or reused controller roles, attaches an actual owned output queue, records remote creation and emits nodeup.

SetConnectedInputHandler(handle, controller, controller.Self) before pumping. TakeConnectedOutput returns finalized packet segments WITHOUT the TCP uint32 length; write that length once then packet bytes under one write owner. Received packet4 body goes to DistributionInputAdapter.PutData on the actual input-handler actor; do not bypass the runtime by decoding a payload and runtime.Send-ing it locally. This is the central difference from part447 raw TCP/ETF observer traffic.

Ordinary registered request uses control {6,FromPid,'',RemoteRegisteredName}, payload {request,FromPid,CorrelationRef,Operation,...}; replies use control {2,'',ToPid} and actual guest payload. Both terms are separately versioned ETF after marker112. Runtime codec supports these layouts in DistributionPassThroughLayout/Codec. Retain actual on-wire buffers and actor-observed replies. Stop/controller system packets may be control-only; use the existing process packet decoder, not an assumption that every packet has payload.

## Genuine original setup and reconnect fences

Original must boot real net_kernel through normal -name/-sname startup and inet_tcp_dist, not the minimal registered kernel447 actor. Discover its actual listener port through a real original erl_epmd query/readiness receipt; do not connect to the old ddll_tcp447 packet4 server. If needing a private epmd, Root must own a dedicated bound daemon process/port through existing bounded ownership recipes, not kill a shared epmd or launch an untracked daemon. Record exact pinned executable/source/provider/observer identities.

For first connection, require authenticated ACK, actual original nodeup/connected nodes containing this managed node, runtime ConnectedSnapshot actual node/creation/controller, and an ordinary guest request/reply both ways. The server challenge ACK alone does not prove runtime data-plane integration.

For disconnect, close/join the actual socket read/write owners, terminate/join its actual runtime controller actor, and wait original nodedown plus absence in original connected nodes and runtime idle/absent connection. RuntimeDistributionPending.cs:163-199 calls RetirePending for the terminated controller; Connected.cs:427 onward retires output/links/monitors and notifies nodedown. Root must drain actual owned output operations and capture zero queued buffers/bytes; nodeup/nodedown are separate from native DDLL physical cleanup.

For reconnect, keep original peer VM and local node identity alive, create a NEW controller actor and BeginPending generation, run a NEW challenge/digest exchange against the real inet_tcp_dist socket, and adopt only authenticated actual flags/creation. Assert connection generation advances and stale old handle/input is rejected or inert per the exact API contract, never delivered into the new queue. Wait original second nodeup and exchange fresh real guest ACKs. Do not treat a node-creation change as required for a same-VM connection-generation change.

## Measured DDLL cohort

1. Actual driver held by real port; creator has exactly1 awaiting DDLL monitor. Send that actual reference through runtime distribution to original peer. Peer demonitor returns complete badarg stack; own original reference stays local on original peer. Echo it through genuine distribution back to creator; same local ref and queue1->0 after creator demonitor.
2. New queued monitor1 held across socket disconnect. Verify creator/port stay alive and awaiting count remains1; connection teardown must not be confused with DDLL monitor ownership. Peer retained actual reference survives its nodedown. After authenticated reconnect, request peer to return it; creator demonitor removes the real row, count0. Record both genuine distribution generations and actual peer node epoch unchanged.
3. Peer-produced actual DDLL reference sent to managed actor must be external/badarg with every frame/argument retained. Send it back over the new connection and original owner demonitor removes its actual queued row. Distinguish foreign admission from owner permission.
4. Local second actor demonitor of returned creator reference returns true but preserves owner awaiting count1; creator removes it. This is optional only if already accepted retained regression covers it; never infer from foreign-peer rejection.
5. If adding original peer OS restart, capture real old/new challenge creations and retain old wire in the surviving VM. Old-peer ref must remain external to new peer generation, new generation roundtrip works. This is a distinct extension, not silently claimed by same-VM reconnect.

Use an original-to-original baseline or the same original-compiled worker against original and managed backends for the complete DDLL outcomes. Own refs/PIDs/generations are related by explicit captured role bindings, never arbitrary output deletion. Retain full stacks and raw transcripts. Explicitly count scenario rows versus canonical tests/API full contracts. Actual original workers, OS VMs, socket owners, native child PhysicalExit/PhysicalCleanup/Completion and zero owner leases must all settle normally; cleanup errors aggregate and independent routes continue after isolated failures.

## Acceptance

No current execution is claimed. Acceptance requires exact-source packet authentication+data-plane traces, true disconnect/reconnect fences, complete expected/actual DDLL results, affected distribution/identity/native regressions on final runtime/package, original provider before/after stability and exact publication-head green CI. A packet4 ETF exchange, fixed reference fixture or direct runtime.Send bypass alone cannot meet this part's explicit distribution requirement.
