# Original-only Linux init progress observation

This diagnostic tool reuses the six exact staged582 fixture sources. It has no dependency on Erlang.NifUpgradeObservations or an unbuilt managed runtime. It performs source admission by default; target execution requires --execute and an actual pinned source-build receipt.

The optional manual workflow mode deliberately skips N1 and .NET. It runs in a distinct original582 job/concurrency/artifact on a separate diagnostic branch. Its success is neither N1 acceptance nor N2/ENV or managed compatibility. Current PR100 runs must not be cancelled/restarted for this experiment.

The existing pinned SchedulerAffinity source builder supplies the real original VM. Existing OwnedGuest506 provider checks, Linux OwnedProcess/stage cleanup, passive ETF decoder and actual pinned header hashes are reused. Three distinct ELF NIFs are genuinely built; four BEAMs are genuinely compiled, including two subject generations with different hashes. All compiler outputs and original compile ETFs are retained.

The unchanged constructor has a monotonic2000ms bound; the observer uses its unchanged ACK poll/receive bounds and the owned VM remains90seconds. The complete nine fields (and complete raised stacks), raw ETF, event log and complete cleanup ETF are retained before classification. ACK-written may race constructor-ACK-seen, so only the declared partial order is required. Old-generation unload must precede current deletion; each accepted subject owner must unload exactly once. A failed observation remains a failed original observation. Process cleanup always uses only the existing owned child.

Native compiler flags retain -Werror and exempt only unused static utility functions in the shared fixture header. Real nm/readelf output is preserved; there is no managed bridge or symbol workaround.

Initial scope is the positive ACK constructor-progress reference only. The no-ACK and no-ready negative controls from the staging protocol are NOT executed or declared accepted by this composition. They remain explicit follow-up original observations before a broader lifecycle redesign. No fixture/golden/native-timeout change is included.

After the publisher verifies exact parent4b720d3 and creates a separate diagnostic branch, dispatch with expected_head equal to that new full commit and init_progress_original_only=true. Do not dispatch the current PR100 branch or represent the deliberately skipped N1 job as passing.

## Controlled original successor

Retain failed original run41429934648 and its full nine outcomes/stacks, event log and process receipts. It requested +S2:2 but did not record online schedulers, pin actors or establish a runnable/warm ACK before DLOpen. ACK only executed after constructor timeout; this observation alone is not proof of a constructor-wide VM barrier.

This explicit changed test-observer variant pins the loader worker to scheduler1 and ACK to scheduler2, requires actual scheduler/online guards, and waits for the actor to report actual ack()->not_ready before starting the constructor. The original50ms actor delay remains before the armed handshake;500 polls/5ms, constructor2000ms and VM90s remain unchanged. A separate full scheduler-profile ETF records actual observations. All nine positive expectations are unchanged; a veto still FAILS the positive case. No result is declared before genuine original execution.

Cleanup distinguishes zero, one and two accepted SUBJECT owners: expected unload pairs are respectively00,10,11. Rejected v2 handle closure is not a NIF unload callback. ACK has a separate accepted library but no unload callback. Old/current/ACK/observer cleanup outcomes are all retained independently. Positive acceptance still requires two subject accepts and exactly one unload for each. Result, complete stacks, profile, cleanup and event evidence are classified independently so a veto does not hide cleanup evidence. This is not execution of all three lifecycle cases or the unimplemented negative controls.
