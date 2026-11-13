# Next steps

Prioritize register planning for selector calls with computed operands. Inspect length before allocation and the guarded function value in shell_default, whose other normalized function bodies already match. [Instructions](validation/part-1540/shell-default-baseline.txt) · [Corpus ranking](validation/part-1540/first-difference-ranking.txt).

Rank terminal operator emission by specific operator and operand forms. The previous audit finds 56 terminal operator bodies across 43 modules. Prioritize branch-local liveness that can also close a near match, starting with the integer branch and binary construction in `ssh_bits:mpint/1` and the guarded projections in `prim_buffer:read_iovec/2`. [Operator audit](validation/part-1509/first-call-spills-after.txt) · [Nearest images](validation/part-1523/nearest.txt).

Extend tuple call chains to sequences containing intervening constructions. The audit has 77 functions across 34 modules after projecting fields for flat tuple returns and case arms. In `io_lib_format:collect_cseq/2`, project the live fields into the following calls and keep the intervening map construction in the same lifetime plan. [Remaining chains](validation/part-1510/terminal-shapes-after.txt).

The remaining terminal remote call group covers 76 functions across 23 modules. The terminal operator group covers 56 functions across 43 modules, including negation, short-circuit tests and chained append. Continue ranking by shared lowering rules within those groups. [Entry saves](validation/part-1509/first-call-spills-after.txt).

Rank the remaining terminal tuple arms by continuation and retained field. The audit finds 23 functions across 23 images that bind a simple call result before a longer sequence. Compare the resulting frame and type chunks with the reference. The xmerl DOM projection now matches, leaving build_dom/2, get_dom/1, and parse_attributes/4 in xmerl_sax_simple_dom. [Continuation shapes](validation/part-1523/continuation-shapes-after.txt) · [Nearest images](validation/part-1523/nearest.txt).

Rank the remaining release images by shared instruction differences and their distance from full equality. Extend register-resident result projection and branch-local slot trimming where the same instruction shapes recur. In `prim_buffer:read_iovec/2`, the selected binary split field still moves to Y before list construction. OTP keeps it in X and trims the branch frame. [Instructions](validation/part-1481/prim-buffer-read_iovec.txt) · [Control](validation/part-1481/probe.txt).

Extend the branch liveness plan for a call followed by byte_size and ordered tests. In prim_buffer read_iovec/2, the large-size branch needs four Y slots in OTP and seven in C#. Keep transient arithmetic and tuple projection in X and trim Y slots at their last use. Use the comparison-guard ranking to measure the shared effect, then compare the complete release file. [Instruction comparison](validation/part-1479/prim-buffer-read_iovec.txt) and [source ranking](validation/part-1479/comparison-ranking-before.txt).

Match preallocation comparison guards for numeric size dispatch. In prim_buffer only read/2 and read_iovec/2 still differ after label normalization. Both start with a numeric comparison before frame allocation in OTP. Use the shared guard and register path across modules and preserve float comparison semantics. The zlib on_load/0 instructions match, with other functions still open. [Remaining preloaded functions](validation/part-1478/remaining-preloaded-functions.txt).

Use the full-image ranking to target groups whose remaining differences share a cause. In xmerl_xsd_re only chr/2 and scan/2 still differ after label normalization. Match the integer guard dispatch and UTF-8 binary matching paths and measure affected release modules. Preserve numeric equality with float operands unless the caller analysis proves the selector is an integer. Check the literal conversion and one-arm result match in erl_tracer on_load/0 as another near-complete image. [xmerl functions](validation/part-1477/xmerl-functions-after.txt), [nearest images](validation/part-1477/nearest-before.txt), and [on_load comparison](validation/part-1477/erl-tracer-on-load-before.txt).

Plan shared payload projection for tagged case arms that read the same field, then emit the terminal calls and returns with the OTP instruction layout. Preserve the selector in X0 for unmatched tags. The Part 1475 ranking finds 100 differing functions in 76 modules with two or more unguarded tagged two-element tuple arms that read the payload. Start with the now zero-slot xmerl_xsd_re map/1 control and compare the whole corpus. [Ranking](validation/part-1476/shared-payload-code-ranking.txt) and [current instructions](validation/part-1476/map-selector-after.txt).

Keep fields of a matched case tuple in X through terminal branch construction and calls. The source ranking finds 133 functions in 103 modules with a direct remote selector and a tuple arm that returns a field or tuple, and 62 functions in 44 modules where an arm constructs a call argument. Start by combining the existing register tuple arms and call argument construction planner. Verify zero Y slots in xmerl_xsd_re map/1 and check whole release files after the change. [Arm ranking](validation/part-1475/case-arm-ranking.txt) and [selector instructions](validation/part-1475/map-selector-after.txt).

Prioritize frame and register liveness for single-clause functions whose body is a case. The source ranking identifies 1,758 functions across 485 modules with an oversized nonzero frame, including 757 functions in 346 modules that select on a remote call. Audit values used only before the call and values live in each successor. Start with register-resident fields and direct selector chains. Use the zero-slot xmerl_xsd_re map/1 as a control and measure whole-file transitions across the corpus. [Source ranking](validation/part-1474/frame-source-ranking.txt), [selector ranking](validation/part-1474/case-selector-ranking.txt), and [selector control](validation/part-1474/map-selector-before.txt).

Prioritize common frame placement and value liveness before adding more emitter shapes. Use the ranked first instruction differences to group overallocated frames and early frame placement. Use the small wxDisplay and logger_handler_watcher examples to verify actual whole-file transitions. Compare the common change across all 1,366 release files. [Frame ranking](validation/part-1473/frame-ranking.txt) and [nearby files](validation/part-1473/closest-instructions.txt).

Extend the case entry lowering to keep a selector in X through matching and guards, then save it on paths that need it across a clobber. Start with the 52 functions in 40 modules that enter a case after a call and first reload the saved result into X. The alias probe moves a test before the save but keeps the three-slot frame. [Ranking](validation/part-1472/reload-ranking.txt) and [alias probe](validation/part-1472/alias-probe.txt).

Forward the first call result into a case selector or call argument when it can stay in X until its last use. Start with the 230 functions in 153 modules where the first use of the saved slot is a move back to X. Apply local proper-list result inference to the eight public_key encoders that now have the OTP frame size. [Spill consumers](validation/part-1471/spill-consumers.txt) and [remaining instructions](validation/part-1471/release-remaining.txt).

