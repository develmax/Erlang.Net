# Owned case results across an ordinary call

The official Windows OTP 29.1.1 is pinned to ad05823719d77c8faee87348ea39513d4e2f99c5. The C# image came from uncached Part 1970 shard 3. The compiler assembly SHA-256 is 048299c0e2953b3998f9f62a3982e9b37f67c0c5bbb39545c9e370699c956001. Both image and section hashes are in sections.json.

| Residual | OTP | C# | Cause |
| --- | --- | --- | --- |
| Code | 955 bytes | 932 bytes | Only connect1/8 differs after function-local label normalization |
| ExpT | 52 bytes | 52 bytes | module_info/0 address 23 to 22 and module_info/1 address 25 to 24 |
| LocT | 28 bytes | 28 bytes | get_username_and_password/1 address 19 to 18 |
| Attr | 39 bytes | 39 bytes | Sole vsn attribute equals essential section MD5 for both images |

Atoms, imports, literals, types and line chunks are byte-equal. Export and local table keys, row counts and order match. Code headers advertise instruction set 0, maximum opcode 181 and six functions, with 26 versus 25 labels including the sentinel. functions.terms preserves lines, registers and all non-label operands. Five of six functions match after normalizing local label numbers. connect1/8 has 187 versus 186 decoded instructions and allocate 8,8 in both. Earlier Part 1970 native unix-telnet-list-patterns.stdout.log includes returned values, calls, log ordering, fallback paths and line 134 badmatch.

| Priority | Mechanism | Proved release reach | Reference |
| --- | --- | --- | --- |
| 1 | Case result owns one stack slot on normal incoming edges | Three functions across three Different modules | beam_ssa_pre_codegen.erl 1938-2042 and beam_ssa_codegen.erl 2556-2564 and 2613-2646 |
| 2 | Shared two-success exit and test inversion | One proved connect1/8 site | beam_jump.erl 146-164, 333-410, 635 and 752-760 |
| 3 | Save whole failing selector while using the still-valid X projection | Several connect1/8 sites | beam_ssa_pre_codegen.erl find_yregs and beam_ssa_codegen.erl prefer_xregs |
| 4 | Stack-root initializations and call argument order | Several connect1/8 sites | beam_ssa_codegen.erl 2653-2731 and later physical layout |
| 5 | Version and table address derivatives | Three table row values and code label fence | beam_asm.erl 229, 305 and 387-409 |

Pinned source lib/common_test/src/unix_telnet.erl 125-175 yields precodegen block 100 with y7/_18 = phi { y4/_32,^124 }, { x0/_87,^137 }, { x0/_90,^148 }, { y4/_32,^153 }, { x0/_93,^174 }, { y4/_32,^181 }. Line 174 calls ct_telnet:end_gen_log/0 then returns Y7. The official image writes Y7 on five physical incoming paths. The final jump optimizer merges two success paths at label 12. C# writes X0 at each logical exit then saves to Y7 at label 14. The shared success label, selector scheduling and liveness differences remain independent of the per-edge transfer.

Reading abstract forms from 1,366 pinned BEAMs found exactly three terminal R = case ..., F(), R functions. Both physical images independently confirm each site.

| Function | OTP result and frame | C# result and frame | Effect |
| --- | --- | --- | --- |
| unix_telnet:connect1/8 | Y7, 8 | Y7, 8 | ct_telnet:end_gen_log/0 line 174 |
| mnesia_schema:read_schema/2 | Y1, 2 | Y1, 2 | unlock_schema/0 line 535 |
| snmpa_target_cache:get_targets/2 | Y1, 2 | Y2, 4 | unlock/0 line 133 |

The SNMP frame has an additional allocation delta. The exact source cohort has zero currently equal release hits. Five already matching functions in unix_telnet are cheap decoded controls. Full-file exact controls and raw sections require a built candidate.

## P1973 isolated draft

production.patch strictly replays against SHA-256-verified Part 1970 compiler preimages. tests.patch strictly replays against the frozen test registry and adds a permanent structural regression. Roslyn parsed all five touched files without diagnostics.

The draft recognizes a fresh named match, a case of at least two clauses, an ordinary zero-argument next call and the direct return of the matched name. Optimization-disabled, guarded and receive scratch contexts retain the existing path. The normal decision and branch emitters record successful arm copies at instruction boundaries. Nested case results reuse the outer plan. The owned register is allocated after all arms at the existing result reservation point. This preserves the original scratch-slot numbering as arms are planned. The existing instruction serializer inserts edge moves while it resolves label operands, before frame and control-flow analysis reads the stream. A Y-to-same-Y edge requires no instruction. Specialized constructor paths keep their emission and record where the result is ready. Early slot reservation was dropped because the one-slot nested fixture demonstrates overlap between the selector and result.

The pinned owned_simple/1 fixture puts two writes to Y0 on incoming edges, in a one-slot frame with one effect. Frozen P1970 has zero writes before the final join and one after it. The structural gate in red-check.terms passes under pinned OTP and records the red result on frozen P1970. The observed_join/1 control deliberately reads the binding in an intervening effect, so this bounded source change retains its existing C# path.

The behavioral fixture ran 14 scenarios in native OTP with each of the pinned and frozen P1970 BEAMs. A failing selector reports {case_clause,no_match} at line 66 with no sink call. A failing arm reports {rejected,no_match} at line 76 with no sink call. A post-join external effect faults back through owned/1 at line 12. The native receipts retain both stacks and instructions.

The next SDK lane should build the candidate, run the new structural check, load and exercise the three release targets and run the 14 controlled source paths. Check per-edge placement, final frame width, physical Line and raw sections. The patched compiler has no measured module gain yet. Remaining connect1/8 changes include selector scratch, stack kills and the shared success exit.
