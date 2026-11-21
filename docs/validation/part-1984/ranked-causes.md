# Residual Windows OTP 29.1.1 differences

Counts use unique module paths and function keys from the pinned opcode evaluator. A function can occur in several rows. Frame size and scheduling take precedence because downstream register and table order can follow from them.

| Family | Modules | Functions | First pass to compare in pinned OTP | Confidence and dependency |
| --- | ---: | ---: | --- | --- |
| Y-slot liveness and frame width after a shared instruction prefix | 773 | 7261 | beam_ssa_pre_codegen:find_yregs and register allocation, then beam_trim | High for first differing width. Medium for the individual lifetime cause. |
| Pure projections and entry checks preceding allocation | 731 | 6322 | beam_ssa_pre_codegen:place_frames and beam_ssa_codegen | High for the representative checked-list and record cases. Medium for the combined group. |
| Argument moves, swaps and Y-slot initialization | 509 | 1475 | beam_ssa_pre_codegen, beam_ssa_codegen and beam_trim | High for the observed order. Apply after frame ownership. |
| Missing reference functions | 358 | 3092 | v3_core helper creation and beam_core_to_ssa traversal | High for missing keys. Medium for missing code versus renamed helpers until source proof. |
| Select-value dispatch versus one exact equality test | 304 | 580 | beam_core_to_ssa constructor partition and beam_ssa_codegen | Medium. Some cases depend on prior guard and type refinement. |

| Raw section | Different images | Diagnostic dependency |
| --- | ---: | --- |
| Code | 854 | Function and instruction emission. |
| LocT | 846 | Private functions and entry labels. |
| AtU8 | 843 | Atom interning follows generated code and metadata order. |
| ExpT | 843 | Function labels and export ordering. |
| Line | 834 | First use and numbering depend on control flow and the source build profile. |
| Attr | 830 | Module attributes include the derived version identity. |
| ImpT | 818 | Import order follows emitted calls. |
| Type | 798 | Type records follow SSA facts and typed operand first use. |
| FunT | 653 | Anonymous helper topology and literal call sites. |
| LitT | 649 | Literal selection and insertion order. |
| StrT | 28 | Literal bit syntax segment emission. |
| Dbgi | 1 | The remaining merl_transform image depends on the pinned two-stage self-bootstrap. |

[Per-function counts](function-rank.terms) · [Per-image section details](full-uncached/residual-sections.json) · [Raw acceptance](full-uncached/otp-corpus.json).