Use the pinned OTP copy_retval pass to delay moving a call result into a stack slot until it must survive a clobbering instruction. Start with the 629 functions across 292 modules where only C# immediately spills the first result. Preserve exception paths and compare register and frame operands across applications. [Ranking](validation/part-1470/call-result-ranking.txt) and [release examples](validation/part-1470/variable-frame-examples.txt).

Preserve the existing register-reusing entry paths for the five direct bodies that lost normalized equality and compare the changed closure capture order. Then use the largest measured frame group to reduce slots allocated for a variable-head sequence in 527 modules. [Function differences](validation/part-1469/function-differences.txt) and [frame ranking](validation/part-1469/frame-ranking.txt).

Delay stack allocation until after list and tuple-head tests where clause order and live values permit it. Start with the 203 functions in 124 modules that have a tuple at the head of a list and a sequence in the clause body. Compare representatives from several applications before the next full release comparison. [Ranking](validation/part-1468/list-entry-ranking.txt) and [release examples](validation/part-1468/list-entry-examples.txt).

Continue with the nonempty list frame group at 530 modules. Prove the local caller facts for proper list arguments and integer accumulators in unused_bitlist/3, then apply the register arithmetic planner to longer list prefixes and other frameless return shapes. [Release example](validation/part-1467/unused-bitlist.txt) and [frame ranking](validation/part-1467/frame-ranking.txt).

Start with shared field projection and frame allocation. The corpus ranking finds 676 images with a projected field moved to an X register where OTP writes it directly, 530 with list checks scheduled differently around allocation and 527 with tuple checks scheduled differently. Confirm the first divergent lowering decision on examples from different applications. [Shared Code ranking](validation/part-1461/shared-code-ranking.txt).

Reproduce OTP Core inlining decisions at local call sites before applying release inline in BEAM generation. Compare weighted size after substitution and separate effort limits across the 56 differing inline-profile images. Keep the experiment as a regression baseline. Measure broadly shared Code differences, including register allocation, across the full corpus. [Inline experiment](validation/part-1461/README.md) and [HTTP callback frame](validation/part-1460/header-callback.txt).

Extend serial mapper calls to one saved capture. The arity-two mapper group has 738 missing helpers in 211 modules. Start with direct local calls and preserve the capture across each mapper and recursive call. In asn1ct:compile_set/3, prove the proper-list result from get_file_list before omitting the invalid generator branch. [ASN.1 instructions](validation/part-1452/asn1ct-compile-set.txt).

Add discarded mapping over tuple generators with a helper that tail-recurses after the effectful call. Keep the generator failure for arbitrary inputs. For asn1ct:print_structured_errors/1, establish a proper-list proof across callers before omitting the error path. [OTP oracle](validation/part-1451/discarded-oracle.txt).

Align the new ASN.1 comprehension helper frames and exception paths. Move the list check before frame allocation and keep the tail, capture and try token in separate live slots. The fifteen parent instruction sequences already match after label normalization. [Frame deltas](validation/part-1450/frame-delta.txt) and [fixture instructions](validation/part-1450/probe.txt).

For simple tail calls with checked record fields, keep the source inputs in X registers through all ordered checks, project fields after the checks, and schedule the failure blocks after the tail call. Use the checked_fields fixture to validate register overlap and the failing source value. [Fixture](validation/part-1449/probe.txt).

Continue the list-before-allocation group now measured in 530 release images. Extend register construction to matching cons arms that return tuples, starting with global:find_node_tag2/2. Build the result after the tag and bound-field checks and preserve the head and tail on failure. For child_name/2, prove a proper list of pairs across local calls. In httpc the input comes from the success clause of a try whose body is a list comprehension of pairs. Use the proof to omit the tuple shape and nil tests. [Global instructions](validation/part-1430/global-entry.txt), [httpc instructions](validation/part-1430/httpc-entry.txt), and [frame ranking](validation/part-1428/baseline-frame-shapes.txt).

For check_unique2/3, share the first extraction between the two-cell and ordinary cons branches. Keep the original suffix for the recursive call when the dynamic element guard fails, and reuse it for [B|T] on success. Then emit the typed element operands before the heap check for the new accumulator cell. [Reference instructions](validation/part-1428/list-entry-examples.txt).

Allocate nested exception handler slots in decreasing Y-register order, preserving their normal and exceptional successors. The nested fixture currently emits y1 then y2 in C# and y1 then y0 in OTP. Re-enable its .NET execution check after the shared slot allocation agrees with the reference. [Nested frame instructions](validation/part-1428/nested-frame.txt).

Extend shared list entry for calls with cons arguments. In collect_quoted/3, schedule the selector and its success and failure bodies after the shared two-cell prefix. In componentrelation_leadingattr/5, defer frame allocation until after the list and record tests and preserve the caught call's record fields. [List-entry ranking](validation/part-1425/list-entry-ranking.txt) and [record branches](validation/part-1423/asn1-before.txt).

Share field projection and literal dispatch across case arms that match the same tuple arity. Start with asn1ct:remove_name_collisions2/4 and its local tuple producer. Reuse the existing return-shape proof before omitting tuple checks. Preserve the original selector value for case_clause errors. [Tuple dispatch candidates](validation/part-1422/tuple-selector-candidates.txt) and [reference instructions](validation/part-1421/list-selector-prefix.txt).

Admit list append bindings and tail expressions into the checked-call planner for the mixed tuple projection schedule in asn1ct_imm:combine_imms/2. [Remaining schedules](validation/part-1417/remaining-schedules.md).

Continue with frame operands in variable-head sequences. The Part 1398 audit counted 1,740 functions in 553 modules. Extend the terminal case planner across record construction for compile_proc/3 by building the record in the last-use input slot before the selector call. [Continuation ranking](validation/part-1398/case-continuations.txt). [Record sequence](validation/part-1395/release-case-prefix.txt).

Continue tuple entry with shared tag dispatch within an arity and with ordered mixed-pattern prefixes. The remaining tuple group covers 1,831 functions in 526 modules. [Frame groups](validation/part-1394/frame-shapes.txt). [Tuple source groups](validation/part-1394/remaining-tuple-entry-groups.txt).

Continue list entry after the new empty-list fallbacks. The remaining nonempty-list first-difference group covers 2,938 functions across 537 modules. The largest audited source subgroup by module count is now a variable list head and a call with a cons argument, covering 219 functions across 132 modules. Pass live call-prefix values into asn1ct:remove_name_collisions2/4 and asn1ct_check:check_fold/3. Use the shared constructor spill plan in the multi-call branch of asn1ct_imm:enc_pre_cg_1/3 after identifying the remaining branch continuation shapes. [Source groups](validation/part-1393/list-case-branches.txt). [Entry instructions](validation/part-1393/list-entry-instructions.txt). [Constructor scheduling evidence](validation/part-1392/README.md). [Continuation instructions](validation/part-1387/next-list-continuations.txt).

