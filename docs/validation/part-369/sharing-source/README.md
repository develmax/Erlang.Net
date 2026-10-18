# Writable binary sharing events

Source-only, unbuilt seven-file proposal. No Main edits or compatibility promotion. Root integrates. Authority blobs and hashes are from OTP29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 through git show, not the sibling working files.

## Event mapping implemented in the proposal

| Native source | Existing managed entry | Proposed operation |
|---|---|---|
| copy.c834 and1289 pin writable subbits during copying | Mailbox.SendTraced before publishing Entry | Pin the message graph on cross-owner, host-origin, sequence-token or priority copy |
| erl_message.c824 ordinary self/onheap/no-seq avoids copying | Same, Owner seeded from ProcessContext pid | Preserve no-pin for ordinary self send |
| erl_message.c738+ seqtrace branch always copies | Same, token nonnull | Pin even self when sequence copy applies |
| erl_proc_sig_queue.c2077/2160..2230 alias/priority alternate-action always copies | Mailbox.SendPending; SendTraced priority | Pin before storing even self alias/priority |
| external.c3860 encoding and5796 size traversal use PIN | ExternalTermFormat.Write case BitString | Pin exactly when encountered, covering Encode, EncodeIoVec and EncodedSize; nested FUN envs already pass through the same writer |
| erl_nif.c1351 inspect pins before checking partial-bit rejection | NifNativeBinaryCallbacks.InspectCore | Pin a managed BitString before checking IsBinary; preserve return0 for partial input |

Runtime has no source implementation of message_queue_data/OffHeap/OnHeap/QueueData (bounded search src/Erlang.Runtime). Current guest queue model is its existing default. Mailbox.CopySelfMessages is an internal initialization capability, not a guest process_flag implementation. A future real offheap mode must select it from coherent process metadata, and support subsequent flag transitions rather than treating init-only configuration as that feature. Full offheap-mode compatibility is not claimed.

Tracing Output publishes through a tracer mailbox with from null, so it uses the copy/pin path; ordinary same-process function calls/pure reads do not pin. Existing tracing event timing is unchanged and is not independently certified by this proposal. No pin was added to general ObserveIncomingTerm/import: that would incorrectly pin pure same-runtime reads and self-send before event classification.

## Bounded graph cursor

BinarySharingCursor has no CLR recursive descent. Each Step(work) processes at most work frames, where a child continuation schedules only one edge. Thus one huge tuple/map/FUN environment cannot bypass a work budget by pushing all children at once. A reference-identity visited set avoids repeatedly expanding shared graph nodes. It traverses Cons head/tail, tuples, both map keys and values, NativeRecord values and FunctionTerm.Environment. It does not invoke closures or inspect opaque host delegate captures. Records' declaration fields are atoms, not binary payloads. Each BitString invokes its existing backing pin, so aliased buffers are physically shrunk/frozen coherently under the backing gate.

The Pin convenience drains in256-work chunks synchronously; it is stack-safe but does not itself yield or charge ProcessContext reductions. Sync mailbox publication cannot await while holding its gate. Existing send accounting still needs a source-based asynchronous/charging adaptation if full large-copy scheduling is in scope; do not describe this change as complete trap/reduction equivalence. Async callers can drive Step with ReduceAsync between chunks only after target/copy classification and before publishing, retaining original failure/event order. Cancellation interrupts preparation without publishing; never publish a partially visited graph.

## Port paths: precise unfinished decision

Native port output is driver-dependent. io.c1776/1840 vector-driver output uses erts_ioq_iodata_to_vec; erl_io_queue.c506 and erl_io_queue.h177 pin each binary, including binaries nested in valid iodata. Scalar driver flatten/copy paths are different; a blanket graph pin is not justified. io.c4303 is port_control's direct-bitstring path, not evidence that every port_command must pin.

Managed RuntimePorts.CommandCore validates options/port, then ErlangIoData.Flatten(data).ToArray, and schedules an owned frame. RuntimePorts.Send schedules command messages. Decide which native driver/capability this process-port implementation models before inserting a vector pin. For the vector path, use a dedicated iodata traversal at the same validated-vector boundary; the arbitrary Term graph walker would also visit invalid tuple/FUN payloads which native iodata rejects and could introduce wrong side effects on failing calls. Retain all original validation and busy/nosuspend timing. No speculative port hook is included.

## Additional copy domains to audit, not silently covered

Spawn argument/local-FUN environments, ETS/process-dictionary copy APIs, process_info returned terms, copied exit/monitor payloads and driver/NIF sharing beyond inspect need their own source-to-entry mapping. Native generic copy pins; do not infer that every managed argument passing or registry import is a native copy. Mailbox routing covers messages that actually use those entrypoints; it does not prove every signal path does. NIF InspectIolist currently flattens pending graphs separately; its original iolist/inspect pin order and error side effects need a specific traversal audit.

Suggested independent witnesses: construct a writable binary, collect numeric refsize before and immediately after ordinary self send, cross-process send, self alias, priority self, seqtrace self, Encode, EncodedSize, EncodeIoVec, nested FUN environment send, and NIF inspect (including partial-bit failure). Record before sending the observation report/ETF so that the observer does not obscure construction state. All expected capacities come from actual original captures. Negative controls must distinguish raw ByteSpan/byte_size/referenced_byte_size pure reads from true pin events.

Required own controls before acceptance: cursor single Step1 visits only one edge of huge tuple/map/env; deep100k Cons stack-safe; repeated aliases and nested FUN env pinned; original logical bits unchanged after pin; ordinary self-no-token retains capacity; cross-owner/seq/priority/alias shrinks eligible backing; dead target does not pin merely because Send was requested; malformed/invalid ETF option does not run the hook before options validation; no public message_queue_data support asserted. No execution was performed by this helper.
