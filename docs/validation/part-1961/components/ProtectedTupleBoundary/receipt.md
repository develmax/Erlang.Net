# Part 1961 protected tuple construction evidence

Pinned reference is Windows OTP 29.1.1 at compiler source commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. Input compiler is the immutable Part 1959 candidate. The candidate was built from the 921 verified C# input hashes in that source closure plus the strict local patch. Patch replay and SDK layout passed.

| Result | Before | Candidate |
| --- | ---: | ---: |
| Scoped modules with whole-image equality | 9 | 9 |
| Different | 4 | 4 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| CompilerError | 0 | 0 |
| megaco_monitor physically equal functions out of 41 | 31 | 35 |
| megaco_monitor normalized equal functions out of 41 | 31 | 35 |

The uncached 13-module candidate run took 39.352 seconds. All of its output hashes agree with Part 1959 except megaco_monitor. No new raw equality and no losses occurred. Scope-derived source witnesses, release hashes, physical and normalized operands, and actual output images are preserved separately. [Corpus report](candidate-sealed.json) · [SHA comparison](wide-compare-sealed.json) · [Function comparison](instruction-delta-sealed.terms).

Pinned `beam_ssa_opt.erl:1771–1868` moves leading and trailing non-failing construction across try tag boundaries. `beam_ssa_pre_codegen.erl:1433,1950,2658` places frames, identifies live Y values, and reserves their slots. `beam_ssa_codegen.erl:1700–1718` emits the landingpad, tag release and protected calls. Pinned compiler listings show that a terminal remote call consumes its inputs before either outcome needs them. A result tuple whose construction cannot fail follows `kill_try_tag` and only the tag has a Y slot. Its exact exception class/reason comparison uses the original X0/X1, and reraising uses X2/X1. A freshly bound pure tuple passed exactly once to a terminal caught call can be emitted directly in its outgoing X register before the catch, with the call itself still protected. [Pinned listing](megaco_monitor.precodegen) · [Source replay](replay-files.json).

The four changed functions are `lookup_request_field/2`, `lookup_reply_field/2`, `request_lockcnt_inc/1` and `request_lockcnt_dec/1`. The Type section is now exact. The remaining four families are ranked below by first differing opcode counts over the 860 residual Part 1959 release images. Counts are diagnostic families rather than proven applicability of this patch.

| Remaining differences | Corpus modules / functions by signature | megaco_monitor functions | Reference dependency and confidence |
| --- | --- | --- | --- |
| Projection before frame | 465 / 2,010 | handle_info/2 | SSA constructor partition and live ranges at the shared tuple arity case. High for the premature frame, medium for projection grouping. |
| Tuple guard before frame | 428 / 1,054 | handle_call/3 | SSA constructor partition, failed-guard snapshots and branch-owned frames. High for the block boundary. |
| Exact selector before frame | 320 / 688 | handle_apply/5 | Frame placement in the terminal caught alternative after an unframed tail call. High. |
| First argument move or Y initialization | 518 / 1,501 for the complete grouped corpus family | apply_after/5, warning_msg/2 | SSA call liveness and direct tuple construction for apply_after. Private input typing and legal hoisting of closure-free append and list construction for warning_msg. High for first placement, medium for proof coverage. |
| Outer term-form test before tagged tuple and result guard | One diagnosed module / one function | cancel_apply_after/1 | OTP constructor partition and keeping the successful call result in X. High for instruction order. |

The Line section uses identical counts and coordinates with a different index order. Both forms have 88 physical line instructions and 86 distinct coordinates. Candidate indices 72–77 put line 316 at the beginning of the run. OTP places it last, matching final physical first use at the cold badrecord label. Attr's remaining difference is the derived version hash from Code. [Line table delta](line-delta.json) · [Physical differences](pinned-linked-revised.diff).

The permanent test is red on the exact frozen Part 1959 compiler and green on the sealed candidate. It checks live exception registers and keeps fallible arithmetic inside the try. Pinned native ERTS executed nine actual module observations including missing keys, invalid fields, counter updates and the original error stack. Eight synthetic observations include a catch that captures its original input and an exception reraised with the original frames. All three roles agreed. [Focused tests](focused-sealed-results.json) · [Release execution](native-monitor-sealed.log) · [Synthetic execution](native-fixture-sealed.log).

The first isolated implementation missed the explicit Erlang begin/end block and retained identical output. Its raw report is preserved. A fixture symbol used the disassembler name instead of the C# opcode catalog and was corrected. The formatted candidate scope reported process exit 1 for differing modules. An initial receipt wrapper expected exit 0, and subsequent wrapper fixes preserved the already measured scope and red result. The full 1,366 module corpus and whole managed gates were outside this isolated pass. [Initial scope](candidate-try.json) · [Fixture build diagnostic](focused-build-invalid-opcode.log) · [Scope timing](scope-sealed-timing.json).