Prove nonempty private list inputs from callers and from the branch that distinguishes singleton from longer lists. Both pretty printers' vertical/1 functions now match OTP's stack size and call sequence. They still need the nonempty entry proof and typed tail test. [Pretty printer instructions](validation/part-1388/next-vertical.txt).

Extend list entry cases with register map dispatch. The variable-selector case group covers 39 functions in 24 modules. In beam_ssa_bool:covered_list/5, OTP loads the dynamic map key from the list head and keeps the cached-result branches frame-free. The cache-miss branch allocates five slots. [Map case instructions](validation/part-1386/next-list-map-case.txt).

Continue frame liveness for variable-head sequences. The audit counts 617 direct-call assignments across 238 modules and 331 explicit external-call assignments across 231 modules. Admit pure constructor bindings between calls and keep X0 available while constructing a call argument if it must also be saved. In asn1ct:compile_proc/3, OTP writes the constructed state to the slot previously holding Options and clears the slot for File before the selector call. [Sequence audit](validation/part-1385/sequence-prefixes.txt). [Constructor spill instructions](validation/part-1385/constructor-spill.txt). [ASN.1 call and frame instructions](validation/part-1384/asn1ct-call-frames.txt).

Extend the call-chain planner to pass its live values into a case continuation. A terminal case occurs in 360 functions across 230 modules in the allocation operand group. In asn1ct:includes/2, the final case needs the initial Options and the updated Options3. [Terminal expression groups](validation/part-1385/sequence-endings.txt). [Case and comprehension instructions](validation/part-1384/next-case-frames.txt).

Extend register construction across more than one dynamic map allocation or through later constructors. Partition heap checks around map allocations and verify which X registers remain live at each check. The current planner schedules one map after its tuple and list children. [Evidence](validation/part-1383/README.md).

Retain parent tuple values across calls when their fields are needed only afterward. In asn1ct_check:leading_attr_index1/5 this can reduce the matching arm from five saved slots to three. [Release instructions](validation/part-1379/release-functions.txt).

Inspect construction sequences in two images with one differing function. httpc_handler_sup:init/1 can fold constant bindings and schedule nested tuple/list construction in one register. ssh_lsocket_sup:init/1 can use the constant call sequence planner after its nil check. Compare instruction and literal table order against the pinned release. [Remaining functions](validation/part-1377/remaining-functions.txt).

Extend register forwarding to trim expired Y slots before the final value-producing call. In cth_conn_log:init/2 the previous result is already in X0 and the sole saved argument is loaded before trim 1,0. Check the shared planner and related call assignments. [Release instructions](validation/part-1376/cth-conn-log.txt).

Extend call and frame planning across sequences inside case arms. The tagged match audit finds an arm with a sequence in 20 differing modules. In ct_release_test:copy_file/3 compare how OTP shares the entry frame with the selector and trims slots in each arm. [Tagged case source audit](validation/part-1366/tagged-case-audit.txt). [Continuation instructions](validation/part-1364/continuation-candidates.txt).

Extend tagged payload liveness to chains with several intermediate values and to guarded clauses. The straight-call audit finds four modules with delayed use and five with both immediate and later uses. The broader 77-module group includes asn1ct:pretty2/2, where OTP saves the tagged result and extracts the file handle after another call. [Straight call audit](validation/part-1365/payload-chain-audit.txt). [Broader source audit](validation/part-1364/tagged-continuation-audit.txt). [ASN.1 instructions](validation/part-1364/asn1-payload-lifetime.txt).

Continue frame placement and call scheduling for guarded clauses with multiple calls. wxListItemAttr:new/3 converts two colour values, and wxStaticBoxSizer:new/2 dispatches between a guarded forwarding clause and an object command. Earlier audits found 472 modules with an initial call binding and a final tail call, and 50 wx modules with text conversion and option mapping. [Current instruction comparison](validation/part-1362/remaining-wx-instructions.txt). [Call sequence audit](validation/part-1353/call-sequences.txt). [Text and option mapping audit](validation/part-1345/wx-text-option-audit.txt).

Compare shared callback code generation for ex_textCtrl, ex_htmlWindow and ex_notebook. The same five function names differ in all three modules. Their export and local symbols agree, and their attributes agree after removing vsn. [Function gaps](validation/part-1361/wx-function-gaps.txt). [Chunk payload audit](validation/part-1361/chunk-ranking.txt).

Extend private argument analysis beyond direct literal call sites and register case branches. Inspect constants forwarded through private calls and the record shape in xmerl_xpath:expanded_name/3. [Argument audit](validation/part-1360/private-constant-arguments-final.txt). [Remaining record check](validation/part-1360/expanded-name.txt).

Continue the external conversion group with atom, binary and ETF conversions. Part 1344 schedules the most frequent list and integer conversions. [Conversion call ranking](validation/part-1343/conversion-call-audit.txt).

Extend computed cons results to multiple head calls, including beam_asm:encode_line_items/2. The abstract forms contain this shape in 33 functions across 27 modules. The shared chain planner now handles tuple conversion branches and matches qlc_pt:restore_locations/2. [Current evidence](validation/part-1343/README.md). [Source inventory](validation/part-1340/list-result-call-ranking.txt).

Share tuple shape and field extraction between adjacent cons branches. The mixed cons planner now preserves the original input through guard failure and matches both escaping functions. Compare record search functions with a common tuple shape and different field tests. [Evidence](validation/part-1342/README.md).

Share the tagged first element and dispatch the second tuple by arity in beam_ssa_codegen:bs_seq_match_fixup/1. The nested tests and remainder guard now stay in registers with a one-slot result frame. The audit found the leading shared-tag shape in four modules. In bs_eq_fixup/1, keep the first list head in X1 on fallthrough and plan binary construction before allocating the call frame. [Instruction comparison](validation/part-1339/beam-ssa-codegen.txt). [Corpus ranking](validation/part-1333/cons-call-frame-ranking.txt).

Audit delayed tuple field extraction across calls in the allocation ranking. asn1ct:save_gen_state/2 matches OTP with one preserved tuple slot. Compare the same liveness in functions where the tuple fields feed calls or constructors. [Evidence](validation/part-1325/asn1-save-gen-state.txt).

