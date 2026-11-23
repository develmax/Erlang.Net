# A zero-slot ordinary call after genuine unframed entry checks

The production and permanent test diffs in this directory are source-only. The Part 1973 exact edge retake compiler is F685472E21EDDB969DC244FA620539FF5D145C8D267B0F8E9486A7F30DAD2476. Its LeafFrameRegisters source closure identifies SHA-256 8282a46eb52da44d270942de5a78369210d1dba0d5c97b2c4ad3c9ac7dbd2bd2. The staged P1974 replay preserves the Part 1973 IReadOnlyList signature through one patch-context adaptation. No source behavior was changed by the adaptation. Both P1975 patches strictly replay against the documented inputs and Roslyn parsed the three C# files without diagnostics.

## Measured scope

A bounded search took the 232 Part 1971 functions with the same ordinary call target as pinned and a zero-width pinned frame. The leaf planner's previous linear cohort was 31 functions across 25 modules. The following checked shapes have independent successful fallthrough and a separate failure edge.

| Shape | Functions | Modules |
| --- | ---: | ---: |
| Function-info rejection before one frame in frozen P1971 | 18 | 12 |
| Same checked ownership on pinned and frozen | 17 | 11 |
| Unframed cold local return instead of function-info rejection | 2 | 2 |
| Active frame and local badrecord failure | 1 | 1 |
| Mixed local failure and deallocation layout | 1 | 1 |
| Matching zero-slot checked controls in existing Equal BEAMs | 760 | 21 |

Physical candidate prefix and root liveness still govern admission. The 17 paired witnesses include ct_logs:date2str/1, edoc_lib:timestr/1, calendar:now_to_local_time/1 and ordinary local graph wrappers. graph.beam and digraph_utils.beam account for eight observed functions. global:gns_volatile_multicast/5 has a distinct pinned schedule and should be treated as a control. The cold cases systools_lib:get_path/1 and dbg_wx_filedialog_win:format_date/1 have their own unframed local return and are deferred. The active-frame eunit_serial:cast/2 badrecord has a different pinned shape and is deferred.

date2str.p1973-exact.terms confirms the same residue on a released module from the exact P1973 retake. The entry tests and X-only outer projections precede frame allocation. The pinned BEAM has allocate_heap with width zero and uses width zero at the final external tail. The P1973 BEAM has allocate width six, six saved projections and tail width six. It preserves the same success target and genuine source line. The original specimen's pinned phase is in the P1974 audit ct_logs.erl.precodegen. beam_ssa_pre_codegen need_frame_1 at 1566-1597 places a continuation frame for the ordinary call. frame_size_1 at 2756-2772 sizes it from Y definitions. These checked entry instructions belong to the unframed region and preserve their failure operands.

## P1975 draft and checks

The checked prefix can now use the existing FindEntryFrameAllocation proof with unframed projections. The probe requires proof of the original function-info destination and a forward success path to the same sole allocation. Each accepted check stays before the allocation. Its failure keeps the original function arguments in the existing leaf dataflow proof. Existing root, register-interference, single ordinary call and stack-cleanup checks remain mandatory. Local failure labels, checked framed paths and values surviving the call in Y retain their prior ownership. The rewrite keeps all lines and does not prescribe a new heap-allocation schedule.

Two source positive probes differ physically on the exact P1973 binary. nested/1 has six frame slots and checked_closure/1 has one, while pinned has zero in both cases. Their failures target the function-info label before allocation on both. The permanent test expects the zero-sized allocate and tail operand on both. The retains/2 negative requires a genuine Y use after the call. cold_result/1 requires a separate direct local error route. Both raw listings and fail stacks are retained.

Nine native scenarios pass on pinned and exact P1973 fixtures with the same values, function_clause arguments and error locations. They include outer constructor misses on either side of a nested tuple, list guard fallback, and a retained value used after a call. The four function_clause witnesses agree at source lines 4, 7 and 11. The candidate compiler has not been rebuilt. Build, semantic red/green on that candidate, raw release function gains and current exact-image retention are pending.

The retained module scope is a subset of Part 1971 for census and controls with one Part 1973 release witness and P1973 fixture verification. A wider current corpus claim requires the completed exact P1973 or P1974 images.
