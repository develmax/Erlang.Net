# Part 1848 — Remove frames around non-collecting guard BIFs

The leaf register allocator now treats guarded non-collecting BIFs as
conditional writers. Each proof retains the failure target and carries
the incoming register state along it. Only the success edge writes the
result. The existing initialized-prefix analysis still verifies each
heap checkpoint. This rule requires an explicit local failure branch.
Ordinary calls and special instructions that clobber X retain their
frame requirements.

Pinned `beam_ssa:clobbers_xregs/1` and
`beam_ssa_pre_codegen:need_frame/1` establish where frames are required.
`beam_validator` models the destination along the successful BIF edge.
The C# implementation reuses leaf liveness and the existing allocation
proof. No runtime export, dependency, or compiler option was added.

The saved-output inventory found 287 candidate functions in 140
different release modules. Each inspected function has a labeled
non-collecting BIF, an allocated candidate frame and a frameless
reference. This is a candidate inventory. A linked source recompile
will measure the actual coverage, instruction and section changes.

A permanent exact Erlang fixture covers literal and dynamic tuple
indices, preserved values on both branches, a normal remote tail
fallback and an existing tuple arity control. The test fails on the
frozen Part 1847 compiler and passes on this frozen candidate. The
fixture comparison produces byte-identical files. Pinned ERTS agrees
on twelve execution outcomes, including failure at a bad tuple or an
invalid index. All 28 integrated focused cases pass. The focused
runner does not regenerate readiness.

[Source and compiler identities](source-freeze.json) ·
[Function inventory](frame-rank.terms) · [Inventory evaluator](rank-guarded-native-frames.eval) ·
[Positive tests](focused.json) · [Expected pre-fix failure](red.log) ·
[ERTS observations](candidate-post-execution.log) · [Pinned OTP observations](beam_guarded_non_gc_bif-execution.log).