The opcode ranking finds a move versus init_yregs mismatch after different allocations in 793 modules, and a tuple test versus allocate mismatch in 661. These groups overlap. Compare branches that can fail before stack slots are needed, starting with the ranked examples. [Ranking](validation/part-1319/opcode-ranking.txt).

Infer integer selector types from the parser state stack before emitting typed registers. Integer select_val and shared tail-call blocks now generate; in core_parse:yeccgoto_anno_atom/7 the release takes the selector from hd/1. [Evidence](validation/part-1318/README.md).

Prioritize functions with three or more clauses and a nonempty-list pattern in a clause head. This group accounts for 525 modules in the allocation audit. Start with unguarded cons patterns whose head and tail are variables. This shape appears in 419 modules in that group, and 311 have a corresponding clause with a local tail call. Extend dispatch beyond the three-clause cons, nil and fallback shape. Of the modules with local tail calls, 179 have a branch using variables and constants only and 162 have a branch constructing a list argument. The categories overlap. The planner now accepts one constructed cons with a variable tail. Share list shape tests and extraction between adjacent cons clauses with different head patterns. The audit identified this shape in 115 modules with a variable-tail argument. [Corpus audit](validation/part-1312/shared-cons-candidates.txt). Reuse tail argument scheduling and compare fallthrough with OTP. [Argument audit](validation/part-1309/local-tail-arguments.txt). [Pattern audit](validation/part-1308/multi-clause-list-shapes.txt). [Body audit](validation/part-1308/list-branch-bodies.txt).

Compare tail receive bodies with OTP and measure which affected functions can return or tail-call directly. In gl:rec/1 this removes the extra join label that shifts the export and local table labels. Then compare shared tuple field extraction and register lifetimes. [Evidence](validation/part-1268/README.md).

Distinguish include directives that record different paths for the same header basename, starting with Inets. [Evidence](validation/part-1195/README.md).

Rank the remaining differing images and check whether common release metadata can close several gaps at once. [Evidence](validation/part-1194/README.md).

Compare ct_master_status:handle_event/2 with the deferred tuple emitter and inspect the release build metadata. [Evidence](validation/part-1193/README.md).

Use actual branch register lifetimes to remove copies after payload extraction, and prioritize larger measured cohorts. [Evidence](validation/part-1192/README.md).

Compare shared payload extraction before select_val and reuse of entry slots across the case bodies. [Evidence](validation/part-1191/README.md).

Compare stack allocation and slot reuse in the release functions affected by register-based case dispatch. [Evidence](validation/part-1190/README.md).

Measure which release functions with register case selectors can delay stack allocation until after the test. [Evidence](validation/part-1189/README.md).

Select a higher-coverage change from the measured frame cohorts and confirm it against several release functions. [Evidence](validation/part-1188/README.md).

Keep fallback call results in X0 when the following tail call builds an outgoing list from them. [Evidence](validation/part-1187/README.md).

Select a release function from the largest allocation cohort and confirm the same lowering shape across modules before extending the emitter. [Evidence](validation/part-1186/README.md).

Keep first-call results in X0 for tuple returns after entry guards and remove their temporary Y slots. [Evidence](validation/part-1185/README.md).

Compare actual OTP frame setup for branches with shared and distinct live values before extending per-clause allocation. [Evidence](validation/part-1184/README.md).

Separate argument uses during pattern dispatch from values live across the first call. [Evidence](validation/part-1183/README.md).

Profile register and frame operands in the largest remaining allocation cohort before extending dispatch further. [Evidence](validation/part-1182/README.md).

Compare shared register dispatch across multiple guarded tuple clauses and reuse common field extractions. [Evidence](validation/part-1181/README.md).

Compare early guard and multiple-clause dispatch in the remaining register allocation cohorts. [Evidence](validation/part-1180/README.md).

Compare register dispatch and frame liveness for tuple clauses with guards or fallbacks. [Evidence](validation/part-1179/README.md).

Apply register-based dispatch and direct extraction to additional tuple clause shapes where failures can branch before allocation. [Evidence](validation/part-1178/README.md).

Extract used tuple fields directly to their Y slots after allocation and skip unused fields. [Evidence](validation/part-1177/README.md).

Move clause-local tests into register-based entry dispatch and size the resulting frames by liveness across the first call. [Evidence](validation/part-1176/README.md).

Plan tuple tests before frame allocation and allocate slots for values live across the first call. [Evidence](validation/part-1175/README.md).

Reduce excess entry stack slots in the X-to-Y move cohort. Compare registers live across the first safepoint before changing initialization order. [Evidence](validation/part-1174/README.md).

Use the latest opcode ranking to separate the most common move versus init_yregs differences by frame placement and liveness. [Evidence](validation/part-1173/README.md).

Compare the four wx images that share Code, ExpT, FunT, LocT and Attr differences. Rank their common instruction changes against the other five-chunk groups. [Evidence](validation/part-1172/README.md).

Determine which initial arguments and bindings survive the first call. Use that set to size frames before emitting init_yregs. [Evidence](validation/part-1080/README.md).

Analyze the consecutive argument moves seen after allocation in the largest move versus init_yregs cohort. OTP emits another move immediately in 738 files. Reproduce the necessary Y initialization based on the first stack safepoint. [Evidence](validation/part-1079/README.md).

Keep literal fields in scratch registers for flat entry list checks. Then compare frame liveness of the largest remaining allocation cohorts. [Evidence](validation/part-1078/README.md).

Keep intermediate tails and fields used only by pattern tests out of Y slots. Use the reference opcode ranking and verify the corresponding entry shapes. [Evidence](validation/part-1077/README.md).

Distinguish cons fields used in entry checks from those needed after a call. Re-rank common entry scheduling differences to prioritize the largest source shape. [Evidence](validation/part-1076/README.md).

Extend shared cons extraction to functions whose clauses all require a cons cell. Compare liveness before saving shared fields in Y registers. [Evidence](validation/part-1075/README.md).

Share early extraction across multiple cons clauses while retaining original inputs for later patterns and guards. Compare branch ordering and frame size against OTP. [Evidence](validation/part-1074/README.md).

Test common nested list head patterns in X registers before allocating. Use operand liveness to distinguish early extraction from OTP paths that allocate first. [Evidence](validation/part-1073/README.md).

Use get_hd or get_tl when only one field is needed, and extract common nested head patterns before allocation. Compare the resulting instruction sequences with the largest remaining list opcode cohort. [Evidence](validation/part-1072/README.md).

Extract list head and tail bindings in X registers before frame allocation. OTP uses get_list immediately after the entry test in 646 files in the measured cohort. Check the affected source shapes before generalizing. [Evidence](validation/part-1071/README.md).

