# Part 449 independent source plan: inbound handshake and actual peer restart

Read-only source analysis. No builds, VM/native execution, git/CI or shared edits. Scope extends accepted448 outbound/same-VM reconnect, whose actual limitations are explicit in docs/NATIVE_API_LIMITATIONS.md:33,72 and Part448 README.

## Inbound v6 authority and sequence

Pinned OTP lib/kernel/src/dist_util.erl:206-244 handshake_other_started is the accepting path: recv_name; expand Mandatory25Digest; validate mandatory flags; mark_pending/status; send_challenge; optional old-protocol complement; validate again; intersect flags; verify challenge reply; send challenge ACK; connection/adopt. Modern N handshake has no old n/complement exchange.

1. Actual original inet_tcp_dist node initiates a TCP connection to the managed listener (packet2). Read NAME N + remoteFlags64BE + remoteCreation32BE + nameLength16BE + Latin1 node bytes; reject unexpected name, bad length or missing required capabilities before adoption. dist_util:775-802.
2. Send status sok only for an admitted fresh/pending peer. Send CHALLENGE N + ownAdvertisedFlags64BE + ownChallenge32BE + actualLocalCreation32BE + ownNameLength16BE + actualLocalName. The accepted448 advertised mandatory/no-cache profile can be reused. Do not just replay the old initiating direction packets: NAME and CHALLENGE have different field layouts.
3. Read reply r + peerChallenge32BE +16-byte MD5(cookieLatin1 + ownChallengeUnsignedDecimalASCII). Verify exact digest/shape; then send a + MD5(cookieLatin1 + peerChallengeUnsignedDecimalASCII). Use a fresh actual challenge, bound packet sizes/timeouts, one socket writer.
4. Only authenticated acceptance may adopt a real runtime controller using actual remote NAME creation and negotiated flags, then packet4 runtime input/output pumps. Require genuine original nodeup and distributed common-worker replies, not just socket ACK. Authenticate both directions independently from actual raw transcript digests.

Dist_util:314-388 mark_pending has ok, ok_simultaneous, alive and pending replacement protocols. A minimal current experiment may expressly admit only a fresh/fully retired connection and reject other statuses. It must not silently call this complete simultaneous/alive replacement support. Wait actual old connection death before next restart admission rather than bypassing a valid source state.

## Making the original node genuinely initiate

Original must use its real net_kernel/inet_tcp_dist setup, e.g. a common-worker net_adm:ping(actualManagedNode) or registered send initiating normal connection. Register the managed listener in the existing dedicated private EPMD so original port discovery is genuine. OTP erl_epmd.erl:428-455 ALIVE2 request: packet2 length, x(120), port16BE, nodeType M(77), protocol0, highVersion16BE=6, lowVersion16BE=6, shortNameLength16BE, shortName Latin1, extraLength16BE, extra bytes. Keep this registration socket open for the lifetime of the managed node; closure unregisters it.

Actual ALIVE2 response is v(118), result0, creation32BE (or supported legacy y(121), result0, creation16BE), NOT a packet2-framed reply. erl_epmd.erl:487-515. Use the actual assigned creation in public managed erlang:setnode/2 and challenge; record registration/handshake/runtime identity consistency. Read before advertising readiness. No shared/global EPMD mutation; private owner/registration/listener/socket cleanup all require receipts.

## Restart identity invariants

- Original external.c:3132 dec_get_node and4803 ref decode resolves node+creation; only equality with erts_this_node localizes the ref. erl_bif_ddll.c:641 demonitor requires internal ref, otherwise badarg. erl_node_tables.c:805-815,890-908 keys ErlNode by sysname+creation. Same node name is not sufficient.
- Actual peer OS restart must prove old process exited/joined, new owned OS PID/process instance, same node name, new actual handshake creation, new controller/connection generation and restored real data plane. Never emulate restart by changing a snapshot, reconnecting the same process, or just rebuilding driver image. Require observed creations differ; do not assert guessed +1 or fixed values.
- Surviving managed local identity stays unchanged. Its existing local DDLL ref, creator actor, port and awaiting count1 remain alive across remote process death. Returning that ref through the new genuine peer should still localize at its surviving origin and remove queue1->0. Old peer memory cannot retain a term across OS death unless an explicit actual persisted-wire mechanism exists; do not claim in-memory retention across restart.
- An actual old-peer-created ref retained at a surviving actor keeps its OLD node name, creation and words after the active connected entry updates to NEW peer creation. ReferenceTerm externalIdentity is immutable; RuntimeDistributionEntryIdentity.Creation is live channel metadata and must not rewrite retained term identity. Serialize old term before/after restart and require same whole wire.
- Deliver the genuine old-peer ref/wire to newly booted same-name peer: it must remain external and demonitor must badarg with full stack. Keep a genuine new-peer DDLL monitor queued1 so stale rejection cannot vacuously pass; assert unchanged new queue1. A genuine new-peer ref routed out and back cancels only its new row, queue0. Do not construct old/new refs from guessed creation/word arrays.

## Minimal measured cohort / controls

1. Actual inbound authenticated connection, mandatory flags intersection/no atomcache/fragments, original nodeup and genuine registered send/reply through runtime queues. Confirm actual receiving socket direction separately from448 outbound.
2. Wrong synthetic fixture cookie: actual original initiator fails connection, no runtime adoption/nodeup/data delivery. Successful source flow afterwards proves listener/registration remain usable. Preserve actual original authentication diagnostic and raw challenge transcript.
3. Actual original peer exits normally under owned stop protocol while local DDLL pending monitor survives. Original oracle and managed runtime observe actual nodedown/absence and old channel/controller retirement. Capture old external ref and whole-wire identity in surviving actors before the stop.
4. Fresh same-name peer OS boot and inbound handshake; assert genuine process/creation/generation change and current data reply. Old handle tested via correct new input-handler actor remains inert/refused; actual new data increments accepted input. Old remote ref remains unchanged and is rejected at new peer without changing its queued monitor.
5. Surviving local ref makes real roundtrip through restarted peer and cancels its original monitor. Fresh peer ref makes its own valid return trip/cancellation. Full errors/frames compared to equivalent original-source path; explicit own-reference role bindings only.
6. Optional separate malformed-name/missing-mandatory admission controls may demonstrate exact early refusals, but do not expand this part into broad protocol/performance/TLS work. Simultaneous connects, alive handoff, forced peer termination and standard managed net_kernel boot remain separate partitions unless actually tested.

## Acceptance discipline

Original-to-original baseline/common original-compiled worker remains semantic authority. Distinguish paired source outcomes from setup/constants/self-checks. Retain initial failures, full raw protocol/ETF/error frames, exact source/runtime/package/provider hashes, real original VM/native/managed controller joins, queued0 and zero owner leases. Private EPMD/listener registration must also close normally. On final source run affected identity/distribution/native regressions and exact publication-head CI/packages. No API/full-module promotion from a few transport scenarios.
