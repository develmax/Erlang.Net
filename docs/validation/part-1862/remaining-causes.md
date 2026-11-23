# Ranked remaining Windows OTP differences after Part 1862

The full uncached comparison has 957 different files. All 957 pairs
have at least one opcode difference. Overlap is expected. Module and
function memberships and exact first differences are in the compressed
[full report](function-rank.terms.gz) and the [representative paths](rank-examples.terms).

| Priority | Observed group | Modules | Functions | Pinned pass and confidence |
| --- | --- | ---: | ---: | --- |
| 1 | Excess plain frame with unchanged live X prefix | 826 | 7,672 | High confidence in slot excess. Compare beam_ssa_pre_codegen lifetimes, register allocation and beam_trim ownership before treating each slot as redundant. |
| 1 | Excess heap frame with unchanged allocation and live prefix | 342 | 815 | High confidence in excess frames. Trace the same earlier value lifetime and trimming dependency. |
| 1 | A move differs from Y initialization after a different allocation | 680 | 3,311 | High confidence in the paired instruction. Trace saved call results and aliases before changing slot initialization. |
| 1 | Tuple projection appears before the candidate frame allocation | 484 | 2,005 | High confidence in scheduling drift. Compare beam_ssa_opt sinking with pre-codegen branch ownership. |
| 1 | Tuple or nonempty list test appears before the candidate frame | 442 / 404 | 1,077 / 2,390 | High confidence in observed order. Distinguish dominance, rooted prefixes and frame demand across continuations. |
| 2 | Reference MFA or generated function key is absent | 447 | 4,053 | High confidence in the inventory. Core rewriting, helper identity and inlining are competing causes. |
| 2 | Value selection differs from an equality test | 362 | 1,654 | High confidence in opcode pairs. Inspect Core clause grouping and beam_ssa_codegen layout before dispatch lowering. |
| 2 | Type chunk differs | 869 | — | High confidence in counts. Use beam_ssa_type producer and join evidence for each operand. One complete Type chunk gains equality. |
| 3 | Atom and import chunks differ | 923 / 894 | — | High confidence in counts. Resolve different instruction membership and reachability before changing beam_asm table order. |
| 3 | Line chunk differs | 923 | — | High confidence in counts. Verify source annotation order through passes before changing the line codec. |
| 3 | Debug info chunk differs | 269 | — | High confidence in counts. Separate profile or abstract form differences from beam_asm down-stream metadata. |

Prioritize the measured constrained keyfind regression before extending
earlier register ownership and frame scheduling over larger shapes.
[Raw section counts and module memberships](full-residual-sections.json).

