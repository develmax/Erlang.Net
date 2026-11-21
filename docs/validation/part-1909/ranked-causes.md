# Residual Windows OTP 29.1.1 differences

Counts use unique module paths and function keys from the pinned opcode evaluator. A function can occur in several rows. Frame size and scheduling take precedence because downstream register and table order can follow from them.

| Family | Modules | Functions | First pass to compare in pinned OTP | Confidence and dependency |
| --- | ---: | ---: | --- | --- |
| Y-slot liveness and frame width after a shared instruction prefix | 830 | 8104 | beam_ssa_pre_codegen:find_yregs and register allocation, then beam_trim | High for first differing width. Medium for the individual lifetime cause. |
| Pure projections and entry checks preceding allocation | 782 | 7019 | beam_ssa_pre_codegen:place_frames and beam_ssa_codegen | High for the representative checked-list and record cases. Medium for the combined group. |
| Argument moves, swaps and Y-slot initialization | 555 | 1677 | beam_ssa_pre_codegen, beam_ssa_codegen and beam_trim | High for the observed order. Apply after frame ownership. |
| Missing reference functions | 411 | 3653 | v3_core helper creation and beam_core_to_ssa traversal | High for missing keys. Medium for missing code versus renamed helpers until source proof. |
| Select-value dispatch versus one exact equality test | 359 | 1626 | beam_core_to_ssa constructor partition and beam_ssa_codegen | Medium. Some cases depend on prior guard and type refinement. |

| Raw section | Different images | Diagnostic dependency |
| --- | ---: | --- |
| Code | 940 | Function and instruction emission. |
| ExpT | 929 | Function labels and export ordering. |
| Attr | 915 | Module attributes include the derived version identity. |
| LocT | 915 | Private functions and entry labels. |
| AtU8 | 913 | Atom interning follows generated code and metadata order. |
| Line | 899 | First use and numbering depend on control flow and the source build profile. |
| ImpT | 884 | Import order follows emitted calls. |
| Type | 849 | Type records follow SSA facts and typed operand first use. |
| FunT | 696 | Anonymous helper topology and literal call sites. |
| LitT | 696 | Literal selection and insertion order. |
| StrT | 46 | Literal bit syntax segment emission. |
| Dbgi | 1 | The remaining merl_transform image depends on the pinned two-stage self-bootstrap. |

[Per-function counts](function-rank.terms) · [Per-image section details](full-uncached/residual-sections.json) · [Raw acceptance](full-uncached/otp-corpus.json).
