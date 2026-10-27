# Independent current-code review: Part514 linear construction fix

Reviewed actual w504 production files and aggregated three-size test source, with hashes in snapshot.json. No build/runtime/native/CI/git mutation was performed. No new concrete blocker was found in the bounded custody/publication review. Actual red/green evidence belongs to the parent; this review does not substitute source reasoning for those runs.

## Custody and backing coverage

BeginConstruction514 validates and publishes the complete input into one actual binary-backed frame slot before constructing its reference-identity admission set. On constructor failure it clears that slot. Both initial admission and binary registry traversal use NativeMapOwnedRoots510.Children, covering canonical/arithmetic rows, hidden physical entries, FlatKeys and HAMT leaf/StoredTerm descendants, rather than only canonical map lookup entries. No comparison/hash provider or runtime callback is invoked by that traversal.

The actual adopter builds only tuple/cons containers. It reuses source binary/map/function leaves and already-owned descendants. Each new container is published under the heap gate only after every direct child is in the input/admitted-node reference set; custody identity must match the exact construction frame. Its logical guest-cell root avoids a repeated transitive binary lease because the input slot already owns every possible backing. An unrelated fresh binary direct child is rejected before node/root admission. The final destination PublishRoot512 creates its complete binary lease before construction frame disposal. Disposing the temporary transaction can then release its one input lease without losing the destination lease.

Source maps/layouts are reused without mutation; this optimization neither reconstructs the map nor canonicalizes hidden physical occurrences. Shared aliases and duplicate physical backing references remain traceable. The five-file change adds no source mutation or external admission callback.

## Publication failure and authority

New-node publication records the node in custody and then the frame dictionary; a dictionary failure removes the newly admitted node. The old destination remains intact until final PublishRoot512. That existing helper stages the complete next lease and restores its previous lease if destination dictionary publication throws. Construction frame disposal releases every intermediate root/input lease. Unpublished allocated nodes remain ordinary unreachable owned payload eligible for collection, rather than falsely claiming immediate deallocation.

The custody object is internal and has no host/delegate exposure. Production search finds BeginConstruction514 only in AdoptGuestInto507 and custody.Publish only in AdoptGuest507. Both run inside the same heap-gated synchronous transaction. An arbitrary reflective caller mutating/clearing a construction frame between these steps is outside that actual path; this review found no reachable internal caller doing so. The existing rejection test deliberately proves a new backing cannot be smuggled through normal publication.

## Complexity and tests

There are a constant number of complete graph walks (validation, input lease, admission inventory, final destination lease). Admission uses HashSet<Term> with ReferenceEqualityComparer, so Contains/Admit do not recursively compare terms; each constructed node checks only direct children and is published once by the memoized adopter. Thus the total work is linear in graph nodes plus edges, including wide tuple inputs and physical map children. Logical intermediate roots remain O(nodes), with two complete backing sets transiently retained at final publication. Diagnostic registry counters alone do not count the admission inventory, but source inspection confirms no hidden per-suffix walk there.

Scaling test currently records all N16/32/64 observations before aggregate failure, preserves its original bounds, checks actual backing counts/aliases and release-to-zero. HiddenPhysicalMap covers a backing absent from canonical entries. RejectAndRollback checks unadmitted new backing rejection and original destination/root preservation. No success or bounds change is inferred from absent pseudocounters; the parent has real instrumented red evidence and must retain final actual green output on these exact sources.
