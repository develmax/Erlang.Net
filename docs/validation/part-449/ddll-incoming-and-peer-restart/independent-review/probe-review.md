# Independent passive review: Part 449 preparation through successor4

Read-only source audit. No compilation, VM/native execution, git/CI or shared edits. This note concerns finalized artifacts/library449-probe4, with earlier preparation faults retained separately.

## Preparation faults found and corrected before acceptance

- Initial probe1 Program remained an outbound448 body calling nonexistent Connect while DistributionTcp449 exposed Accept. Root confirmed the initial rewrite had not executed. Do not credit any449 execution from it.
- Successor2 Program.cs105 had one unmatched closing parenthesis (7 opening/8 closing in that statement). Corrected in later immutable successor.
- net_adm:ping would call gen:call to the remote net_kernel is_auth service indefinitely (pinned net_adm.erl101-107); minimal managed kernel only receives stop. Successor4 correctly uses actual ORIGINAL net_kernel:connect_node true/false through the auxiliary original server, without importing net_adm/net_kernel into the managed common worker, synthesizing pong or bypassing runtime data delivery.
- Original Process handle now captured BEFORE Stop, then retained and joined before replacement launch. Constructor-failure cleanup added for listener/restarted process; successful restarted process stream joins are now required.

## Current success-path review

No additional success-path semantic blocker identified.

DistributionListener449 constructs genuine ALIVE2 registration, consumes actual 32-bit assigned creation and keeps registration open. Public managed start_node_creation uses that creation, and both inbound challenges use the same value. Actual original connect_node triggers the TCP connection to the registered listener; Accept parses modern N name/flags/creation, verifies actual reply digest, emits ACK, then adopts the real controller and pumps runtime queues. Minimal mandatory/no-cache profile stays explicit. Wrong-cookie original call must return false, actual receiver verifies digest mismatch and does not adopt; subsequent true connection and real remote data provide the positive control.

The local native DDLL monitor is queued1 before actual old peer stop. EOF/expected socket reset is separately awaited; channel/controller retired; both source branches await real nodedown and confirm peer absent while local queue remains1. Actual old Process handle joins exit0; new owned process uses original command/name/provider with only fresh ready-file path. New actual peer identity must have the same name and a different observed creation; incoming connection also gets a new runtime generation, and its creation must match actual peer identity. No guessed numeric increment is used.

Retained foreign ref creation and whole encoded wire remain unchanged after the new connected entry adopts the new peer creation. Actual old peer dictionary is observed lost (undefined), rather than claimed to survive an OS restart. Surviving local ref is explicitly sent to the new VM, rejected there as foreign, returned exactly and removes its real old local queue1->0. New peer creates/holds a genuine monitor1; actual old-peer ref is sent over distribution and rejected, preserving that queue1; fresh peer ref roundtrip then cancels to0. Thus stale rejection is not an empty-queue-only test.

Old channel snapshot is refused before/after restart. ProbeStale uses the correct new actual input-handler actor and genuine old packet; current accepted packets/queues stay unchanged. A following genuine current-channel reply increases AcceptedInputPackets. Full values/error trees are recursively compared with only captured own-reference bindings.

## Boundaries / observer details

- old-peer-ref-rejected-by-new-peer is fully compared, but its explicit assertion checks exception marker/badarg rather than the class/stack-cardinality guard used for names containing foreign-reject. If stating exact class/profile cardinality as an additional independent assertion for this row, add it explicitly or report only the existing whole-tree comparison. Do not invent a shorter stack shape.
- New-peer-reference cancellation RemotePair calls execute twice on the same genuine peer owner; first removes the row and second legitimately returns true for the completed internal ref. Report actual queue1->0 once, not two independent removals.
- Auxiliary original bootstrap/control, self identity checks/constants and actual paired common-worker observations must be counted separately; row count is not full MFA contract count.
- This implements modern static-name fresh inbound acceptance and actual graceful peer OS halt/restart. It does not establish alive/simultaneous connection handoff, malformed protocol partitions, TLS, forced peer termination, standard managed net_kernel/application boot or full module compatibility.
- Residual failure-only cleanup can still throw inside restarted constructor kill/wait or pump Wait before writing exit receipt, and Program peer/listener Dispose calls are unguarded. Such a run must remain failed/incomplete, with parent-owned timeout cleanup; do not infer successful settlement from missing receipts. For normal acceptance demand every explicit actual exit/pump/channel/native receipt, not merely a final boolean.

## Required actual acceptance evidence

No449 success is claimed by this source note. Root must capture inbound raw handshake reply/ACK digest verification, EPMD/runtime creation consistency, normal old/new/oracle OS exits and joined pumps, real nodedown/absence, new PID+creation+generation, frozen old wire, stale/new queue1/0, native PhysicalExit/PhysicalCleanup/Completion/ZeroLeases, listener/registration/privateEPMD cleanup, unchanged provider/source/package bindings and exact final regression/CI gates. Preserve failed preparations and initial actual observations without expected rewriting.
