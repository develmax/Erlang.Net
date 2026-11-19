# Remaining shared differences in Part 1860

The current full Windows OTP 29.1.1 comparison has 957 Different
images. All have a raw Code difference. The cached disassembly rank
runs in 163.16 seconds versus 331.88 seconds on Part 1858. Part 1860
changes 82 images relative to that checkpoint, allowing more reused
generated disassemblies. The original compiler inputs and raw
compiled comparisons did not use compiled result caching.

| Priority | Difference | Modules | Functions | Upstream cause and evidence |
| --- | --- | ---: | ---: | --- |
| 1 | Excess frame width with equal X roots | 825 | 7,692 | Exact operand comparison; down six functions. Pre-call live-through high Y prevents the existing pass from considering lower projections. The source census gives 29 paired same-X witnesses in 24 modules for the next scoped fix. |
| 1 | Excess heap frame width with equal allocation words and X roots | 342 | 815 | Exact operand comparison. Preserve constructor scheduling and physical frame ownership. |
| 1 | Initialization after differently sized allocations | 680 | 3,310 | Exact first mismatch. Start with the frame lifetime rather than independently changing init_yregs. |
| 1 | Tuple field projection before allocation in OTP | 485 | 2,011 | Exact first mismatch. Five release modules prove shared terminal frame cleanup blocks the existing whole-function ownership gate. A selected-path retake has ten static arm witnesses. |
| 1 | Tuple or nonempty-list check before allocation in OTP | 442 / 404 | 1,077 / 2,390 | Observed opcode order. Frame ownership and shape proof are earlier dependencies. Candidate coverage must be measured at individual edges. |
| 2 | Reference name/arity key absent | 449 | 4,070 | Exact inventory. Ten restored list-helper keys reduce the literal lc$^ subset from 2,782 to 2,772 across 404 modules. Tagged patterns, lexical owners and helper ordinals remain. |
| 2 | OTP allocates where the candidate begins list extraction | 389 | 1,307 | Exact observation, down nine functions. Retained-head and field lifetimes explain a checked subset. |
| 3 | Type or Line differs | 870 / 923 | — | Raw sections. A separate 75-site proof for immediate lists:keyfind/3 consumers covers 74 functions in 39 modules. This is eligible typed-operand coverage and does not establish complete section equality. |
| 3 | Atom or import tables differ | 923 / 894 | — | Raw sections. Call and helper identity, plus later serialization order, contribute. |

Both Part 1858 normalized regressions are restored. The full changed
comparison gains 79 normalized function bodies and loses none. No
complete raw chunk or whole-file classification changes. Marker,
list and frame counts reflect different evidence scopes.

[Raw rank](rank.stdout.log) · [Per-function rank](function-rank.terms.gz)
· [Rank time](rank-receipt.json) · [Exact missing helper delta](rank-lc-delta.terms)
· [Residual chunks](full-residual-sections.json).