Classify the remaining list-test versus allocation differences by source shape. The default-list pattern changed one image. Prioritize a common pre-call shape and keep values in X registers through nonallocating checks. [Evidence](validation/part-1070/README.md).

Compare early list case dispatch in asn1_db:dbstart/1 and other top-ranked list cases. Delay frame allocation past the list shape decision where the source does not require earlier work. [Evidence](validation/part-1069/README.md).

Keep tuple fields in X registers when their last use is in call arguments. Reserve Y slots for fields that must survive the call, then measure common list entry dispatch. [Evidence](validation/part-1068/README.md).

Record peak Y slots per tuple arity group in the planning pass. Use those sizes for branch allocation, tail calls and failure exits. [Evidence](validation/part-1067/README.md).

Extract tuple fields in X registers before allocating branch-local frames, starting with the groups whose Code length gap widened. Measure mixed list entry dispatch after the tuple frame change. [Evidence](validation/part-1066/README.md).

Use select_tuple_arity before allocating a frame for mixed-arity clauses, with clause order preserved within each arity. Compare early dispatch on mixed list entry shapes. [Evidence](validation/part-1065/README.md).

Share the initial tuple type test when clause arities differ and measure the remaining tuple dispatch. Inspect list dispatch with mixed shapes before frame allocation. [Evidence](validation/part-1064/README.md).

Hoist common tuple shape tests for multi-clause functions before frame allocation. Measure the change against the corpus ranking, then extend delayed allocation to list dispatch with different shapes. [Evidence](validation/part-1063/README.md).

Apply X-register liveness to simple call setup across nonallocating BIFs. The tuple-only pass changed two corpus images, so prioritize the shared call path. Reuse identical BIF results when no intervening operation can change them. [Evidence](validation/part-1062/README.md).

Keep entry parameters in X registers across proven nonallocating BIFs. Emit their results directly to the consuming register to remove remaining frame slots in simple constructors and call setup. [Evidence](validation/part-1061/README.md).

Keep a sole computed tuple member in X registers until tuple construction. Then preserve input arguments across nonallocating BIFs to remove frame slots that still distinguish the probe from OTP. [Evidence](validation/part-1060/README.md).

Retain a sole computed call argument in X registers when the other arguments can be loaded from constants or established bindings. Compare the generated call setup and frame size against OTP across the corpus. [Evidence](validation/part-1059/README.md).

Derive the general stack layout from values live across calls. Use the pinned OTP opt_allocate pass to omit initialization only for slots defined before the first stack safepoint on each path. [Evidence](validation/part-1058/README.md).

Investigate delayed frame initialization and argument liveness. The most frequent first opcode mismatch is move versus init_yregs in 832 images, beginning with asn1_db:req/1. Verify the lowering against pinned OTP before applying it more broadly. [Evidence](validation/part-1057/README.md).

Inspect register-only tuple construction and atom dispatch before supervisor calls, beginning with diameter_tcp_sup. Check reuse across the remaining corpus. [Evidence](validation/part-1056/README.md).

Identify additional call-result branches that can keep temporary pattern fields in X registers. Group by affected modules and retain cross-call values only. [Evidence](validation/part-1055/README.md).

Lower call-result cases with only cross-call inputs in Y registers and temporary pattern fields in X registers. Start with asn1_db:get_table/3. [Evidence](validation/part-1054/README.md).

Break down the largest remaining move versus init_yregs group by frame placement and live values. Use the opcode ranking to pick a shared lowering improvement. [Evidence](validation/part-1053/README.md).

Rank the remaining code differences by affected modules. Extend live-argument lowering where a nested call result and preserved inputs share the same tail-call shape. [Evidence](validation/part-1052/README.md).

Match diameter_transport:select/3 by saving only arguments live across eval/1 and combining the frame and list heap allocation. Check the complete module and corpus. [Evidence](validation/part-1051/README.md).

Extend frame-free tail lowering to tuple destructuring and nested arguments in diameter_transport:start/3. Compare allocation and byte differences across the corpus. [Evidence](validation/part-1050/README.md).

Fold constant list suffixes and extend frame-free tail lowering to tuple destructuring and nested argument construction in diameter_transport:start/3. [Evidence](validation/part-1049/README.md).

Build simple list arguments in X registers before direct calls. Start with the list construction in diameter_transport and measure frame and byte differences across the corpus. [Evidence](validation/part-1048/README.md).

Align literal dispatch grouping and typed operands with OTP, then extend register-only lowering to eligible guards. [Evidence](validation/part-1047/README.md).

Extend frame-free lowering to clause dispatch where head tests and tail arguments fit in X registers. Use the corpus frame counts to measure the effect. [Evidence](validation/part-1046/README.md).

Trace placement of independent assignments in OTP mixed tail calls. Recover the lost cover and qlc lifted body matches and compare the remaining general allocation differences. [Evidence](validation/part-1045/README.md).

Extend the OTP move-chain scheduler to mixed constant and register arguments after comparing their decoded instruction order. Keep the Core capture identity experiment separate. [Evidence](validation/part-1044/README.md).

Derive Core capture identities across clause patterns and aliases before changing general closure ordering. Check against the named transitions from the discarded sorting experiment. [Evidence](validation/part-1043/README.md).

Trace OTP capture ordering and independent argument moves in lifted functions. Extend register-only emission to clause dispatch where tests and all resulting paths fit in X registers. [Evidence](validation/part-1042/README.md).

Trace allocation inside individual clauses and keep only values live across calls in Y registers. Start with beam_disasm:resolve_inst/4 and unicode_util:gc_h_lv_lvt/3. [Evidence](validation/part-1041/README.md).

Reuse Y-register slots across mutually exclusive general function clauses, preserving argument slots and frame high-water tracking. Check the five images with oversized frames, then repeat all 1366 release comparisons. [Evidence](validation/part-1040/README.md).

Classify OTP loader errors in the 978 differing images rejected by prepare_loading. Start with asn1_db:dbstart/1 and isolate the first failing instruction form in a minimal source module. [Evidence](validation/part-1039/README.md).

Reduce general frame sizes by keeping short-lived call results and tail-call arguments in X registers where no call intervenes. Start with asn1_db:init/2 and measure the effect using the frame analysis. [Evidence](validation/part-1038/README.md).

Trace the remaining fprof specialized fallback, source rejections and common release Code differences. [Evidence](validation/part-968/README.md).

Trace remaining source and emission failures and recover shared release build profiles. [Evidence](validation/part-967/README.md).

