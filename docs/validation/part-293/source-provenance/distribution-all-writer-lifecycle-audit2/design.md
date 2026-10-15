# All distribution writers and exit-continuation ownership

Source-only second audit. No Main edit, build, native execution, network, changed budgets or Git mutation. source-bindings.json binds actual Main reads, old isolated5 candidate files and exact pinned OTP extracts. Old isolated5 full output ends without a final full result; **its cause is unknown**. The source counterexamples below are not a diagnosis of that incomplete run.

## Writer inventory and current risks

Main has two finalized-packet sinks: RuntimeDistributionPending.Connected.cs EnqueueConnectedOutputOwned (message+payload) and RuntimeDistributionPending.LinkSignals.cs QueueConnectedControlSignal (control-only). Both encode and use HostDistribution.TryEnqueueFinalizedHostBuffer; false becomes synchronous QueueFull. The latter is shared by:

| Writer/caller | Role and ownership | Commit-sensitive state |
| --- | --- | --- |
| BeginConnectedProcessUnlink, DistributionLinks.cs | Live local caller, outgoing UNLINK_ID35 | Allocates unlink ID and removes active link only after successful admission |
| QueueDistributionUnlinkReply, DistributionLinks.cs | Input-handler/deferred owner callback, UNLINK_ID_ACK36 | Must preserve acknowledgement after matching connection signal processing |
| QueueDistributionLinkReply, DistributionLinks.cs | Incoming LINK to dead target, or process termination | EXIT3 noproc/terminal reason; cannot require dead local source to remain a live process |
| BeginConnectedProcessMonitor, DistributionMonitors.cs | Live observer, MONITOR19 | Currently adds monitor before sending; admission failure cannot leave an installed unsent monitor |
| EndConnectedProcessMonitor | Live observer, DEMONITOR20 | Currently removes monitor before sending; rollback/terminal generation policy required if publication waits or fails |
| QueueConnectedProcessMonitor missing target | Input handler, MONITOR_EXIT21 noproc | A system-generated reply; cannot silently drop on pressure |
| ConnectedMonitorProcessTerminated | Exiting observer/target, DEMONITOR20 or MONITOR_EXIT21 | Captured monitor identity, original watched form, established/noproc reason |
| ConnectedLinkProcessTerminated | Exiting linked local process | Captured link identity, active/pending reason; remove once after state transfer |
| EnqueueConnectedOutput overloads | Explicit host-owned or live sender messages | Serializer owner/node snapshot; synchronous host API cannot pretend to suspend Erlang caller |

Future spawn29–32/alias33–34/alternative37, automatic message/explicit EXIT2/trace/payload writers must use the same ownership/admission substrate. Main does not currently route every public remote operation through these two sinks; exposing an async host enqueue helper is not evidence that all guest writers use it.

## Old pressure candidate counterexamples

DistributionOutputAdmission.AdmitAsync gives one reservation per ProcessContext, ordered admission and head-only encoding. RuntimeDistributionPending.AsyncOutput links its wait to sender.Cancellation and revalidates RequireProcess(sender). That is suitable only for a live ordinary sender; an already-dead source performing required terminal EXIT/DOWN cannot satisfy it.

The old candidate changes both synchronous sinks to TryRunUnreserved. If **any** reservation exists, that method returns false even with free actual queue capacity. The caller throws QueueFull. It is therefore a fence, not all-writer asynchronous integration. Giving more queue room would not fix this branch.

ProcessRuntime.Terminate currently removes processes[pid], then calls distributionPending.ProcessTerminated, which synchronously invokes both terminal writer families. Slot removal, Terminated/ExitReason assignment, Stop.Cancel and Done completion occur later. A synchronous writer exception can therefore interrupt cleanup after public process removal but before Done/slot release. The actor's finally also calls Terminate before executingProcesses removal and Unwound completion, so a thrown termination hook can skip those instructions. This is a source-level failure path requiring a controlled test; it does not establish that this caused the old observed incomplete full run.

Begin monitor/demonitor and queued signal callbacks have analogous state-before-publication windows. Merely catching QueueFull avoids a host exception while **losing an obligatory control**; merely returning ok or classifying a valid packet as malformed is not an implementation.

## Pinned OTP lifetime and pressure semantics

dist.c3268-3298: ordinary process sends participate in busy suspension; process-null/system context and F_ASYNC_DIST set ignore_busy; no_suspend can report WOULD_SUSPEND before sending. dist.c3595-3785 appends actual encoded fragments, sets busy threshold, suspends/resumes the sender and returns YIELD or CONTINUE with real continuation state. Native busy_limit is not an exact three-dimensional hard packet/byte/segment cap. Keep the SDK's existing budgets intact and describe its bounded admission separately; do not inflate limits to imitate native ignore_busy.

