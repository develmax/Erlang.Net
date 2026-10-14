# Priority permission and local link admission candidate

2026-10-14. Isolated archive of published84fff75d85f0491fc7ea02730a1ad92c5c9320ab (implementation231). Main, shared233 and completed candidates unchanged. No build/native execution performed at source freeze. The six already recorded original232 mismatches in this baseline remain untouched; integration into repaired233 needs narrow hunks, never whole-file overlays.

## Actual behavior

process_info(Pid,priority_messages) reads a locked count of actual active permission identities: priority alias entries, priority origin-monitor entries, caller-owned priority process links and priority port links. It neither counts messages nor duplicates ownership in a new cache. Reply aliases, DOWN/EXIT controls and existing monitor/port lifecycle callbacks remove actual identities at admission; unread priority messages do not keep permission enabled. Monitor aliases retain their independently ordinary alias flag. Pending request references and failed allocation do not count. This scalar key does not require an owner snapshot or force signal admission; mixed lists keep their existing requested snapshots.

Local link BIF creation now installs caller ownership immediately and queues a real invisible reciprocal LINK control. Each owner has its own link identity; initial spawn links remain installed directly, as native process creation requires. Unlink clears caller activity and priority immediately, retains an unlink tombstone, and queues reciprocal UNLINK. Admission removes a recipient link only when it is not already unlinking, then queues an invisible ACK. ACK removes only the caller identity with the same pending unlink token. Relinking cancels that token; old ACK cannot delete the renewed link. Incoming LINK respects an existing link or unlink tombstone. Recipient death includes pending LINK origins in linked EXIT fanout, so dying before admitting LINK does not strand the caller link. Controls return no ordinary payload, do not create nil messages, and do not change ordinary admitted counts or receive-trace origins.

## Pinned branch review

OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5 only:

- erl_bif_info.c854: priority_messages flags0 and main lock; c2321 reads FS_PRIO_MQ. Standalone query must not FORCE_SIG_SEND.
- erl_proc_sig_queue.c9502-9587: active alias/link/monitor permission refcount installs/clears FS_PRIO_MQ independently of retained priority queue/end marker.
- bif.c175-203: local link creates caller entry, revives unlinking entry, emits reciprocal LINK signal. Queue handler c6635-6658 inserts only when no existing receiver entry.
- bif.c1212-1260: caller priority removed immediately; unlink id/tombstone retained until acknowledgement. Queue handler c6682-6735 removes recipient entry when not unlinking and sends ACK; corresponding ACK handler compares unlink identity. Candidate models unique identities rather than numeric wire ids because no production distribution wire exists.
- bif.c5853-5927 / queue c6013-6218: independent alias priority and reply admission retirement. bif.c978 and queue c6458-6507: monitor activation / actual DOWN retirement. PRIO_ML is not PRIO_ALIAS.
- erl_process.c12923-12950: local spawn parent link/monitor activation precedes async reply; successful suppressed replies do not remove live permissions.

The existing async spawn request reply/token/control callbacks are unchanged. The existing abandonment cleanup now also removes the discarded link identities and child priority entry alongside its existing child link removal, preventing stale ownership after that existing operation. Distribution/pending remote spawn and physical BEAM priority queue implementation remain explicit gaps. No per-process physical telemetry or process_info/1 claim.

## Frozen tests and limits

Nineteen new original-ready done-outcome sources append after the entire4747 baseline;151 diagnoses/six exclusions are not edited. Sources cover dormant/ordinary versus priority alias, multiple aliases, unread priority payload after unalias, reply-alias admission, self link, upgrade/downgrade/duplicate link, mixed link+alias, ordinary/priority/self monitor, unread admitted DOWN, retained ordinary monitor alias, time-offset monitor, local spawn_request before reply and suppressed success, duplicate list keys and dead target. Bounded await_down inspects real messages and leaves DOWN unread; it is not a synthetic notification.

One host uses two actual process owners held by completion barriers,64 unlink/relink/admission rounds and64 concurrent priority reply aliases. It checks direct completed scalar requests before admission, stale ACK immunity, receiver permission retirement, zero ordinary control messages, and exactly-once alias retirement with64 real retained payloads. These are implementation/source reviews, not passing results: all tests/builds await coordinated slot and actual execution. Port lifetime and distributed phases remain source-reviewed existing producers, not new native proof.

