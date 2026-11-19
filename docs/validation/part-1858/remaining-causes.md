# Remaining shared differences in Part 1858

The uncached candidate has 957 Different images. The cached reference
rank processed all 957 in 331.88 seconds with no unreadable images.
Every residual image has a different raw Code chunk and at least one
different opcode sequence. The groups overlap.

| Priority | Measured difference | Modules | Functions | OTP dependency and confidence |
| --- | --- | ---: | ---: | --- |
| 1 | Excess plain frame with the same X root count | 825 | 7,698 | Exact operands. High confidence in excess width; individual causes include SSA register allocation and value lifetimes, with earlier motion to be proved per subgroup. |
| 1 | Excess heap frame with the same heap and X roots | 342 | 816 | Exact operands. High confidence in excess width. Resolve ordinary-call lifetimes and constructor motion before reassigning slots. |
| 1 | Stack initialization after differently sized allocations | 680 | 3,316 | Exact first difference. High confidence that allocation is the upstream dependency; preserve exceptional edges when shortening the frame. |
| 1 | Tuple field projection precedes allocation in OTP | 485 | 2,011 | Exact first difference, down from 2,119. Pinned SSA codegen and branch-local allocation give a high-confidence direction. The proven arity-selection subgroup still requires focused testing. |
| 1 | Tuple or nonempty-list shape test precedes allocation in OTP | 442 / 404 | 1,077 / 2,391 | High confidence in opcode order. Medium confidence in a single cause. Arity selection and branch ownership can block earlier frame movement. |
| 1 | Previously matching outgoing tuple projections use an extra move | 2 | 2 | Raw listings identify the same call-preparation sequence in both functions. The retained-tuple producer path chooses the input register before assigning outgoing X operands. Restore the proven direct destination first. |
| 2 | Reference name/arity key absent | 450 | 4,080 | Exact key inventory. Among names containing the literal lc$^ marker, 2,782 keys are absent in 405 modules, down from 2,787 with the same rule. Core helper generation and SSA order both contribute. |
| 2 | OTP allocates where the candidate begins list extraction at the first mismatch | 393 | 1,316 | High confidence in the first mismatch, medium confidence in its origin. Inspect predecessor reachability, frame ownership and tested list shape. |
| 3 | Type or Line differs | 870 / 923 | — | Exact bytes. Improve SSA facts and physical line placement before comparing final serialization. |
| 3 | Atom or import tables differ | 923 / 894 | — | Exact bytes. Resolve helper keys and instruction operand changes before changing table order. |

Five ordinary conditional-list keys are restored, across three binary
MEGACO resolvers, megaco_sdp and tftp_lib. The previous checkpoint also
retains a separately classified narrower helper inventory, so the
comparison here uses the same literal name predicate on both raw ranks.
The raw rank identifies every module and function in the opcode groups.

[Rank output](rank.stdout.log) · [Function keys and operands](function-rank.terms.gz)
· [Rank receipt](rank-receipt.json) · [Raw sections](full-residual-sections.json)
· [List key delta](rank-lc-delta.terms).