erl_process.c13600-13635 and13715-13755 show terminal MONITOR_EXIT and EXIT sends handling YIELD/CONTINUE, storing dist_state and forcing a later scheduler turn. Full exit uses a phase cursor and retains process references (14234+). **Important correction:** at FREE, erl_process.c14456 removes the process from the public process table, unregisters resources and captures link/monitor trees before subsequent distributed exit phases14635+. The native runtime retains a separate continuation/process ref until DONE and eventual delete. Public liveness false before all remote exit work finishes is legitimate. Requiring IsAlive=true or an occupied public process-table slot until managed Done would assert the wrong native invariant.

The needed invariant is retained **runtime-owned cleanup**, not indefinite public visibility. Slot release should occur once at the defined public FREE boundary, with termination identity and cleanup kept separately; native slot/count exact timing still needs direct observations before making a parity claim. Managed Done and Unwound have distinct host meanings and must not become orphaned.

## Concrete implementation boundary

1. Add a runtime-owned ProcessExitContinuation with first-winning reason, captured local/distribution links/monitors, current phase/index, unencoded pending control descriptor and generation/connection leases. Claim termination once under runtime gate; mark EXITING, stop ordinary actor execution and transfer state before removing public lookup/slot at a documented FREE boundary. Keep continuation in a separate runtime-owned collection until completion.
2. Centralize **all** producers on a connection-bound admission interface with explicit roles: live ordinary process; exiting-process continuation; input-handler/system reply; explicit host caller. A dead/exiting producer must not pass RequireProcess or use the cancelled actor Stop token. Validate its captured exit capability instead. Retired connection/generation cancellation remains valid; it must never publish into a replacement connection.
3. Reserve an ordered place under the appropriate owner gate, transfer only a compact unencoded descriptor, release gates, encode only when eligible and attempt actual bounded queue admission. Await capacity outside runtime/mailbox/admission gates. Retain exact immutable packet until admitted or the captured generation retires. Do not double-encode/advance trace serial on retry. Revalidate state at commit.
4. A process may owe many EXIT/DOWN controls, including several on one connection. One-reservation-per-ProcessContext cannot be called concurrently for all links. Use one exit cursor with at most its current operation reserved per connection, or an explicitly bounded role/operation identity with serialized owner chain. Do not turn every owed control into an eagerly encoded hidden side queue. Existing captured link/monitor collections provide the work source; no new unbounded packet backlog.
5. Incoming control-generated replies currently run inside synchronous Mailbox pending callbacks. Split state claim from async publication, as an owned signal continuation; callbacks must not synchronously Wait/GetResult on capacity. Enqueue and drain owner tasks with a real completion handle. Reusing payload deferred-drain patterns requires checking nested-wait ownership, not assuming every callback can await safely.
6. Define each operation's linearization point: unlink identity and monitor installation/removal versus actual publication. Preserve local ordering and native signal semantics; do not attempt generic rollback after a packet was committed. A generation retirement resolves an uncommitted obligation without mutating replacement identities; it is not permission to drop a live-generation packet.
7. Termination pipeline runs in a guaranteed finally envelope: scheduling/accounting cleanup cannot be skipped by a transport hook exception. Expected retirement/cancellation transitions have explicit outcomes. Unexpected publication failure is recorded and handled through the connection's real teardown policy; never complete a success-looking Done while silently losing a required live-channel control. Complete Done exactly once when owned logical termination is complete; complete callback Unwound in the actual actor finally, and separately track exit-continuation quiescence.
8. Dispose/initializer exit/halt must drain or retire those owned continuations according to the existing shutdown contract, without holding runtime gate across awaits. Close connection/output waiters before waiting for exit completion where shutdown legitimately retires them. Include outstanding exit/output tasks in actual runtime quiescence, not a snapshot that can miss later chained work.

## Options and tradeoffs

**Preferred:** asynchronous owned exit/signal continuations plus one common admission owner. It preserves bounded bytes and releases scheduler threads during pressure, but requires real lifecycle changes across termination, monitor/link signals and shutdown. Implement that whole boundary together, then integrate spawn protocol; avoid promoting the old two-sink fence as a finished prerequisite.

**Not acceptable:** synchronous blocking under gate, always-error QueueFull fence, fake ok/drop, hidden unbounded encoded controls, queue inflation, or canceling all terminal output with actor Stop. A per-connection runtime signal worker is viable only if its work ownership is bounded and represented in quiescence; assigning all control traffic to the controller without preserving original sender/trace/epoch identity changes semantics.

Global FIFO across unrelated producers is an implementation choice, not a pinned native guarantee. Preserve required per-sender/signal ordering and prevent bypass of already reserved owner work. Tests should not assert an invented universal ordering where OTP permits interleaving.
