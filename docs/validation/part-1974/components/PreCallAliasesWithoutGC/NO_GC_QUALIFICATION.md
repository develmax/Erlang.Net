# Nonallocating pre-call frame alias draft

This production draft limits the earlier def-use audit to proven intervals with no GC or implicit register clobber. It is based on the frozen P1973 shadow with the read-only late decoder. Separate compiler and test patches pass strict application together. All changed-file preimages and digests are recorded in their receipts.

- [Production patch](nonallocating-precall-aliases.patch)
- [Permanent test patch](nonallocating-precall-aliases-test.patch)
- [Production receipt](source-draft-receipt.json)
- [Test receipt](test-draft-receipt.json)

The planner first establishes an exclusive ordinary call and a single disjoint frame owner. It requires evidence that a different Y value already outstanding on entry to the call is read in the initial post-call block. An erased alias comes from an untyped X-to-Y copy and has no other writes or uses after the call. Each operand role comes from a small whitelist. Every read between the definition and last use sees the unchanged X. Interior labels, GC checks, protected paths, debugger metadata, trimmed frames and unknown clobbers decline the rewrite. The actual operand edit preserves existing type annotations. Reference bytes are used only by the separate diagnostics.

A late safepoint can follow the removed last use. The alias edit therefore publishes only together with compaction of the selected entry frames. After the edit, the reader recomputes the instruction and read-only offset views. The existing disjoint frame compactor recalculates the width, Y operands, initialization lists and deallocation sizes for those entries. No other function enters that compaction invocation.

The independent Part1971 no-GC outer screen has 26 candidate sites in 24 functions across 18 modules, after excluding every function handled by the prior cleared-copy group. A deliberately stricter local-only post-call check confirms another literal live Y in nine functions across eight modules. The implementation's predecessor-aware retained-root proof can admit more from the outer screen. These figures describe immutable instruction observations. [Scope plan](no-gc-scope-plan.txt) includes all 18 different modules and three exact controls. The separate broader one-GC plan in REPORT.md has no implementation in this draft.

The permanent test is compiler/beam/removes-precall-frame-aliases-without-crossing-gc. Positive structural inputs assert an direct atom guard with an actual later Y read, both plain and typed replacement operands, two failure/success exits and a physically narrower frame. Negative inputs assert an unchanged Y used after the call, a heap reservation, an X overwrite, a Y initialization between definition and use, a second entry into the old interval, and debugger metadata. The private-pass tests use the repository's existing synthetic BEAM byte tests. The late pass is registered at the regular EmitCode entry before final label ordering.

Candidate compilation, regression execution, style, current native loading and new byte counts await an isolated SDK lane. The static proof and artifact receipts are runnable locally without GitHub.
