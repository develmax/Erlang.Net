# Passive source review: Part 450 probe1

Read-only review of immutable artifacts/library450-probe1. No builds, VMs/native, git/CI or shared changes. This is a source review, not execution acceptance.

## Genuine simultaneous setup

The original node actually starts connect_node toward the managed listener. The listener reads its complete real NAME and then waits on statusDecision without emitting status. While that actual original outgoing setup is pending, the competing initiating socket sends NAME to original inet_tcp_dist. The observed original status selects the candidate, and an independent ASCII lexical assertion requires nok when original>managed and ok_simultaneous when original<managed. This is the actual pinned net_kernel pending branch, not guessed concurrent timing or injected sys_dist state.

The losing original outgoing socket is required to close in the ok_simultaneous branch; the losing host initiating candidate is never adopted in the nok branch. Runtime pending handle is created before both attempts and must equal the winner's adopted handle. Subsequent source calls use actual runtime distribution input/output queues. The host artifact coordinates transport selection; this is not a claim that the complete original net_kernel collision algorithm now lives in the product runtime.

## Alive controls / handoff

False-alive and true-alive/wrong-cookie candidates require actual salive, exact reply, original candidate close and no adoption. Existing managed connection generation is unchanged and fresh guest request/reply must still traverse the existing old socket. This is meaningful preservation evidence rather than an empty state equality.

Valid alive candidate verifies original challenge/ACK digest. After sending its correct reply, retireOld callback awaits actual old socket EOF/reset and joins its actual runtime controller before the host reads candidate ACK/adopts. The wording RetiredBeforeHostAckRead is precise: source guarantees original old controller retirement before ACK, but separate buffered TCP reads cannot prove a global wire-arrival ordering. Same peer VM creation/new runtime generation and old queued0 are asserted. Peer actually retains the reference across this same-VM handoff, and its exact returned wire cancels the genuine local monitor1->0.

One evidence-strengthening opportunity: original connection_row before alive is saved, but rows returned after false/wrong-cookie are discarded. If reporting explicit original controller/conn_id equality, save/assert those exact fields in additional immutable evidence. Current continuing physical old-socket data plus unchanged managed generation already proves the narrower accepted path; do not infer an unrecorded original-row comparison. Do not expand into unrelated refactoring merely to improve a report.

## Planned forced original process-tree death

FatalPeer450 snapshots actual process parent relationships, restricts its set to the admitted owned original root and descendants, opens process handles/start times, invokes Kill(entireProcessTree:true), joins every captured family process and records a separate deliberate scenario operation. It does not enumerate-and-kill all Erlang/system processes, and the private EPMD/managed native child are separate owners.

Python's actual original creator alone classifies the expected nonzero old exit, requiring exact PID match, Passed kill receipt and KillRequested. Cleanup fallback remains distinct; unexpected nonzero or forced cleanup still fails. Oracle/replacement/private EPMD must exit normally. This preserves planned-operation versus accidental-cleanup evidence. A snapshot only proves the captured descendant family; do not claim adversarial future-spawn containment/job-object semantics.

Actual forced old peer death then uses449's genuine nodedown/absence, immutable old external ref, surviving local DDLL queue1 and new OS PID/creation/generation protocol. New peer memory loss is explicit. A genuine new monitor1 survives rejection of old-peer ref; fresh new ref cancels0. No fictitious unload notification from the dead old VM is required.

## Residual failure-path boundaries

Collision incoming/outgoing candidates are locals without an outer finally owner until the selected link is assigned. If setup fails before selection, the run can lack candidate cleanup receipts; fixture/outer owned-process timeout must settle them, and such a run remains failed/incomplete. Constructor/dispose exceptional paths inherited from449 likewise must not be credited from missing receipts. They do not weaken successful-path criteria but limit any claim that every failed preparation has the same fine-grained cleanup proof.

## Actual acceptance requirements

No450 execution is credited here. Require both lexical orders' actual statuses and winning socket digests; losing socket close/no adoption; false/wrong-cookie old-channel data; correct alive new generation/peer memory; intentional kill family/actual parent nonzero receipt; new original OS process identity/creation; full raw DDLL error/value comparisons; normal remaining OS/native/EPMD cleanup and zero queues/leases. Distinguish paired source observations from auxiliary protocol guards and canonical test counts. Exact final runtime/package, input/provider stability and affected/retained/full CI gates remain Root responsibilities.
