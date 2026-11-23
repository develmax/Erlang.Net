# Remaining compiler differences on the complete Part 1875 corpus

This ranking covers 956 differing images and all readable disassemblies from the
fresh uncached 1,366-image comparison. Counts overlap. A first opcode mismatch
is a witness, and the phase column identifies the pinned phase to audit. The
function and module lists are available in the complete rank.

| Priority | Difference and reach | Pinned OTP phase and dependencies | Cause confidence |
| --- | --- | --- | --- |
| 1 | Plain allocation has excess Y slots with the same X root count in 7,718 functions across 826 modules. The combined heap/frame case with the same heap and X operands covers 808 functions in 341 modules. Initial move versus stack initialization affects 3,280 functions in 681 modules. | `beam_ssa_pre_codegen` places frames and calculates Y liveness and intervals. `beam_trim` and code generation then affect final slot order. Frame decisions precede dependent move and initialization differences. | High for widths and ordering in proven subgroups such as send and temporary copies. The full set has multiple causes. |
| 2 | Reference has 4,023 missing function keys across 445 modules. List-comprehension names account for 2,725 missing keys across 399 modules. Across all forms, 282 missing keys in 84 modules have an actual key with the same name and a different arity. The actual image has 1,832 extra function keys across 261 modules. | `v3_core` lifts generator bodies. `beam_ssa_opt` handles inlining and cleanup. Compare ownership and helper numbering before instruction alignment. | High for the measured generator forms. Other missing functions need classification by source transformation. |
| 3 | Tuple projection on the reference side versus allocation in the actual image is the first mismatch in 2,004 functions across 484 modules. A reference tuple test versus allocation covers 1,077 functions across 442 modules. | `beam_ssa_opt` value and type propagation, followed by `beam_ssa_pre_codegen` frame placement. Several forms overlap with the first group. | Medium for projection or frame placement as a family. Witnesses alone do not identify the exact transformation. |
| 4 | Type differs in 868 images. Function counts are pending decoding and site attribution. | `beam_ssa_type`, range propagation and `beam_asm` table construction. Compare normalized use sites before changing serialization or inference. | Low for an aggregate cause. This count includes instruction-derived changes to type table order. |
| 5 | Line differs in 923 images. Register and file marker work also affects instruction sequence. | Frontend source spans and optimizer line retention precede `beam_asm` line indexing. Diagnose each source location against a stable instruction map. | Low as one cause. Generated file transitions and executable line placement require separate evidence. |
| 6 | Dbgi differs in 101 images. File and include provenance is an independently testable subset. | `epp` directives, explicit profile and compiler annotations precede debug serialization. | High for the verified include provenance cohorts. Other residuals require normalized debug-form comparison. |
| 7 | AtU8 differs in 923 images, ImpT in 894, ExpT in 944 and LocT in 928. | Function generation, lowering and assembly order feed the atom, import, export and local tables. Investigate isolated ordering only after matching owner schedules. | Low for direct table ordering as the cause across the whole group. |

[Complete function rank](function-rank.terms) ·
[Counts](compact-function-rank.terms) ·
[Missing and extra key families](function-key-rank.terms) ·
[Section membership](residual-sections.json) ·
[Rank execution](rank-receipt.json).