Expected before measurement:4766SDK modules/4760 comparable/151 diagnoses/six exclusions,482 registered exports (erlang324),6367 test cases if base6347 plus19 sources and one host. No new MFA: existing process_info/2 gains an actual key. Original5689 plus fd8 is only a plan, not evidence.

## First focused execution

Coordinated build succeeded with zero warnings/errors. Complete4747 inherited source/generated/outcome/context/policy/finite bytes/order,151 diagnoses,six exclusions and482 exports guard passed. First focus19/20 retained: actual64 unlink/relink/ACK rounds and64 concurrent reply aliases passed; suppressed-success source accidentally supplied invalid reply mode error. Pinned erl_process.c parser accepts error_only, not error, and the managed runtime correctly created no child/permission on badopt. Only this new unpublished source changes to error_only; the same priority assertions and independent done remain. Correction ledger contains full old/new source and hashes. No runtime change in response; second focus pending.

## Revised source review before second focus

Pinned handle_exit_signal4676-4723 checks current local PID link and unlinking flag, then removes the entry even for ignored linked EXIT. There is no local generation-id filter; distributed connection-loss protects a connection id separately. Linked EXIT admission therefore removes inactive local tombstones too, without adding an invented generation guard. Pending-only incoming LINK on an exiting receiver uses noproc (queue7363), distinct from the actual killed/normal reason for admitted links. The candidate fanout now preserves each pending LINK signal separately with noproc and its real token propagation, while admitted links retain their actual death reason.

The same host additionally runs64 simultaneous reciprocal unlink rounds and16 actual pending-LINK recipient deaths, alternating trapped noproc EXIT and unlink-before-death ignored EXIT. It reads the internal identity dictionary count only (reflection reads, no mutation) to check no-ACK tombstone cleanup. Real receive tracing checks the exact ordinary EXIT subject/receiver/PID, and ignored/control paths emit no ordinary receive event. All new host branches await actual second focused execution; first19/20 remains retained.

## Second focused execution and source-only lifecycle review

Revision2 build0 warnings/errors and exact4747/151/6/482 contract guard passed. The extended actual host passed64 unlink/relink rounds,64 simultaneous reciprocal unlink rounds,64 concurrent reply aliases and16 real pending-LINK deaths, with actual ordinary EXIT receive tracing and ignored/noACK tombstone cleanup. The corrected error_only source passed. Second focus19/20 retained a new dead-target timeout: immediate child exit can precede monitor creation and legitimately yield noproc, while the source demanded normal. That source now holds child until monitor installation, sends stop, requires normal DOWN then queries the dead PID. The unread-DOWN source had the same theoretical creation race despite passing both focuses; source review applies the same gate while retaining its unread-message and false-permission assertions. No old4747 contract or independent done outcome changes; full old/new Source/hash ledger retained. Third focus/build and full remain pending while the peer's coordinated full runs.

## Third focus and pre-full caller reactivation refinement

Third build0 warnings/errors and exact complete4747/151/6/482 guard passed; third focus20/20 passed all19 sources and the extended actual host. These revision3 results/source manifest/patch are preserved before refinement. Final source review identified missing-target trap_exit caller reactivation: native bif.c175-203 performs its live lookup rejection first, then creates/modifies caller link, returns early for already active link, or clears unlinking before emitting LINK/noproc. Both live and dead-trap candidate branches now use one caller activation helper. Dead-trap reactivation clears an old pending unlink token; an already active link keeps the native shortcut after option flag changes. No local EXIT generation filter is added.

Eight real old-ACK/dead-peer-relink host rounds now establish LINK, admit actual reciprocal UNLINK so ACK remains pending, kill the real peer, reactivate the caller dead-PID link under trap_exit, read the pending-token invariant without mutation, then admit ACK/noproc EXIT and verify ordinary trace/last-permission cleanup. Fourth build/focus is held while Main235 full runs. Same19 source names/outcomes and one host remain; no old contract changes. The initial refinement script stopped at a host formatting anchor after applying the narrow runtime edits; it was completed before any build, preserving revision3 first.
