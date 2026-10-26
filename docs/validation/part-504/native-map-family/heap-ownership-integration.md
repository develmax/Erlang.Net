# Batch 504: physical ownership boundary

Pinned original: ad05823719d77c8faee87348ea39513d4e2f99c5.

## Implemented shared foundation

`ProcessOwnedWordArena504` owns actual native pointer-sized cells. Nonempty cells contain real GCHandle roots, not estimated allocation counters. A BEAM source invocation owns its native Y-frame allocations through `ProcessWordExecution504`; a logical process supplies its arena. Clone and trim create separate buffers while retaining the original Term, marker, binary context and handler identities. Deallocate, unwind and state disposal release cells. The execution scope retires orphan snapshots and waits for admitted asynchronous match operations before freeing their buffers. Both mailbox paths discard rejected BEAM selection snapshots deterministically.

This does not move the Part 502 logical exit/DOWN boundary or await a process from its own termination finally. No public heap information or guest heap option is enabled by this foundation.

## Source escape audit

The `BeamLambdaNames.Create` path materializes captured values in a separate `Term[]`; closures do not keep a reference to the disposed native frame cell. Returning a Term likewise retains the managed object independently. Trim preserves `BeamFrameIdentity` and copies the exact installed handler object. Receive clones have separate native storage, so disposal of the previous or failed snapshot cannot erase a successful snapshot. Source code/image ownership continues through the existing flow and execution leases.

## Four focused host proofs

1. Physical native cells begin zeroed; GCHandle slots keep an otherwise weak-only object alive; clearing and disposing release roots and memory.
2. Actual hard arena allocation admission is atomic; retirement waits on held leases and allows admitted owners to finish. This local arena limit is deliberately not guest max_heap_size policy.
3. Real BeamRegisterState Y allocations, cloning, trim and deallocation change actual lease capacity; escaped Term identity survives release.
4. Closing an execution scope waits for an admitted operation, refuses new operations and releases orphan frame leases only after the admitted operation completes.

Parent owns serialized builds and final affected regression, including original protected receive/trim, callback, deep-trampoline, cross-image closure identity and ordinary native loading. A passed prior binary is not evidence for later source edits.

## Work still needed for all resource contracts

- Own term payload graph allocations and their shared/binary domains; Y cells are roots, not an ERTS process heap substitute.
- Integrate generated C# lexical/local roots, mailbox contents, process dictionary, continuation/frame storage and native graph physical lifetimes.
- Add real per-process collection admission, root tracing, dead-object retirement and per-VM collection events. Native root release alone is not a garbage_collection event or reclaimed term-word count.
- Apply the prepared original-source heap size curve/default flags to actual allocation domains; distinguish disabled limits, kill=false, logging and shared-binary inclusion. Do not install unconditional quota failure for kill=false.
- Bind process_info scalar/list/default queries and proc_lib crash reports to a complete atomic resource provider.
- Preserve proc_lib physical hibernation and remote/logging effects; current source behavior must be checked as complete contracts.

The 24 baseline Known differences stay open until the shared root causes and their whole contract evidence are complete. The matrix must not replace Known with Pending just because a fixture passes or an option parses.

## Concrete next integration order

1. Define an ownership/backing interface in Erlang.Terms (the dependency direction is Runtime.Core -> Terms; Terms already grants Runtime friendship). Core must provide the owner implementation. Preserve ordinary standalone .NET Term construction outside a logical VM.
2. Start with the tuple element vector and list head/tail backing, using real owner storage instead of an additional shadow allocation. Root transfer must distinguish a process-owned value from a host export, closure capture and message copy. Existing public TupleTerm.Items and Term equality must keep working after a source invocation returns.
3. Do not attach permanent GCHandles to every child Term without a collector: an escaped closure can point back to a tuple, making a GCHandle-rooted cycle permanently live. Native term references need stable owner identities and graph tracing from real roots, with managed wrappers participating in liveness correctly.
4. Integrate all term producer paths (BEAM instruction constructors, compiler generated expressions, BIF/NIF construction and external import) before enabling heap max limits. Activating only the tuple constructor creates bypasses through maps, binaries, lists and host injection.
5. Extend root discovery to current Y slots, X/float register representation where relevant, continuation state, native callbacks and exported/host-held values. Dictionary is currently a public concrete Dictionary<Term,Term>; track its live roots at collection without pretending all mutation goes through BIF wrappers.
6. Mailbox Entry currently owns a managed Term directly. Message copy, priority insertion, deferred admission, distribution traces, receive selection and removal need one ownership handoff. Reuse the existing precedence/rescan semantics; release a queued value only after the selected consumer holds its own actual root.
7. Implement collection over that graph, bound per-logical-VM event/reclaimed-word statistics to actual collection, then enable heap defaults/limits and physical process_info as one shared batch. Source prepared policy is validation/rounding only until this stage.

Each stage requires escape, cycle, cross-process copy, cancellation and native physical-lifetime tests before public resource contract promotion. The current original protected receive replay/held packet extends real-source execution evidence; it is not term-heap collection evidence.

## Second concrete substrate increment

The new `ProcessOwnedTermHeap504` now implements actual native binary bytes and native child-identity vectors, atomic root leases, root tracing and unreachable/cyclic graph collection. No permanent GCHandles are attached to child graphs. A process context has a separate private instance, but guest Term constructors, process cleanup and public APIs do not use it yet. Therefore its private reclaimed byte/node results are not guest GC statistics, and its native capacity is not guest heap_size.

Host checks cover payload independence from caller arrays, native graph cycles retained by roots then reclaimed after roots leave, invalid cross-owner/dangling edges, duplicate root leases, and cancellation of a retirement waiter while the actual live payload remains readable until its last root releases. Parent owns build/execution evidence for these checks.
