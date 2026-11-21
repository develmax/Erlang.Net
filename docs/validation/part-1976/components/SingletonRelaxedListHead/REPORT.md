# List-entry frames and a relaxed singleton generator

This audit uses the complete uncached Part 1973 Windows release corpus and compiler F685472E21EDDB969DC244FA620539FF5D145C8D267B0F8E9486A7F30DAD2476. Raw corpus acceptance was 509 Equal, 857 Different, 0 Unsupported and 0 SourceRejected. Pinned OTP is 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5.

| Observed prefix or narrower dependency | Functions | Modules |
| --- | ---: | ---: |
| First differing nonempty-list test versus allocation | 2048 | 402 |
| Tail-only projection with no pinned frame | 267 | 17 |
| One frame on each side, managed list and both projections in saved Y slots | 171 | 106 |
| One frame on each side, managed list in X and both projected values in Y | 149 | 92 |
| Same selector and get_list in both, followed by a pinned second nonempty-list check | 75 | Up to 51 across both source-register groups |
| Shared managed failure and split pinned failure on the same nested head check | 37 | 18 |
| Lifted list-comprehension helpers in that split | 30 | 15 |
| Pinned exact one-cell head among those helpers | 10 | 7 |
| Other raw nested projections within the same family | 20 | See head-kind.summary |
| Already exact Part 1973 images matching the narrowed physical prefix | 0 | 0 of 509 images |

The groups overlap where noted. The largest syntactically connected one-frame cohort needs more than earlier allocation placement. In the selected relaxed helpers, candidate code enters one fallback with both non-cons input and head rejection. It then rechecks the saved list and reads its tail again. The pinned body has a direct skip tail call only when a cons was already proved. Original non-cons data goes to the nil or bad_generator continuation. Nested shapes with multiple refinements, including string:reverse_1/2, need extra projected roots and independently ordered comparisons. asn1ct_imm helpers can allocate between the head check and nested projection. Those groups are distinct engineering work from merely moving the first allocation.

Pinned compiler evidence. v3_core:lc_tq1 at 1822-1870 creates separate successful, mismatch, tail and bad-generator clauses. beam_core_to_ssa:match_con and select_types at 1333-1381 partition cons and nil before nested matches. beam_ssa_pre_codegen:place_frames at 1433-1473 selects the block owning the continuation frame, and find_yregs at 1938-2042 retains only initialized values live across a call. The synthetic precodegen listing and exact mnesia_frag helper show the same separation. Outer GetList keeps the next list in X0. Rejected nested patterns use that X0 on the skip edge. Successful exact-singleton head tests can hold the candidate value in X through both pure projections and detect the inner nil tail before the recursive allocation. The original non-cons input stays untouched because its failure edge leaves before the first projection.

## Isolated draft

The C# source change stays in the verified Part 1973 StructuralListEntry file. It builds on existing branch and allocation planning rather than duplicating frame and heap emission. It admits source helpers or authored functions with the exact four-clause relaxed layout. The accepting and skipped heads both have a self continuation with their exact shared capture operands. The terminal nil and bad_generator clauses keep their original behaviors and line information. The accepting head is one anonymous singleton container with a new variable field. No alias or guard can observe the old nested selector after the projection. This proves reuse of the nested list register for its discarded inner tail. The final catch-all cons clause takes only that outer tail and the existing captures, so allocating the outer tail in X0 does not change the original input needed by the initial non-cons branch. Strict generators, guard-filtered cases, multi-element heads, noncanonical calls and differing terminal cases remain outside the added gate.

The proof gates rely on source and on the existing branch-specific heap and frame planner. No module names, example-specific arities, literal IDs, reference bytes or source positions choose the implementation. The ten release witnesses are a measured upper bound for this exact source admission. positive-release-modules.json preserves all seven actual module statuses and sections. Each currently has additional unrelated differences, and none is claimed as a whole-image gain.

The permanent test proves the two independent failure labels, X-only ordered list projections, a frame after the last pure test, and an unframed skip call for both one-argument and captured cases. The exact Part 1973 emits the earlier allocation before the first test in both synthetic cases. Pinned and frozen BEAMs pass all nine representative native values and reason tests, including a malformed inner list being skipped, improper outer input raising bad_generator, strict mismatch raising badmatch, a two-element control, and a guard filter. The relaxed improper-list stack and line agree. The frozen strict fixture attributes its badmatch to strict/1 while pinned reports the lifted helper at the same line. Strict mode is outside this draft.

Both patches strictly apply to the documented exact preimages. The source files passed syntax parsing. The candidate compiler has not been rebuilt, so candidate structural green, native replay and raw function/module measurements remain pending.
