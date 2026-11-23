# Ranked remaining differences in Part 1853

The uncached Windows OTP 29.1.1 comparison leaves 959 Different images.
The pinned disassembler reads all 959 pairs. Every pair has an opcode
sequence difference in at least one function. The rows below overlap.

| Priority | Observed difference | Modules | Functions | Pinned compiler dependency and confidence |
| --- | --- | ---: | ---: | --- |
| 1 | Excess plain frame with equal live X prefix | 830 | 7,775 | High confidence in excess slot counts. SSA frame placement, value lifetimes and trim are the shared paths to test. |
| 1 | Excess heap frame with equal heap and live X prefix | 358 | 855 | High confidence in excess slots with identical roots. SSA interval placement and stack trimming precede final emission. |
| 1 | A move differs from Y initialization after different allocation | 682 | 3,323 | High confidence that the first differing instruction follows frame drift. Trace call-result saves and alias lifetime through the earlier pass. |
| 1 | Reference tuple projection reaches an earlier point than allocation | 490 | 2,119 | High confidence in the ordering difference. SSA sinking, frame placement and layout can contribute. Compare branch ownership before reordering. |
| 1 | Reference tuple or list test precedes the candidate frame | 442 / 402 | 1,071 / 2,376 | High confidence in the instruction order. Test clause selection, dominators and non-tail continuations on multiple shapes. |
| 2 | Type chunk differs | 871 | — | High confidence in the raw section count. Producer and join facts require per-operand proof. Fixed binary widths account for a separately paired subset. |
| 2 | Reference function is absent by name and arity | 453 | 4,085 | High confidence in the measured inventory. Entry liveness, generated helper identity or inlining can share a cause. Function-name pairing does not identify the earlier transformation. |
| 3 | Atom and import chunks differ | 923 / 897 | — | High confidence in raw section counts. Check changed instruction membership and reachability before index order. |
| 3 | Source line chunk differs | 925 | — | High confidence in the raw count. Branch and call scheduling determine encounter order. The safe BIF proof covers a verified subset. |
| 3 | Debug info chunk differs | 269 | — | High confidence in the raw count. Detect provenance separately from abstract-form differences. The current package closed 55 such sections. |

The early binary-binding change has a separate source-level witness in
three PER codecs. The initial real module loses 91 intermediate tuple
allocations while preserving the original binary failure subject. Keep
the fixed-width type work with this earlier allocation dependency. Use
the highest-count exact-root frame groups to select the next broad
lifetime case.

The opcode pass took 850.78 seconds. Its local disassembly cache uses
the image hash, ERTS and disassembler identities and the evaluator
hash. Its entries are separate from source compilation and its
results are diagnostic.

[Machine-readable groups](full-function-rank-summary.json) ·
[Complete function report](full-function-rank.terms.gz) ·
[Raw section counts](full-residual-sections.json) · [Opcode receipt](rank-receipt.json).
