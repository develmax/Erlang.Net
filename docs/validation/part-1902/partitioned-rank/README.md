# Part 1902 full opcode and function rank

All 954 differing images in the uncached 1,366-image run were disassembled. The pinned Windows runtime processed 28 sequential partitions, each with at most 35 images, in 617.656 seconds. Every partition succeeded. The assembler evaluator is identified by SHA-256 in the receipt. The cache contains reference disassembly only.

| Priority | Observation after function and local-label normalization | Modules | Functions | Pinned OTP passes | Confidence |
| --- | --- | ---: | ---: | --- | --- |
| 1 | Plain allocations reserve more Y slots with identical Live | 821 | 7,414 | beam_ssa_pre_codegen and beam_ssa_codegen | High for the count and the point of divergence. Function-local ownership and later register rewrites still need separation. |
| 2 | Heap allocations reserve more Y slots with the same heap and Live | 324 | 747 | beam_ssa_pre_codegen and beam_ssa_codegen | High for the count. Requires the same stack proof as the plain allocations and separate heap validation. |
| 3 | List-comprehension helper MFAs absent in the candidate | 367 | 2,370 | v3_core and beam_core_to_ssa, followed by SSA reachability | High for the function-key count. Includes helper admission, ordinals and capture arity. |
| 4 | Reference begins an atom/value selection where the candidate begins a single equality test | 362 | 1,659 | beam_ssa_opt, beam_ssa_codegen and beam_jump | High for the opcode observation. Selection proof and physical ordering differ by source case. |
| 5 | Reference tuple projection occurs where candidate starts allocation | 484 | 2,033 | beam_ssa_pre_codegen and beam_ssa_codegen | High for the opcode observation. Projection placement and stack ownership overlap priority 1. |
| 6 | Debug forms differ only in the syntax-tools self-transform module | 1 | — | syntax_tools two-stage build and compiler abstract forms | High for the section count. The compiler still selects the bootstrap macro profile. |

The rows overlap. Do not add their module or function counts. The terms files contain the original per-module keys, instruction groups and detailed counts. The key summary records 267 same-name, different-arity observations in 75 modules across the generated-helper families.

This is diagnostic evidence. The full raw result for this source is 412 Equal, 954 Different, 0 Unsupported and 0 SourceRejected. That gate compares the complete original files. [Full comparison](../full-release/README.md).