Group the remaining six emission failures and seven source rejections, and recover shared release build profiles. [Evidence](validation/part-966/README.md).

Trace the comprehension in the remaining targeted inline template and prioritize shared release instruction differences. [Evidence](validation/part-965/README.md).

Inspect the four targeted inline lowering failures and prioritize shared improvements to the emitted release Code. [Evidence](validation/part-964/README.md).

Trace comprehension filter bindings and the four targeted inline lowering failures. [Evidence](validation/part-963/README.md).

Trace binary generator and comprehension binding failures and quantify release build profile gaps. [Evidence](validation/part-962/README.md).

Trace the three remaining EmitBody fallbacks and quantify which release build profiles can be recovered from shared build rules. [Evidence](validation/part-961/README.md).

Lower round/1 in qlc_pt guards and compare the new supervisor and shell_docs_markdown images against OTP. [Evidence](validation/part-960/README.md).

Lower min/2 guards in supervisor and shell_docs_markdown and compare shared Code differences. [Evidence](validation/part-959/README.md).

Trace remaining receive guard and EmitBody failures, then compare common Code differences against OTP. [Evidence](validation/part-958/README.md).

Trace the remaining EmitBody and Expression failure groups and compare common Code differences against the pinned release. [Evidence](validation/part-957/README.md).

Trace the grouped andalso guard in snmp_config and compare the newly generated eunit instructions against OTP. [Evidence](validation/part-956/README.md).

Trace the six debug token mismatches in eunit and snmp_config and compare the generated beam_asm Code and Dbgi. [Evidence](validation/part-955/README.md).

Trace the remaining debug token mismatches, including beam_asm:build_beam_debug_info_1/1, and compare the new dialyzer images. [Evidence](validation/part-954/README.md).

Trace the six remaining debug token mismatches and compare typer instructions with the pinned release. [Evidence](validation/part-953/README.md).

Trace the seven debug-form failures at Write. Start with map updates and shorthand catch patterns. [Evidence](validation/part-952/README.md).

Trace bindings in nested case expressions, then compare the most common Code and release metadata differences. [Evidence](validation/part-951/README.md).

Trace the fifteen remaining variable lookup failures and compare release init bodies against OTP constant folding and register allocation. [Evidence](validation/part-950/README.md).

Trace the restricted EmitBody fallback in 28 modules. Start with the patterned init clause in logger_sup. [Evidence](validation/part-949/README.md).

Lower variables bound across all case branches and read afterward. Start with ct:fail/1 and socket:cancel/2. [Evidence](validation/part-948/README.md).

Compare release code and metadata for the newly admitted automatic-inlining cohort and identify shared optimization gaps. [Evidence](validation/part-947/README.md).

Inspect automatic inline candidate selection for the five rejected sources using -compile(inline). Preserve its functional compilation effect. [Evidence](validation/part-946/README.md).

Lower the selective receive and timeout branches in inet_epmd_socket:output_handler_wait/6. [Pinned disassembly](validation/part-860/next-output-wait.txt).

Align remaining LDAP instructions, registers, source coordinates and Type. Keep exact AtU8 and ImpT comparisons. [Evidence](validation/part-647/README.md).

Align remaining LDAP binary instructions and atoms, then register allocation, source coordinates and Type. [Evidence](validation/part-646/README.md).

Align LDAP instruction sequences, import and atom order, register allocation and source/type metadata. [Evidence](validation/part-645/README.md).

Replace LDAP fixed byte extraction through binary:at/2 with native bit matching. Continue register, source annotation and Type alignment. [Evidence](validation/part-644/README.md).

Align remaining LDAP opcode sequences and register lifetimes. Align source coordinates and Type inference. [Evidence](validation/part-643/README.md).

Align source coordinates for LDAP Dbgi and Docs. Continue BEAM instruction selection, registers, Line and Type. [Evidence](validation/part-642/README.md).

Align remaining LDAP generated functions and source positions, then continue instruction, register and inferred type alignment. [Evidence](validation/part-641/README.md).

Align generated LDAP function bodies and positions with release forms. Continue BEAM instruction and register alignment and inferred Type metadata. [Evidence](validation/part-640/README.md).

Align LDAP generated source and included record forms with the release. Continue instruction selection, source annotations and Type inference. [Evidence](validation/part-639/README.md).

Align remaining LDAP call frames and instructions. Reconstruct Dbgi abstract forms, function annotations and inferred types. [Evidence](validation/part-638/README.md).

Align remaining LDAP encoder and decoder register lifetimes and instruction selection. Reconstruct abstract debug forms, function annotations and inferred types. [Evidence](validation/part-637/README.md).

Align remaining LDAP register lifetimes and field decoding, then reconstruct debug forms, source annotations and inferred types. [Evidence](validation/part-636/README.md).

Reduce the remaining LDAP encoder and decoder frame lifetimes and align branch instructions. Reconstruct function annotations, Dbgi abstract forms and Type entries. [Evidence](validation/part-635/README.md).

Align dispatch and field decoding instruction selection, register lifetimes and labels. Reconstruct abstract debug forms, Docs, Line and Type metadata. [Instruction gaps](validation/part-634/instruction-gaps.txt).

Align LDAP instruction selection, register allocation and labels, then regenerate release attributes, debug forms, line information and type metadata. [Evidence](validation/part-633/README.md).

Reproduce the original BER primitive helpers and decoder inlining, emit OTP comprehension helpers, then align instructions/register allocation and release metadata. [Evidence](validation/part-632/README.md).

Continue LDAP optional fields, defaults and choice codec bodies, then align instructions and release metadata. [Evidence](validation/part-631/README.md).

Continue direct LDAP sequence and choice codecs, then align helper order, instructions and release metadata. [Evidence](validation/part-630/README.md).

Continue the remaining OTP-generated LDAP type bodies, helper ordering, instructions and release metadata. [Evidence](validation/part-629/README.md).

Reproduce the remaining OTP type bodies and helper order, then align instruction selection and release metadata. [Evidence](validation/part-628/README.md).

Continue reproducing OTP-specific LDAP codec bodies and release metadata. [Evidence](validation/part-627/README.md).

Generate the OTP-specific LDAP type codec bodies and private helper ordering, then align release metadata and instruction selection. [Evidence](validation/part-626/README.md).

Reproduce the OTP ASN.1 function bodies and helper ordering, then align release compiler metadata and instruction selection. [Evidence](validation/part-625/README.md).

Close the remaining release image differences in source generation, OTP lowering and metadata. Continue corpus rejection and unsupported-form work. [Evidence](validation/part-623/README.md).

