# Branch-frame ownership and pre-clobber spill removal

The part 1832 saved output census pairs functions by module, name and arity. It compares the first allocation in physical code order when both images have one and its live X count agrees. This scope is broader than the first-opcode-mismatch rank. All 962 differing saved modules decode successfully.

| Diagnostic subset | Functions | Modules |
| --- | ---: | ---: |
| Same live X and excess Y slots in the first allocation | 15,492 | 926 |
| One candidate allocation | 14,706 | 912 |
| One allocation after a physical frameless return | 819 | 332 |
| Entry reload observed before the first ordinary call | 4,946 | 728 |
| Early tuple projection into Y | 3,641 | 650 |
| More than one handler | 343 | 201 |
| More than one trim | 110 | 82 |

The rows overlap. An observed instruction shape marks candidates for further proof. It does not establish a cause by itself.

Among 741 single-allocation functions that have a frameless return before the allocation and have no trim or exception handler, invoking the existing PlanEntryRegisterUses on saved code proves 116 removable incoming-argument saves in 93 functions across 67 modules. The existing late linear-suffix check refuses every one. 108 saves in 92 functions across 66 modules occupy exclusively removable slots in that saved image. The other slots may be allocated to later values.

OTP beam_ssa_pre_codegen:find_yregs reserves a Y value when it is consumed after a clobber. reserve_yregs filters values that are unused in the frame region, and frame_size counts actually assigned Y registers. The C# branch fallback reuses the existing disjoint-frame ownership analysis to prove that a reachable frameless return can bypass exactly one reachable frame without mixing different frame owners at a join. The existing entry register analysis decides whether a particular value needs a save. Trims and exception handlers remain outside the new fallback.

In the ten-module focused recompile, 4 modules remain Equal and 6 remain Different, with no Unsupported or SourceRejected. All six outputs changed. All 18 before, after and oracle images prepare in OTP. The preceding comparison completed in 9.617 seconds and the candidate comparison in 11.509 seconds with one worker on the shared host.

| Witness | Previous Y slots | Candidate Y slots | OTP Y slots | Live X |
| --- | ---: | ---: | ---: | ---: |
| format_lib_supp:get_format/2 | 2 | 1 | 1 | 2 |
| test_server:cast_to_list/1 | 1 | 0 | 0 | 1 |
| os:reverse_element/1 | 1 | 0 | 0 | 3 |
| snmpa_mib_data_tttn:handle_backup_res/2 | 1 | 0 | 0 | 2 |
| dist_util:dflag2str/1 | 1 | 0 | 0 | 1 |
| eunit_proc:scale_timeout/2 | 3 | 2 | 1 | 2 |
| dist_util:is_allowed/2 | 5 | 4 | 3 | 3 |

One fixture checks an early return with zero remaining saves, a positive with one live post-call save, a negative with two live saves and a negative protected by an exception handler. The permanent test fails at the new assertion on the predecessor and passes in the focused group on the candidate. All 12 native observations match previous and pinned OTP execution, including a caught error.

The remaining call-result spill before GC BIF in eunit_proc:scale_timeout/2 and ct_repeat:remaining_time/1 is a separate cause. The leading projection expansion in inet_tcp:do_connect2/3 and listen/2 is also separate. In both groups changing Y-slot coloring alone will not recover values whose lowering introduces longer lifetimes.

See inventory.json, proof.json, proof-exclusive.json, before.json, after.json, native *.log and *.terms, and owned-branched-entry-spills.patch. inventory/Program.cs and proof/Program.cs contain the read-only analyzers. Full corpus compilation and full regression are outside this isolated checkpoint.
