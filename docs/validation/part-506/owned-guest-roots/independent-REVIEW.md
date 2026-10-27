# Independent part506 ownership review

Read-only source review. No build, VM, original fixture, git mutation, CI or active
source change. Findings are static source paths, not reproduced failures. The
proposal owner is fixing staged files concurrently; this is a per-file hash
snapshot, not an atomically frozen or buildable package. See snapshot-manifest.json.
Zero public API contracts are promoted.

## 1. Dictionary descriptor callbacks must leave its gate

The initial production-slice.patch retained here contains the indexer getter
holding the dictionary gate while exporting (line884), and a gated snapshot
export through the same path. The reviewed initial source similarly invokes
ProcessTermHeap506.Export -> detached copy -> ObserveIncomingTerm while holding
the dictionary gate.

This is a concrete conditional lock cycle for a dictionary external PID, port
or reference. Detached copying creates a new unobserved wrapper. The active
RuntimeKnownNodes.cs external-discovery path calls resolveDistributionEntry
after leaving its own gate; ProcessRuntime constructor wires that resolver to
TryGetDistributionEntryIdentity, whose runtime gate is visible in
ProcessRuntime.DistributionEntries.cs:9-17. Meanwhile staged ProcessRuntime.cs
holds the runtime gate in Info while reading/enumerating the dictionary at
lines842-843. One thread can hold dictionary -> wait runtime, while Info holds
runtime -> waits dictionary. It is not correct to claim every scalar export
takes the runtime gate; the external identity supplies the concrete path.

The proposal owner accepted this finding and the dictionary version captured
later in snapshot already uses Borrow under its gate and PrepareBorrow outside.
The source update is promising but the retained initial patch is stale.
Regenerate one coherent patch/manifest after fixes, and prove the lock ordering
with an external-identity dictionary entry and controlled runtime/descriptor
barriers. Keep all descriptor factories and external discovery outside both
dictionary and heap gates, including setter preparation and whole snapshots.

## 2. Every continuation carrier needs a root before frame unwind

Snapshot Execution.cs:107 and :268 transfer only private TailCall and
ErlangException before disposing the owned scope. The same file emits
SourceTailCall at869/906, RuntimeTailCall at914, and FunctionTailCall at986.
Unchanged Execution.InvocationLoop.cs catches these carriers at57/73/85,
outside the owned frame wrapper. Source image acquisition can await between
successive targets. Arguments and callable environments held only in the CLR
exception object are not registered guest roots after Frame.Dispose.

A collection during that handoff can therefore clear cells before the next
callee publishes its arguments. CLR strong references do not satisfy this
collector's explicit-root contract. Protect every carrier's arguments, callable
environment and target metadata through a trampoline-owned frame before the
callee frame leaves. Include SourceFunctionTransfer target environments when
that carrier is used. The proposal owner accepted this fix and is staging it;
the snapshot is before that continuation fix.

Use finite controlled handoff tests for source->source, source->runtime/native,
and function-tail paths: pause between target selection and the next admission,
collect, resume, and verify whole values and closure invocation. Test cancellation
and error transfer while that destination root is held as one batch.

## 3. Part505 physical map state must not be normalized by export

OwnedTermDetachedCopy506.cs:59-61 reconstructs every MapTerm with the ordinary
constructor from logical pairs. That constructor deduplicates and sorts entries
(active MapTerm.cs:34 onward), and does not carry nativeLookup505 or
NativeEquality505. Active MapTerm.Native504.cs explicitly preserves physical
cardinality and native lookup/equality callbacks for part505 maps; TryGet in
MapTerm.cs:94 uses that saved native path.

Thus even though the pilot accounts only tuple/list slabs, merely exporting a
portable map subgraph can silently change map size, lookup, exact equality or
nested-key behavior after key-buffer mutation. This must depend on the final
reviewed part505 detached-copy representation, or explicitly reject that native
physical map class in the selected pilot until the representation can be copied
faithfully. Rebuilding a normalized dictionary is not an equivalent copy. The
proposal owner has been notified; no copying policy has been edited here.

## 4. Owned retirement is not yet a runtime physical join

ProcessContext.OwnedTermPilot506.cs:30-36 starts heap.RetireAsync and stores
OwnedTermRetirement506. Snapshot ProcessRuntime.cs:346-347 then signals Unwound.
The staged tree has no consumer of OwnedTermRetirement506. The existing shutdown
drain lists (around1768/1870 in staged ProcessRuntime.cs) likewise omit it.

This matters already for the captured host-task profile, not just future native
providers. Public calls on a still-live context may admit an independent owned
frame off the actor, then await a finite host callback. Cancelling the idle actor
can close its body while that separately admitted frame still exists. Retirement
waits for the frame, but runtime disposal does not currently join that task.
The current AwaitAndClosure test uses a callback explicitly respecting process
cancellation; it does not prove independent uncooperative finite callback drain.

Either register owned heaps with physical runtime/halt retirement, or constrain
external invocation admission/lifetimes so such frames cannot outlive the joined
owner. Do not move this wait before logical DOWN or make the actor's finally
await its own Unwound task. A controlled callback with an explicit release gate
should prove shutdown waits for actual root retirement; release it in finally.
This is a static lifetime gap, not an executed leak claim.

## Safe integration order

1. Keep the pilot disabled by default and preserve all24 Known classifications.
   Finish dictionary callback splitting and all continuation carriers first.
2. Freeze final part505 map semantics; bind faithful map copying or narrow the
   pilot explicitly. Regenerate the whole staged patch/manifest against final
   active source hashes. The currently mixed snapshots are not an apply target.
3. Add owned heap admission closure and physical retirement join without changing
   logical exit/DOWN ordering; include detached overlapping host calls.
4. Build the coherent package serially. Run finite payload, dictionary/export
   lock-order, every tail carrier, closures, overlapping await/cancel and actual
   retirement tests together. No source test results are claimed in this review.
5. Run affected ordinary source/BEAM/process/native regressions on the final
   sources; compare ordinary and pilot original whole values/stacks. Measure the
   default fast path before enabling the pilot more broadly.
6. Complete BEAM/queue/native/ETS/persistent roots and genuine allocation policy
   before replacing public GC/statistics or claiming heap/resource compatibility.

The borrow-then-prepare split and atomic tuple/list payload publication are useful
foundations. This review does not equate managed slabs with complete ERTS heap
footprints, generations, binary-vheap accounting or allocator behavior.
