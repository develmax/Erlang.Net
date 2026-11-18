# Part 1838 — Preserve proved minimum lengths through addition

The C# pair-result analysis derives the lower bound from
module-joined successful producer results and propagates the sum.
The first isolated candidate used stronger per-call assumptions
and lost the exact ELDAP match. The joined proof retains ELDAP
and every tested exact control.

The integrated eleven-module candidate took 22.49 seconds.
It retained five Equal and six Different with zero Unsupported,
SourceRejected or status changes. ERTS prepared both changed
images. Thirteen audited operands match OTP. Two normalized
functions gained equality and zero lost equality. Sixteen
permanent focused tests passed. The full regression and corpus
are deferred to the linked correction of the Part 1837 losses.

[Corpus](cohort.json) · [Normalized instructions](cohort-normalized.terms) ·
[ERTS](cohort-loader.log) · [Focused tests](focused.json) ·
[Isolated execution](isolated-release-after-v2.log).
