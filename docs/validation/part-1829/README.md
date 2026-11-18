# Part 1829 — Allocate leaf tuple fields in definition order

The leaf allocator recognizes the destination of `put_tuple2` and all
registers in its flattened element list. Tuple construction uses its
existing heap reservation. After ordinary code canonicalization, eligible
leaf frames can be removed using the same initialized X root proof.
The candidate colors tuple fields in first-definition order, corresponding
to the start ordering in the pinned OTP interval scan.

Pinned `beam_validator` reads the tuple elements before storing the
result and deducts words from the reserved heap. The implementation
reuses C# liveness, register coloring, and heap check placement.
No dependency was added.

The tuple-only candidate gained three normalized matches in a 50-module
sample. First-definition ordering gained four, with no sample losses.
The early placement's 397-second full run gained 66 matches but lost
one in `inet_parse`. The lost closure reads the list head and retains the
tail in X0 under pinned OTP. Applying tuple coloring earlier preempted
the old layout and moved that tail to X1. The final pass runs after the
existing transforms, preserving that layout. A permanent minimized
closure test catches the ordering defect alongside the positive
three-field conversion.

The final uncached comparison compiled all 1,366 Windows OTP images
in four isolated processes and took 367 seconds. It retained 400 Equal,
966 Different, zero Unsupported and zero SourceRejected. No byte
matches were gained or lost. ERTS batch preparation admitted 273 of
274 changed images. The `crypto` image loaded through the ordinary
on-load path and produced the same SHA-256 result as both controls.

The normalized comparison gained 45 functions and lost zero. The
frame-removal comparison reports 617 matches to frameless same-key
OTP functions and 22 removals without a same-key reference. The
remaining same-key framed `test_server` closure is numbered differently
in OTP. Its equivalent tuple-construction closure is frameless.

The focused regression passed. All four tuple fixture controls, three
lookup controls, and five public `mnesia_frag_hash` outcomes matched
pinned ERTS. This includes the old-state conversion helper, which now
also has matching normalized instructions. The Mnesia file still has
code, line, and type payload differences.

Arithmetic BIF register liveness is the next allocation group. The
pinned validator checks live registers and prunes X roots at a
collecting BIF. A pre-change scan of the prior differing images found
1,366 candidate functions across 342 modules when requiring a frame,
an exit-only GC BIF, and no ordinary call. Actual eligibility still
depends on the operator, frame, operands, and failure graph.

[Summary](results.json) · [Corpus](otp-corpus.json) ·
[Changed images](changed-images.json) · [Loader](loader.log) ·
[Normalized delta](normalized-delta.terms) · [Frame removals](frame-removals.terms) ·
[Variant comparison](subset-variants.terms) · [ERTS controls](deferred-module-execution.log).
