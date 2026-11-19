# Terminal list frames, bounded release inlining and tuple-entry list allocation

The exact isolated source applies three connected lowering changes.
It recognizes an exclusive terminal badmatch relay when placing a list
match frame, admits a conservative pure leaf under the release profile
inline option and keeps a proven tuple-entry list constructor in X
registers across a non-collecting nullary BIF.

The 81-path uncached selection reports 7 Equal, 74 Different, 0
Unsupported and 0 SourceRejected in 587.12 seconds. The immediate
Part 1865 parent has the same file statuses. All 17 byte-changing files
prepare in pinned ERTS. sys_messages gains complete LitT equality.
None of the previously equal sections lose equality. The comparison
has one normalized body gain and no normalized losses.

The actual megaco_tcp_accept:start_link/1 gains complete normalized
instruction equality. Both exact release and generated BEAMs exercise
the loopback API. The worker is linked, shuts down and closes the
listener, and malformed input retains function_clause.

Ninety-nine direct calls are removed across 12 of the 15 paired source
caller/callee signatures in the four inline profile witnesses. Seventy-eight
calls remain in the narrowed scope. Both earlier Equal inline-profile
controls keep their whole-file status.

The frame-distance audit finds 231 increases and two decreases.
The increases are concentrated in generated Yecc functions and
beam_ssa_type:sig_fun_call/8. Raw disassembly confirms wider
success frames. The post-placement branch slot pass refuses the
unchanged failure arm when a source Line lies between the X-only
relay and terminal badmatch. This is an integration regression
and remains open. The next correction preserves that metadata
while recognizing the existing frameless failure.

Initial C# build failures are preserved for a type name colliding
with an emitter member and stray quote tokens in a synthetic test.
The isolated corrections produce a clean build, 21/21 targeted
tests from a 9,817-case catalog and a sealed source. No whole
release or managed acceptance is claimed for this candidate.

[Scope](pilot.json) · [Exact source](source-freeze.json)
· [Focused cases](focused.json) · [Immediate parent delta](parent-comparison/pilot-summary.terms)
· [Frame increases](parent-comparison/farther-by-module.terms)
· [Inline calls](inline-edges.terms) · [Megaco behavior](megaco-native/1.terms).

