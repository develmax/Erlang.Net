# A non-collecting head BIF inside a frameless parser leaf

The release census uses the uncached Part 1973 corpus and compiler f685472e21eddb969dc244fa620539ff5d145c8d267b0f8e9486a7f30dad2476. The new source draft and synthetic probe use the exact Part 1975 record-return retake compiler 3bce5198e9713d66ff83a88d0115b77675cf09ce87abed4d31956b50e028e13c and its matching source closure. The pinned source is Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5.

| Shape among 267 tail-only projected entries | Functions | Modules |
| --- | ---: | ---: |
| Generated Yecc leaf with one non-collecting body hd/1, local framed tail and no ordinary calls | 211 | 14 |
| Generated Yecc leaf with one hd/1 plus element/2 body BIFs | 53 | 1, included in the 14 |
| QLC leaf with exact equality BIF | 1 | 1 |
| ASN.1 leaf with GC BIF | 1 | 1 |
| Binary-size computation with an actual ordinary call | 1 | 1 |

The first row is an upper bound for the production gate. Other requirements in the existing leaf planner can reduce it. No source module names or generator names enter the C# matcher. The core_parse:yeccpars2_103/7 witness has an unchanged hd/1 BIF and local tail target on both images. Pinned uses X-only checked projections, an explicitly rooted list constructor, and a frameless local tail. The candidate has an entry frame and spills purely to prepare the same leaf operation. Whole-function equality still depends on separate branch layout, operand choices, and label/type handling.

Pinned beam_ssa:clobbers_xregs at 193-203 marks calls and specific VM operations as clobbers. The normal non-GC BIF is outside this group. beam_ssa_pre_codegen:need_frame_1 at 1566-1597 leaves this leaf unframed. In erts/emulator/beam/emu/ops.tab 998-1002, a body BIF with no local failure target uses i_bif_body. The invocation in bif_instrs.tab reads its input into temporary registers and writes one output register. The hd_1 native implementation at erts/emulator/beam/bif.c 2918 takes a list head without allocation or raises badarg.

## Local patch

The patch admits only an unconditional BifOne whose import resolves exactly to erlang:hd/1. It applies at the existing canonical whole-function leaf allocator, excluding functions with returning calls from the new clause. On successful continuation the opcode defines only its stated destination and uses its source register. Failure exits by throwing and keeps the original BIF and line in the output. The rewrite borrows the existing liveness, interference, terminal-frame and initialized TestHeap prefix proofs. Register and cleanup definitions are rewritten only after they pass. Unsupported or unsafe bodies stay on their emitted path. element/2, other non-GC body BIFs, ordinary clobber boundaries and protected frames remain outside the added capability.

The permanent emission test verifies the body BIF, line and heap instructions remain on the frameless positive path and the tail uses the frameless form. It also checks framed controls for element/2, a preceding ordinary call and a protected call. The exact Part 1975 compiler keeps four frame slots for the positive probe while pinned has no allocation. Both pass all ten native cases. Empty and non-list heads report the exact erlang:hd argument with the source caller at line 7. Two separate match failures agree at lines 5 and 6.

Pinned and generated Part 1973 core_parse parse the same emitted Core listing and reject the same truncated input. Both erl_parse images produce the same abstract forms for two valid source inputs and reject the same malformed source input. Three other generated modules loaded successfully in native OTP. The loader-only receipt identifies megaco_text_parser_v3, xref_parser and yeccparser by hash. It does not claim execution of these three. The report retains the initial validator formatting error separately from the successful run.

Both diffs strictly replay against the source closure and all four affected C# files parse cleanly. The patched compiler has not been built or run. Exact post-change module/function counts and applicable regressions remain pending.
