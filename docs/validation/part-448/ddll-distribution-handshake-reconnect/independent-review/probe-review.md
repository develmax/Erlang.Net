# Independent source review of Part 448 probe1

Read artifacts/library448-probe1 and completed library448-probe-run1 receipts only; no VM/build/native/git/CI or shared edits. This is an unaccepted pilot/source review, not an execution credit.

## Sound protocol/integration choices

DistributionTcp448 matches pinned v6 NAME/CHALLENGE/REPLY/ACK layouts, packet2 framing, unsigned-decimal cookie MD5, fixed-time ACK verification, offered/intersected supported flags, no atom cache/fragments, then packet4 framing. It adopts a genuine runtime controller only after verified authentication, pumps actual finalized runtime output buffers and feeds actual incoming bodies through DistributionInputAdapter.PutData. Guest remote/3 uses registered distributed send/reply; the main roundtrip does not use direct host runtime.Send for data delivery. Final worker stop is a lifecycle message, not a data-plane substitution.

Wrong-cookie negative requires actual challenge receipt then EOF without adoption; unexpected successful ACK or timeout fails. Separate original nodes use real -name boot and real private foreground EPMD/PORT_PLEASE lookup. Source explicit queue1/0 checks and genuine unload notification/native child cleanup are retained.

## Required corrections / narrower claims

1. Program RejectOld uses fixture.Actor, which is not the distribution input-handler controller. Thus a wrong-caller refusal can be mistaken for stale-generation rejection. PutConnectedData also converts stale ReadHandle ArgumentException into ErlangException(badarg); the current catch only ArgumentException/InvalidOperationException misses this real public failure. Before reconnect, verify retired handle with ConnectedSnapshot or another generation-specific state read. After reconnect, test old handle through the actual NEW input-handler controller, and positively prove same-caller current handle succeeds without duplicated guest delivery. Keep exact badarg semantics; do not catch all exceptions as acceptance.
2. The held ref is never sent to/stored in the peer before disconnect: first held-ref-after-reconnect decode occurs after the second handshake. This proves local DDLL queue survival, not remote retained-term survival. Send the actual ref through distribution before socket closure into a peer process dictionary, then retrieve/encode that retained term after reconnect without re-sending the old wire. Record actual queued1 before/after and0 upon creator cancellation.
3. foreign=peer.Call(new_ref) currently arrives through auxiliary raw TCP and is then passed directly to Managed(demon). That admission check is genuine foreign-origin, but not distribution-delivered. Add a stored actual peer ref getter through remote/3 for BOTH original and managed workers, and prove the returned term matches the auxiliary provenance if desired. Other roundtrip paths already exercise actual distribution data plane correctly.
4. peer await_down proves a real nodedown event, but does not assert disconnected node absence. Add seen_node=false checks before beginning the next handshake, alongside runtime retired/idle state. Record creation unchanged and connection generation changed (already asserted), without calling a connection generation a node epoch.
5. FinalSnapshot is captured before pump stop and only accepted/consumed counters are asserted. It is useful data-plane evidence, but does not by itself prove zero queued payload/segments/buffers or owned output operations drained. Add actual empty queue/drain and controller completion fences if reporting full distribution queue cleanup. Preserve native PhysicalCleanup as a distinct fence.
6. EPMD cleanup fallback terminate/wait can throw inside outer finally before provider-after scan/completion output. Catch/aggregate that final cleanup independently so after scan and failure report still happen. Normal successful cleanup does not expose this path, but failure evidence must remain complete.

## Actual pilot evidence read

library448-probe-run1/completion.json is Passed=false, Groups=[], FileNotFoundError before any comparison. Both6427-file provider scans pass, inputs unchanged, Runtime94744C030C53C4B93E55168E8A14A852EFF5B5A8AB8DE0D7611E2B76F04B3A81. No handshake, wrong-cookie, reconnect or DDLL observation is credited from this pilot. Locate the admitted epmd executable via provider manifest rather than assuming installed/bin/epmd.exe.

Physical original peer OS restart remains a separate optional extension. Same-VM authenticated reconnect plus actual retained monitor/reference is this part's stated scope. Do not promote pilot/source availability to full module/platform compatibility or canonical API-contract counts.
