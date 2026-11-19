# Receive marker audit and bounded compiler proposal

Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 was the oracle. The exact Part 1855 source archive SHA256 was 20D40EC3F43387696B7754118D767A1B569364491E80151896DB77EACD630979. Audit inputs were the 1,366 reference images and the saved uncached Part 1855 corpus. The compact decoder found no errors. No new compiler, Erlang source, test or corpus build was launched.

| Release marker inventory | References | Part 1855 generated |
| --- | ---: | ---: |
| Modules | 95 | 1 |
| Functions | 210 | 2 |
| Reserve | 172 | 2 |
| Bind | 172 | 2 |
| Use | 186 | 2 |
| Clear | 544 | 2 |

The one Equal marked module is observer with start_and_wait/0 and /1. The other 94 marked release modules are Different. Of 210 marked reference functions, 208 have missing marker opcodes. The reference signatures comprise 110 with all kinds, 36 reserve/bind/clear, 19 reserve/bind only, 17 use/clear, 5 use only and 23 clear only. Across markers with both producer and use in the same function, 38 functions in 17 modules have a make_ref/0 producer, 58 in 46 modules have a monitor producer, and 13 in 13 modules have a spawn_monitor producer. These bounds are from linear static pair association, not eligible route counts. Five reserve sites could not be assigned by the linear scan.

The exact carrier_info_receive/3 reference tests MsgCount =:= 0, clears x2 on that exit and allocates two slots on the active arm. It saves the Ref in y0 and HistStart in y1, calls carrier_info_receive_1/3, then clears y0 before constructing the result. The current C# output has one slot for HistStart, and neither clear is generated. For that path, carriers/1 reserves and binds before calling dispatch_gather/4. The Ref then reaches the wrapper and its helper through local call arguments. The helper executes recv_marker_use on the active arm and clears the final-count exit. allocations/1 and its receive wrappers have analogous marker traffic. The inventory includes nine marked instrument functions, including generated list-comprehension helpers.

Pinned beam_ssa_recv plan conducts a forward fixed point over fresh reference definitions, CFG edges and translated local calls, proves every accepting receive removes a message matched against the same reference, then restricts availability to reverse-reachable uses and inserts clears where a branch loses activity. Local arguments, child recursion, return translation and use/clear absence behavior are all needed for instrument. The source-level proposal deliberately leaves that module to this broader dependency. Adding a clear in one instrument function would fail the connected-dataflow proof.

The isolated candidate recognizes a resolved make_ref/0 assignment at the existing general call entry, a tracked straight-line prelude and one terminal infinite receive. A sole unguarded whole-reference or flat tuple pattern must use the existing fresh binding; the successful result is a value. The opaque reserve slot and Ref slot are allocated in normal frame bookkeeping. Reserve precedes the creator call. Bind can follow fresh, neutral assignments and resolved self/0. At a checked send it follows the shared operand preparation and precedes Send. An ordinary clobbering call ends the neutral scheduling region. Already-bound or repeated matches, dynamic call shapes, compound control flow, timeouts and any unmatched accepting clause fall back. use sits before the scan label, and clear sits on the matched edge before remove_message. Known receiver recursion and nonterminal onward marker lifetimes are deferred.

The existing monitored observer lowering retains its ordinary default. Source no_recv_opt and no_module_opt select the existing unmarked lowering. The dedicated no_recv_opt parser flag leaves no_ssa_opt and already configured profile options untouched. Pinned compile.erl stores source directives in effective #compile.options only. CInf and Dbgi's option lists come from #compile.mod_options. Dbgi still carries the literal source compile form. The new permanent check compares the decoded form and exact recorded option list through a release build profile.

Permanent tests cover a materialized payload and an inline send operand, preserving mailbox order and both exact and local-shadowed results, explicit opt-outs, an unsafe catchall and failing matches against an entry variable and a variable bound earlier in the prelude. The marker sequence assertion decodes actual Code instructions. Future interprocedural validation should include mark-free callers of the same helper, recursive receive count zero and one/many, intermediate wrong-reference notifications, timeout edges, protected raises and immediate monitor DOWN. Managed priority mailbox support needs native/managed parity separately because the legacy marker selection path can refuse a priority state.

The source-derived implementation uses existing project emitters, AST bindings and framed BEAM reader conventions. Pinned OTP supplies algorithm and instruction ordering evidence. The patch contains project-authored C# and no copied OTP code. See marker-corpus.json, producer-modes.json, raw-witness.log, the frozen function audit and fresh-reference-receive.patch for reproducible evidence. Exact release gains and test outcomes remain unmeasured until the isolated managed worker is released.

