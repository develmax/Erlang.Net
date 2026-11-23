# Ranked remaining causes in Part 1855

The full comparison has 958 Different images. The latest diagnostic
disassembly took 410.64 seconds with reusable reference entries.
All remaining files have a raw Code section difference. The rows
overlap and follow the earliest shared dependency where established.

| Priority | Difference | Modules | Functions | Evidence and next dependency |
| --- | --- | ---: | ---: | --- |
| 1 | Excess frame width with the same X root count | 829 | 7,707 | Exact allocation operands. Study live value intervals, branch-owned continuations and frame placement. The previous count was 7,775. |
| 1 | Excess heap frame width with the same heap and X root operands | 345 | 824 | Exact allocation operands. Test call and pure-constructor scheduling before changing heap or slot counts. The previous count was 855. |
| 1 | Stack initialization follows differently sized allocations | 681 | 3,319 | The first opcode difference is downstream of frame width. Start with the cause of the extra live value. |
| 1 | OTP projects a tuple field before the candidate allocation | 490 | 2,119 | Exact first-opcode difference. Leading shape checks and distinct successor call sites narrow the source-derived motion. |
| 1 | OTP checks a tuple or nonempty list before the candidate allocation | 442 / 402 | 1,071 / 2,376 | Exact first-opcode difference. Small returned constructors distributed across case predecessors can eliminate the common frame. |
| 2 | Lifted or source function name and arity absent | 453 | 4,085 | Name and arity inventory. It includes 2,739 list-comprehension helper keys. Source and SSA counters differ. Early SSA and lexical identity must be paired before renaming. |
| 2 | Reference receive marker signature absent | 94 | 208 | Reference emits markers in 210 functions across 95 modules. Observer accounts for the two corresponding C# functions. The fresh-reference same-function subset precedes the local-call propagation needed by instrument. |
| 3 | Type or source line chunks differ | 870 / 923 | — | Exact chunk comparison. Range inference, node scheduling and metadata origin can contribute. |
| 3 | Atom or import table chunks differ | 923 / 897 | — | Exact chunk comparison. Check live instruction operands and helper identity before encounter order. |

Both frame-distance outliers use one candidate slot and two
reference slots. In instrument the reference is retained for
recv_marker_clear. In tprof OTP keeps the tuple operands through the
list subtraction and constructs the pure cons head after the call.
The latter source-order rule has a verified isolated successor.

[Raw rank](rank.stdout.log) · [Per-function rank](function-rank.terms.gz) ·
[Rank receipt](rank-receipt.json) · [Raw section groups](full-residual-sections.json) ·
[Frame comparison](frame-distance/).