Continue replace_tags/3, to_tlv/2 and from_tlv/2 to complete whole-module LDAP emission. [Evidence](validation/part-622/README.md).

Continue the five remaining LDAP functions with comprehensions, tag wrapping and value dispatch. [Evidence](validation/part-621/README.md).

Continue the six remaining LDAP functions, including comprehensions, tag wrapping and value dispatch. [Evidence](validation/part-620/README.md).

Continue the eight remaining LDAP functions with recursive list construction, nested matching and comprehensions. [Evidence](validation/part-619/README.md).

Continue the ten remaining LDAP functions with nested clause patterns, list processing and comprehensions. [Evidence](validation/part-618/README.md).

Continue the 12 remaining LDAP functions, including recursive tag matching, nested patterns and comprehensions. [Evidence](validation/part-617/README.md).

Continue the remaining 15 LDAP functions with bit arithmetic, comprehensions, nested patterns and clause dispatch. [Evidence](validation/part-616/README.md).

Continue the remaining 18 LDAP functions with exception handling, bit arithmetic, comprehensions and nested clause patterns. [Evidence](validation/part-615/README.md).

Extend LDAP BEAM lowering to recursive bit patterns with arithmetic, guards with additional work, comprehensions and exceptions. [Evidence](validation/part-614/README.md).

Continue generated ELDAPv3 lowering with tagged tuple dispatch, bit syntax, guards and exception handling. [Evidence](validation/part-613/README.md).

Extend BEAM lowering for the remaining generated ELDAPv3 codec functions, including guards, binary patterns and exception handling. Continue recovering additional release profiles. [Evidence](validation/part-612/README.md).

Extend release type metadata to named and bounded specs, type declarations and callbacks. Continue BEAM lowering for modules outside the current profile, including the generated ELDAPv3 codec. [Evidence](validation/part-611/README.md).

Expand BEAM lowering for the generated LDAP codec. Exact ELDAPv3 release parity also requires reproducing OTP code generation structure and release metadata. [Evidence](validation/part-610/README.md).

Continue from the Part609 corpus diagnostics. Implement file annotations, resolve remaining include inputs, and support semantic compiler options such as inlining and parse transforms. [Evidence](validation/part-609/README.md).

Extend Part608 beyond its bounded call-result pattern to general register-valued constructors, guarded clauses and cases. Recover additional release profiles and reduce source rejections. [Corpus evidence](validation/part-608/README.md).

Extend Part607 with wider case dispatch, guarded and multi-clause functions, compound register-valued constructors, typed abstract forms, and additional release profiles. Extend the pinned compressor to other platforms. [Corpus evidence](validation/part-607/README.md).

Continue source admission with the highest-frequency Part596 diagnostics. Distinguish OTP warning-only compile directives from options that affect semantics or emitted bytes. Implement preprocessing and typed forms before extending module lowering. Documentation chunk emission needs its own OTP comparisons. [Corpus report](validation/part-596/otp-corpus.json).

Continue the BEAM backend with OTP preprocessor and forms admission, then extend instruction selection and optimization beyond constant functions. Bind per-application source provenance, generated inputs, and release build options before using packaged BEAMs as byte oracles. Start with the 1,366 recorded Windows release results. [Evidence](validation/part-595/README.md).

Preserve the completed [sys alternatives and Part591 acceptance](validation/part-591/README.md):37 exports,137/137 per backend and six52-case error depth profiles. Isolate publication from other dirty ports and retain the BEAM tail/non-tail error controls. Complete full CT capture, genuine remote/global/via, platform/resource/scheduling/metadata acceptance before claiming100%; full regression/readiness remain outside the requested focused checks.

Preserve [Part589 rpc acceptance](validation/rpc-port-20261105/README.md), its35-export [backend contract](rpc-backends.md), complete tested error stacks and explicit dependency selection. Extend external ERTS/old-peer/EI, full upstream suite, resource/scheduler/platform/metadata and full-release startup acceptance before claiming universal100%. Keep full regression/readiness outside the present focused task.

Preserve [Part590 error_logger](error-logger-backends.md), its143+10 accepted observations per backend, native warning primitive and stored callback recipes. Extend distributed/upstream/resource/failure/platform and full release-manifest acceptance before a100% claim. Retain the official emulator-suite failure separately. Isolate publication from shared dirty ports; keep the focused-only validation instruction. [Evidence](validation/error-logger-port-20261105/README.md).

Preserve [Part588 logger](logger-backends.md) and its155-observation dual-backend receipt, including5 unchanged upstream functions, source-bound configuration-error events and explicit startup ownership. Remaining acceptance is the full upstream suite, distributed/remote group leaders, overload/resource/failure interleavings, other platforms and complete release-manifest startup; API completeness and finite comparisons do not certify universal100%. Preserve canonical readiness; isolate publication from unrelated dirty ports.

Preserve the io Part587 selected implementations and666/666 primary comparison per backend. Extend full upstream CT terminal/filesystem, cross-platform/resource/distributed-old-peer and complete release-manifest startup acceptance before a universal compatibility claim. Keep explicit dependencies, source/owner/corpus admission and unchanged original assets. Only connected focused checks ran. [Receipt](validation/io-port-20261105/README.md).

Preserve the42-export [gen_server alternative](gen-server-backends.md) and admitted107+70 observations per backend. Extend full gen_server_SUITE, distributed/global integration, resource/scheduling/platform/owned-heap, actual Logger handlers and metadata acceptance before claiming100%. Keep the focused-check instruction and [immutable evidence](validation/gen-server-port-20261105/README.md); isolate publication from concurrent dirty work.

application: retain the [Part584](validation/part-584/README.md) passing finite acceptance for both39-export alternatives. Next acceptance scope is distributed takeover/failover/node-loss, full upstream application_SUITE, whole release/kernel boot, platform/resource exhaustion and module_info equivalence. Keep the original helper protocols and isolate publication from the shared dirty checkout. Full regression/readiness remain deliberately unrun.

Part593: preserve node/dist census and the passing dual-backend262+59/physical13/connected43 closure. Extend full upstream node_container_SUITE, distribution fragmentation/native environments, collector/resource interleavings, platform/metadata/full startup acceptance before a universal claim. Keep focused-only checks and historical canonical readiness. [Evidence](validation/part-593/README.md).

Supervisor Part583: preserve both passing287 API/105 scenario reports, including19 unchanged upstream testcases per backend. Keep unchanged upstream source/BEAM and explicit startup selection. Remaining universal platform/resource/distribution/source-stack/Logger-metadata and full upstream-suite gates are documented in [the backend contract](supervisor-backends.md); full regression and canonical readiness remain outside this focused task.

Part580: review the selectable gen_event implementation and semantic constants PR; investigate the retained Managed depth3 zero-timeout upstream failure before merge. Primary105/105 per backend passes; no full regression/readiness refresh was run. [Scope and reports](validation/part-580/README.md).

## Executable queue

- Preserve [Part579 gen_statem](validation/part-579/README.md), its40-export [backend contract](gen-statem-backends.md),74 complete API observations and311 source-bound engine scenarios per implementation. Keep delegated-gen error origins and sys successor termination controls. Finish full upstream gen_statem_SUITE, owned-heap/resource, other-platform/distribution/global/via, actual Logger handler and metadata acceptance before claiming100%. Full regression/readiness remain excluded from this focused task.

- Preserve [Part578 timer acceptance](validation/part-578/README.md) and the [backend contract](timer-backends.md). The scoped PR tree builds normally over main without the unrelated draft ports. Keep semantic constant ownership and exact values/stacks/effects; inspect fresh PR checks/review. Full regression/readiness and platform/resource/distribution acceptance remain separate gates.

- Extend [ETS acceptance](validation/ets-port-20261104/README.md) beyond the accepted 355+8+12+75 observations per backend and bidirectional OTP dump/real DETS interoperability: full upstream ETS suite, concurrency/platform/resource/metadata and whole startup-manifest acceptance remain. Preserve focused checks, historical failures and canonical readiness; obtain an isolated tested publication snapshot after shared integration stabilizes.

- Preserve [Part566 gen_event acceptance](validation/part-566/README.md) and [backend ownership](gen-event-backends.md): 44 exports, 105 complete observations per backend, unchanged upstream fixtures and source/corpus/assembly admission. Extend full gen_event_SUITE, distributed naming/races, physical hibernation and metadata acceptance before a universal compatibility claim. Keep the shared BEAM send exception boundary and focused-only verification; do not refresh canonical readiness from partial reports.

- Preserve [Part565 timer acceptance](validation/part-565/README.md) and the [explicit backend contract](timer-backends.md):36 exports,728 primary observations per implementation,47 unchanged upstream tests and complete backtrace-depth controls. Keep malformed-state/call-failure-frame and reference/timing projection guards. Whole-repository build/publication is blocked by unrelated concurrent DETS draft compilation; global regression/readiness, distributed/resource/platform and load acceptance are separate gates.

- Close the [file runtime gates](file-backends.md): implement the missing prim_file NIF and prim_buffer resource operations and RAM driver with OTP error/ownership semantics, then independently replay raw, normal, RAM, consult and compressed cycles for both backends. Preserve the 408-row exact library corpus and authenticated reference/owner checks. Extend filesystem mutation, parsing/evaluation, compression, sendfile and platform acceptance; do not call 69 registered exports or the passing subset 100% compatibility. Keep verification focused as requested.

- Complete [code acceptance](validation/code-port-20261104/README.md) beyond its69-export implementation and156+25 same-platform observations per backend: full release selection/boot closure, documentation/debug-AST/gzip dependencies, filesystem failures, races and upstream code_SUITE. Preserve17 passing connected groups and the selected code:is_loaded import repair. No whole-module100% promotion from finite counts.

- Preserve the [DETS implementations](dets-backends.md) and focused OTP evidence. Extend selected upstream acceptance to remaining suite/resource/platform/interleaving and whole release-manifest startup gates. Keep C#/Erlang selection explicit; do not regenerate canonical readiness from a partial report.

- Preserve [Part563 Unicode acceptance](validation/part-563/README.md) and its explicit [backend contract](unicode-backends.md). Both 22-export alternatives pass the same 66,458 primary observations and 29 backing probes; keep the ordinary BEAM append, internal guest-function boundary and exact native Unicode capability controls. Global regression/readiness/publication are separate future gates; the present user explicitly requested only connected focused checks.

- Preserve [Part562 string acceptance](validation/part-562/README.md):71 source contracts, both37145-case audits,8522-case upstream-inclusive replays,138 depth observations per backend and9649 full tests. Maintain the exact source/assembly/provenance guards. Remaining whole-domain decimal/platform/metadata/resource acceptance and exact scoped publication/CI must not be replaced by corpus percentages.

- Maintain the [string backend acceptance](string-backends.md) through the shared differential command and both `HelloOtpString` selections. Track global regression/source-binding/publication gates separately; do not infer universal OTP or platform equivalence from finite corpus coverage.

- Preserve the accepted Part561 checkpoint:9647/9647 full tests,28 focused groups and8522 original cases in each string backend; readiness/source bindings are reconciled locally. Publication must bind the exact scoped commit and fresh CI/review while preserving unrelated shared map work. The earlier1328-row crash and rejected broad equality experiment remain immutable. See [current receipts](validation/part-561/README.md).

- Close the [ordsets acceptance gates](ORDSETS_COMPATIBILITY.md): shared term ordering/equality and `lists` helper closure, followed by complete regression and readiness publication. Preserve the exact OTP corpus and repaired native reverse source frames.

- Complete the [math backend acceptance gates](math-backends.md): platform numerical audit and complete regression/readiness publication. The 26 exports must not be presented as 100% semantic compatibility.

1. Read the [remaining-work index](remaining-work/INDEX.md), its linked checkpoint and the latest [changelog volume](../CHANGELOG.md). Resolve their recorded scope against the actual source and current task before implementation.
2. Select the next bounded task from that queue; retain its acceptance criteria, unresolved limitations and original-OTP comparison requirements.
3. Run checks appropriate to that task and record actual results and unrun gates in the changelog and owning plan.

The documentation split does not authorize publication, merge or a new runtime work part. Recorded historical CI and local results are evidence for their original revisions.

## Historical plans

All previous plans, including unfinished scope and handoffs, remain in [ordered historical volumes](history/next-steps/index.md). They preserve the source order and must be interpreted in their recorded checkpoint context. The old entry page started at Part485; consult the maintained queue for later work.

## Maintenance

Keep only current priorities and links here. Maintain detailed plans in remaining-work/, and record completed steps in the active changelog volume.

## erpc focused acceptance

Preserve all33 C#/original exports, explicit selection and the330-observation/8-connected-check receipt. Extend remaining upstream erpc_SUITE peer/old-release/EI, cross-platform, scheduling and resource acceptance before claiming universal100%. Keep the full regression/readiness outside this requested focused scope. Publication must isolate the erpc diff from unrelated concurrent library work. See [erpc contract](erpc-backends.md).
