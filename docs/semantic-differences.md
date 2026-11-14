Part 1687 shares arity and tag checks across consecutive cons patterns with the same tuple shape. [Evidence](validation/part-1687/README.md).

Part 1686 preserves projection capture order when structural tuple fields reuse an earlier register. [Evidence](validation/part-1686/README.md).

Part 1685 replaces dead parent tuples with their final projections in structural list matching. [Evidence](validation/part-1685/README.md).

Part 1684 skips unused tuple fields and releases temporary registers after literal tests. [Evidence](validation/part-1684/README.md).

Part 1683 reuses the source register for the list tail when the cons fallback covers all nonempty inputs. [Evidence](validation/part-1683/README.md).

Part 1682 allocates frames after the type test when each branch constructs a cons result from a call. [Evidence](validation/part-1682/README.md).

Part 1681 uses a boolean comparison for lists:member/2 and lists:keymember/3 predicates in closed comprehensions. [Evidence](validation/part-1681/README.md).

Part 1680 keeps case selector call results in X0 when both branches end in tail calls and need no saved input. [Evidence](validation/part-1680/README.md).

Part 1679 uses an external tail call without allocating a stack frame. [Evidence](validation/part-1679/README.md).

Part 1678 uses an external tail call without allocating a stack frame. [Evidence](validation/part-1678/README.md).

Part 1677 uses an external tail call without allocating a stack frame. [Evidence](validation/part-1677/README.md).

Part 1676 uses an external tail call without allocating a stack frame. [Evidence](validation/part-1676/README.md).

Part 1675 keeps the complete record in one slot until the field is needed. [Evidence](validation/part-1675/README.md).

Part 1674 keeps a list element returned by a call in X0 until the list is built. [Evidence](validation/part-1674/README.md).

Part 1673 keeps the guarded selector value in X0 and saves one fallback slot. [Evidence](validation/part-1673/README.md).

Part 1672 keeps the unmatched selector value in X0 through the shared fallback dispatch. [Evidence](validation/part-1672/README.md).

Part 1671 keeps the GC BIF operands and returned tuple fields in X registers. [Evidence](validation/part-1671/README.md).

Part 1670 skips unused heads in fixed-length list patterns. [Evidence](validation/part-1670/README.md).

Part 1669 skips unused list tails and uses X0 directly to return a matched tuple field. [Evidence](validation/part-1669/README.md).

Part 1668 removes the nil test when the nonempty-list test proves the argument is empty. [Evidence](validation/part-1668/README.md).

Part 1667 chooses a surviving slot using its index in the current frame. [Evidence](validation/part-1667/README.md).

Part 1666 stages a saved receiver before loading integer and list arguments. [Evidence](validation/part-1666/README.md).

Part 1665 stages a saved receiver before loading integer and list arguments. [Evidence](validation/part-1665/README.md).

Part 1664 loads the receiver before nil after a spill. [Evidence](validation/part-1664/README.md).

Part 1663 trims intermediate frames by moving a live slot. [Evidence](validation/part-1663/README.md).

Part 1662 recognizes nested literal tuple patterns before record calls. [Evidence](validation/part-1662/README.md).

Part 1661 shares identical local tail calls reached from literal list heads. [Evidence](validation/part-1661/README.md).

Part 1660 matches argument ordering when saving a previous call result. [Evidence](validation/part-1660/README.md).

Part 1659 keeps the previous call result in X0 until the list prefix has been built. [Evidence](validation/part-1659/README.md).

Part 1658 constructs short list prefixes without calling ++. [Evidence](validation/part-1658/README.md).

Part 1657 checks nested record tags before allocating the frame. [Evidence](validation/part-1657/README.md).

Part 1656 preserves the returned tuple operands across multiple stack slot relocations. [Evidence](validation/part-1656/README.md).

Part 1655 delays loading the first call argument until after frame trimming. [Evidence](validation/part-1655/README.md).

Part 1654 relocates a still-live tagged payload before trimming the frame. [Evidence](validation/part-1654/README.md).

Part 1653 uses function_clause for record parameters that fail their head pattern. [Evidence](validation/part-1653/README.md).

Part 1652 shares a badrecord branch for calls that require the same record. [Evidence](validation/part-1652/README.md).

Part 1651 checks the record before allocating a frame to retain it across a call. [Evidence](validation/part-1651/README.md).

Part 1650 uses the existing closure emitter and tail-call instructions for single closure arguments. [Evidence](validation/part-1650/README.md).

Part 1649 shares boolean returns for ordered disjoint inclusive ranges. [Evidence](validation/part-1649/README.md).

Part 1648 shares guarded integer returns with equivalent literal alternatives. [Evidence](validation/part-1648/README.md).

Part 1647 merges duplicate local tail calls. [Evidence](validation/part-1647/README.md).

Part 1646 uses a shared miss result after the nil check. [Evidence](validation/part-1646/README.md).

Part 1645 merges identical unframed return blocks. [Evidence](validation/part-1645/README.md).

Part 1644 keeps list-head guard alternatives and recursive calls in registers. [Evidence](validation/part-1644/README.md).

Part 1643 shares the replacement body between disjoint list-head keys. [Evidence](validation/part-1643/README.md).

Part 1642 reuses the stack slot after the final read in a constructed argument. [Evidence](validation/part-1642/README.md).

Part 1641 reserves heap jointly and avoids saving intermediate constructors to Y registers in supported tail continuations. [Evidence](validation/part-1641/README.md).

Part 1640 eliminates an extra frame slot when one projected field feeds a call and only the original tuple survives it. [Evidence](validation/part-1640/README.md).

Part 1639 skips closed unused values when planning case entry and result arms, enabling a tail call directly from the selector register. No release image changed equality status. [Evidence](validation/part-1639/README.md).

Part 1638 matches mixed tuple and atom dispatch in the supported parameter shapes. The complete httpd_file release image now matches OTP. [Evidence](validation/part-1638/README.md).

Part 1637 matches the Line section in httpd_file and aligns handle_error/5 after label normalization. The module still differs in Code and other sections. [Evidence](validation/part-1637/README.md).

Part 1636 matches single-slot frames for nested append chains. The release images are identical to part 1635. [Evidence](validation/part-1636/README.md).

Part 1635 reduces redundant line instructions around straight-line append expressions. httpd_file still differs in frame generation and dispatch. [Evidence](validation/part-1635/README.md).

Part 1634 matches append destination registers in httpd_file. Stack frame and line metadata still differ. [Evidence](validation/part-1634/README.md).

Part 1633 matches OTP lowering for short proper literal append prefixes. httpd_file still differs in the surrounding frame and call sequence. [Evidence](validation/part-1633/README.md).

Part 1632 aligns the singleton list branch frame in Observer start/1, completing release image equality for Observer. [Evidence](validation/part-1632/README.md).

Part 1631 aligns the guarded monitored receive in Observer. Its start/1 still differs in entry dispatch and frame placement. [Evidence](validation/part-1631/README.md).

Part 1630 aligns the monitored receive after a checked setup call. The guarded Observer entry path still needs the same frame plan. [Evidence](validation/part-1630/README.md).

Part 1629 aligns repeated source locations within discarded constructor calls. The Windows release images are identical to part 1628. [Evidence](validation/part-1629/README.md).

Part 1628 drops unused tuple and list construction and aligns snmpa_app:convert_config/0 after label normalization. Multiple calls on one line can still emit duplicate line instructions. [Evidence](validation/part-1628/README.md).

Part 1627 emits erlang:hibernate/3 in tail position after a receive timeout. The release trace cleaner still differs in entry dispatch and frame placement. [Evidence](validation/part-1627/README.md).

Part 1626 aligns constant list suffix emission and records nine new normalized instruction matches and no losses. Nested constructor scheduling can still differ. [Evidence](validation/part-1626/README.md).

Part 1625 aligns wildcard tail calls and records 4 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1625/README.md).

Part 1624 aligns inline record replacement calls in a permanent fixture. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1624/README.md).

Part 1623 aligns record entry frame placement and records 0 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1623/README.md).

Part 1622 aligns dead call result storage and records 0 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1622/README.md).

Part 1621 aligns wildcard match storage and records 2 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1621/README.md).

Part 1620 aligns heap reservations for wrapped record updates and records 0 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1620/README.md).

Part 1619 aligns deferred tuple projections and records 1 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1619/README.md).

Part 1618 aligns literal case arms after saved calls in a permanent fixture. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1618/README.md).

Part 1617 aligns terminal call matches and records 8 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1617/README.md).

Part 1616 aligns terminal call matches and records 29 new normalized instruction matches and 0 losses. Remaining release images are tracked in the corpus report. [Evidence](validation/part-1616/README.md).

Part 1615 aligns three more functions after label normalization. The affected release images still differ. [Evidence](validation/part-1615/README.md).

Part 1597 aligns allocate in the lifted normalize closure in xmerl_xpath_pred. Whole-module differences remain. [Evidence](validation/part-1597/README.md).

Part 1596 aligns allocation operands in thirteen release functions. Whole-module differences remain.

Part 1595 matches the complete erl_abstract_code release image. [Evidence](validation/part-1595/README.md).

Part 1594 aligns atom dispatch in erl_abstract_code:debug_info/4. Register allocation still differs. [Evidence](validation/part-1594/README.md).

Part 1593 removes unused fields in erl_abstract_code:debug_info/4. Atom dispatch and register allocation still differ. [Evidence](validation/part-1593/README.md).

Part 1591 closes the wx_misc frame relocation gap and matches the full release image.

Part 1590 aligns the checked wx_misc tuple projection. Stack relocation and trim instructions still differ. [Evidence](validation/part-1590/README.md).

Part 1589 matches the wx_misc wrapper instructions. The two-argument function still differs in projection and stack-frame trimming. [Evidence](validation/part-1589/README.md).

Part 1588 aligns 33 functions after label normalization. The complete release images still differ. [Evidence](validation/part-1588/README.md).

Part 1587 aligns two megaco_stats functions after label normalization. The complete release image still differs.

Part 1586 reduces the Code size gap in six release images. Their byte comparisons still differ. [Evidence](validation/part-1586/README.md).

Part 1585 reduces the Code size gap in erl_tar and string. Both release images still differ. [Evidence](validation/part-1585/README.md).

Part 1584 reduces the Code size difference in ct_netconfc and diameter_dbg. Both complete images still differ. [Evidence](validation/part-1584/README.md).

Part 1583 matches the direct guarded split fixture. The rule changed zero release images. [Evidence](validation/part-1583/README.md).

Part 1582 aligns the guarded body match entry opcode. Frame allocation and capture registers still differ. [Evidence](validation/part-1582/README.md).

Part 1581 aligns the normalized instructions in ct_netconfc:trim/1. The full image still differs. [Evidence](validation/part-1581/README.md).

Part 1580 removes the frame from ct_netconfc:trim/1. The match entry and alignment still differ. [Evidence](validation/part-1580/README.md).

Part 1579 matches the fixed binary tuple fixture. The optimized shape changed zero release images. [Evidence](validation/part-1579/README.md).

Part 1578 aligns the literal equality operands in the fixed binary prefix fixture. Frame allocation and capture registers still differ. [Evidence](validation/part-1578/README.md).

Part 1577 aligns the normalized instructions in diameter_codec:hop_by_hop_id/2. The full release image still differs. [Evidence](validation/part-1577/README.md).

Part 1576 aligns two normalized functions by removing redundant matched literal loads. The two release images still differ. [Evidence](validation/part-1576/README.md).

Part 1575 aligns the frame and tail argument preparation in erts_trace_cleaner:call_check/0. The release image still differs. [Evidence](validation/part-1575/README.md).

Part 1574 aligns heap allocation and register use for the measured tuple send. The full asn1_db image still differs. [Evidence](validation/part-1574/README.md).

The C# source compiler has deterministic BEAM output for supported returns, calls, constructors, literals and patterns. Explicit release profiles can reconstruct supported module metadata and debug forms for constant bindings and nested tuples, lists, and maps. Windows x64 uses the pinned OTP compressor. Other platforms keep the existing ETF compressor. Rich documentation, type metadata, full preprocessing, general lowering and broader release reconstruction remain open. [Evidence](validation/part-606/README.md).

Part 1572 matches the shared fallback layout in the tagged try fixture. The release corpus remains at 390 exact matches. [Evidence](validation/part-1572/README.md).

Part 1571 aligns six protected tuple returns in prim_inet. The full release image still differs. [Evidence](validation/part-1571/README.md).

Part 1570 matches use of X1 in simple catch result tuples. Return layout and stack frames still differ. [Evidence](validation/part-1570/README.md).

Part 1569 aligns the simple catch class test and raw rethrow. The remaining reason loads and return layout can still differ. [Evidence](validation/part-1569/README.md).

Part 1568 removes unused catch stacktraces from general try lowering. Catch register placement and branch layout still differ. [Evidence](validation/part-1568/README.md).

Part 1567 folds the fixture tuple field to a literal. The release corpus remains at 390 exact matches. [Evidence](validation/part-1567/README.md).

Part 1566 matches six normalized functions for single-call cases. The complete images still differ. [Evidence](validation/part-1566/README.md).

Part 1565 matches 36 normalized functions by planning records with call results. The complete images still differ. [Evidence](validation/part-1565/README.md).

Part 1564 aligns seven normalized functions after enabling saved-call planning for constant returns. The complete images still differ. [Evidence](validation/part-1564/README.md).

Part 1563 aligns move ordering in expandable_term/4. The full observer_html_lib image still differs. [Evidence](validation/part-1563/README.md).

Part 1562 matches the frame and swap in expandable_term/4. The order of independent moves still differs. [Evidence](validation/part-1562/expandable_term.txt).

Part 1561 aligns reuse of a dying slot in global:delete_global_lock/2. The full global image still differs. [Evidence](validation/part-1561/README.md).

Part 1560 aligns a register-only tail after a linear call prefix. The full test_server_ctrl image still differs. [Evidence](validation/part-1560/README.md).

Part 1559 folds constant wildcard record constructors inside call arguments. Six normalized functions match after the change. Their full module images still differ. [Evidence](validation/part-1559/README.md).

Part 1558 carries the lookup result type into single tagged tuple projections. The full pubkey_cert image still differs. [Evidence](validation/part-1558/README.md).

Part 1557 emits the lookup result union for local-call tuple tests. Single tagged projections can still omit the operand type. [Evidence](validation/part-1557/README.md).

Part 1545 aligns the measured frame layout and if_end in prim_buffer. The remaining disassembly differences are typed operands. [Evidence](validation/part-1545/README.md).

Part 1530 matches three normalized function bodies. The stack-free throw rewrite still differs in snmpc:get_info/2. [Evidence](validation/part-1530/README.md).

Part 1529 matches saved operand types and normalized instructions in both Windows net lookup helpers and three other functions. Their complete release images still differ. [Evidence](validation/part-1529/README.md).

Part 1528 matches normalized instructions for compress_data/3 in disk_log_1 and logger_std_h. Both modules still differ as complete images. [Evidence](validation/part-1528/README.md).

Parts 1526 and 1527 match 21 additional normalized function bodies. The Windows OTP corpus still contains 979 differing images. [Evidence](validation/part-1527/README.md).

Part 1525 removes nine oversized frames and matches normalized instructions for dialyzer_dep:map__lookup/2. Typed exact comparisons and known atom alternatives still differ in several changed functions. [Evidence](validation/part-1525/README.md).

Part 1524 matches normalized instructions for erl_compile_server:get_path_arg/2. The module still differs and its Code chunk grew by six bytes. The full corpus remains at 979 differing images. [Evidence](validation/part-1524/README.md).

Part 1523 matches normalized instructions for find_and_remove_attribute/3 in both xmerl DOM modules. Their remaining functions still differ. The complete corpus has 979 differing images. [Evidence](validation/part-1523/README.md).

Part 1495 restores the frame size in the two release functions whose saved slots were reused in Part 1494. The entry tuple stays intact across calls in OTP's `dbg_ieval:rec_clauses/3` and `erl_eval:match_clause/5`; the C# entry projects its fields early and now needs an extra slot. [Release instructions](validation/part-1495/cases-after.txt) · [Frames](validation/part-1495/frame-delta.txt).

Part 1494 restores normalized instruction equality for `test_server_node:start_which_node/1`. Retaining the outer field by reusing a saved slot leaves frame differences in two release functions and the retained-field control. [Release instructions](validation/part-1494/identity-release.txt) · [Frames](validation/part-1494/frame-delta.txt).

Part 1493 matches nine more nested-case instruction bodies after label normalization. The new path emits an unnecessary selector reconstruction in `test_server_node:start_which_node/1`, which previously matched. [Release instructions](validation/part-1493/cases-after.txt).

Part 1492 matches the instructions for `xmerl_xlate:file/3`. The other differences in that module include closure and list construction in `string/3`, and frame placement and checked tuple projection in `hook/2`. Two frames elsewhere now use one fewer slot than OTP. [Release instructions](validation/part-1492/cases-after.txt) · [Frame changes](validation/part-1492/frame-delta.txt).

Part 1491 extends register projection to saved-selector arms before a continuation. The control still differs in guard field projection, the final result join and frame size. [Instruction comparison](validation/part-1491/probe.txt).

Part 1490 projects saved tuple fields in X for flat case arms with eligible branch results. Exact bound-field checks, guard paths, and result construction after the case still differ in the controls. [Instruction comparison](validation/part-1490/probe.txt).

Part 1489 dispatches on a saved tuple in Y and preserves the original selector on failure. The control still differs in payload register allocation and return construction. [Instruction comparison](validation/part-1489/probe.txt).

Part 1488 selects tuple arity before tag and keeps the second argument tests in their branches. `ct_ssh:handle_msg/2` now has the same number of arity and value tables as OTP. It still repeats more tagged tests and projections. [Dispatch counts](validation/part-1488/ct-ssh-dispatch.txt).

Part 1487 selects distinct tuple tags after a common arity test. The control still differs in payload projection order and the target of the failed guard. [Instruction comparison](validation/part-1487/probe.txt).

Part 1486 shares the tuple check and field selector for consecutive eligible clauses. In `tuple_alias/2`, OTP reuses the projected value after the failed guard, while C# rechecks the later clause from the original argument. [Instruction comparison](validation/part-1486/probe.txt).

Part 1485 matches the entry selector and constant branches in `ssh_bits:mpint/1`. Its positive and negative branches still differ in binary instructions, register types, and frame sizes. [Instruction comparison](validation/part-1485/ssh-bits-mpint.txt).

Part 1484 checks tuple aliases and literal fields in entry registers. In `tuple_alias/2`, OTP shares the tuple check and selects the field with `select_val`. C# checks each clause in source order and allocates only in the branch with a non-tail call. [Instruction comparison](validation/part-1484/probe.txt).

Part 1483 applies branch-local frame planning to tuple-pattern clauses with sequences and decisions. In `tuple_entry/2`, both compilers use a two-slot frame. C# projects the field into X before allocation. OTP allocates first and projects the field directly into Y. [Instruction comparison](validation/part-1483/probe.txt).

Part 1482 emits supported entry type and equality tests before branch-local allocation when the function has variable-only parameters. The `guarded_shape/2` fixture reaches normalized instruction equality. [Instruction comparison](validation/part-1482/probe.txt).

Part 1481 projects the needed `split_binary/2` field from X in `prim_buffer:read_iovec/2`. The C# main frame uses five slots and OTP uses four. Remaining differences include the projection destination, heap check placement, and branch-local frame trimming. [Instructions](validation/part-1481/prim-buffer-read_iovec.txt).

Part 1480 emits byte_size from X and an arithmetic guard comparison from X in prim_buffer read_iovec/2. Remaining differences include type annotations, pair extraction and stack trimming in the large-size branch. [Instructions](validation/part-1480/prim-buffer-read_iovec.txt).

Part 1479 matches prim_buffer read/2 instructions and the entry and small-size path of read_iovec/2. The large-size path still uses seven frame slots compared with four in OTP. [Comparisons](validation/part-1479/README.md).

Part 1478 matches the erl_tracer release image, including documentation signatures and private type visibility. The prim_buffer and zlib on_load/0 instruction streams match OTP, with other function and chunk differences remaining. [Evidence](validation/part-1478/README.md).

Part 1477 matches the complete xmerl_xsd_re map/1 disassembly. The module still differs in chr/2 and scan/2. [Instructions](validation/part-1477/map-selector-comparison.txt) and [remaining functions](validation/part-1477/xmerl-functions-after.txt).

Part 1476 reaches the zero-slot OTP frame in xmerl_xsd_re map/1. Field projection and tail error call placement still differ. [Comparison](validation/part-1476/README.md).

Part 1475 reduces the xmerl_xsd_re map/1 frame from two slots to one. OTP uses no slots and emits shared tuple field projection and a terminal error call. [Before](validation/part-1474/map-selector-before.txt) and [after](validation/part-1475/map-selector-after.txt).

Part 1474 matches the wxDisplay unary constructor dispatch, stack allocation, register moves and error branches. The resulting release file is byte-identical. [Evidence](validation/part-1474/README.md).

Part 1473 matches the one-slot frame in the guarded preserve/2 control. The guard operand type and shared return remain different. The survive/2 control retains the selector across put/2 and uses three slots compared with two in OTP. [Evidence](validation/part-1473/README.md).

Part 1472 matches eight public_key instruction bodies and removes three immediate result spills. The changed release frame sizes stay the same. The bound control still needs two slots compared with one in OTP. [Evidence](validation/part-1472/README.md).

Part 1471 removes the immediate result spill from ten functions and reaches the OTP frame size in each one. crypto macN/5 matches after label normalization. The public_key encoders still differ in the proper-list operand type. All complete file statuses stay the same. [Evidence](validation/part-1471/README.md).

Part 1470 restores six normalized release function bodies. Thirteen changed frame sizes move farther from OTP, while the aggregate Code size gap drops by 83 bytes. The complete file comparison statuses stay the same. [Evidence](validation/part-1470/README.md).

Part 1469 reaches the OTP entry list test in 709 functions. One body gains complete normalized equality and six lose it because of register reuse, branch destinations or closure argument ordering. All file comparison statuses stay the same. [Evidence](validation/part-1469/README.md).

Part 1468 matches seven release bodies of unused_bitlist/3 after label normalization. Their complete module images still have other instruction and metadata differences. [Evidence](validation/part-1468/README.md).

Part 1467 matches the frame sizes of seven release functions. The unused_bitlist/3 body still differs in proper-list and numeric facts derived from callers. [Evidence](validation/part-1467/README.md).

Part 1466 matches the frame sizes and normalized instructions of 14 closure dispatch functions. The nonempty list frame cohort remains at 530 modules. [Evidence](validation/part-1466/README.md).

Part 1465 reduces projection candidates to 1,805 functions in 556 images. The complete normalized function match count stays the same. Frame allocation timing and register assignments remain open. [Evidence](validation/part-1465/README.md).

Part 1464 reduces remaining projection candidates to 2,268 functions in 606 images. Complete normalized instruction matches remain unchanged. Two shorter wx examples move three bytes farther from the reference Code size. [Evidence](validation/part-1464/README.md).

Part 1463 reduces the Code gap by 1,749 bytes. Frame allocation sizes remain unchanged. The most common residual projection pattern has a subsequent move in 401 images. [Evidence](validation/part-1463/README.md).

Part 1462 aligns 17 frame sizes. Move sequences following field extraction remain in 403 candidate images, and some terminal tagged clauses still perform a separate argument move. [Evidence](validation/part-1462/README.md).

Part 1461 shows that the AST inliner and OTP Core inliner make different decisions at local call sites. Applying the release option with the current AST heuristic increased the Code gap by 121,466 bytes across 56 changed images. [Evidence](validation/part-1461/README.md).

Part 1460 restores direct fold calls across most of the corpus. Closure index order still differs in ct_framework:save_seq/4. The HTTP callback retains extra frame storage and untyped result operands. [Evidence](validation/part-1460/README.md).

Part 1459 aligns two record dispatch bodies and three frame sizes. The HTTP fold callback frame and erl_lint:get_doc_attr/2 record and key proofs remain different. [Evidence](validation/part-1459/README.md).

Part 1458 aligns 24 normalized bodies and 13 frame sizes. Record field dispatch such as httpc_manager:get_option/2 still uses a frame and individual key tests. [Evidence](validation/part-1458/README.md).

Part 1457 aligns 33 normalized function bodies and 32 frame sizes. DNS cases whose guards read an outer input through a wildcard and equality alternatives in SSL still differ. [Evidence](validation/part-1457/README.md).

Part 1456 brings 384 functions to normalized instruction equality. The affected parser images retain differences in other functions and metadata. [Evidence](validation/part-1456/README.md).

Part 1455 aligns ten erl_internal functions after label rebasing. The release image still differs in remaining functions and metadata. [Evidence](validation/part-1455/README.md).

Part 1454 reaches byte equality for erl_posix_msg.beam. The optimized test_server_sup:check_purge/1 fallback still emits throw/1 where OTP emits raise/3. [Evidence](validation/part-1454/README.md).

Part 1453 aligns the parent and helper of asn1ct:remove_asn_flags/1 and erl_abstract_code:delete_reports/1 after label normalization. The erl_abstract_code:debug_info/4 body still differs. [Evidence](validation/part-1453/README.md).

Part 1452 aligns fifty-nine mapper helpers after label normalization. In asn1ct:compile_set/3 the frame drops from twelve slots to seven, compared with five in OTP. The helper retains an improper-list branch that OTP omits. [Evidence](validation/part-1452/README.md).

Part 1451 aligns the instruction bodies of thirteen tuple projection helpers after label normalization. compile.beam still differs and has two newly emitted comprehension helper names absent from the release. [Evidence](validation/part-1451/README.md).

Part 1450 aligns fifteen comprehension parent instruction sequences after label normalization. The new ASN.1 helpers allocate eleven slots where OTP allocates four, with differences in exception handling and line emission. [Evidence](validation/part-1450/README.md).

Part 1449 aligns the three field destinations in mod_head:do_head/1. It still reserves eight frame slots compared with four in OTP. The checked-fields fixture also retains the explicit frame and interleaved record checks, while OTP keeps both original inputs in X registers and branches to failure blocks after the call. [Evidence](validation/part-1449/README.md).

Part 1448 aligns the destination of the record field in mod_head:do/1. Register allocation and instruction order still differ. Every differing release image has an unequal Code chunk and at least three more unequal chunks. [Evidence](validation/part-1448/README.md).

Part 1447 aligns the projection count in mod_head:do/1. Its frame falls from five slots to three, with one slot in OTP. Whole-image equality is unchanged. [Evidence](validation/part-1447/README.md).

Part 1433 removes a field spill in the single-file path of compile_proc/3. The nested call in the multiple-files path still sets the frame size to six, compared with three in OTP. [Evidence](validation/part-1433/README.md).

Part 1432 keeps the selector in x0 for supported call-result cases. compile_proc/3 still saves fresh fields in Y and repeats tagged tests that OTP groups by arity. [Evidence](validation/part-1432/README.md).

Part 1431 removes two prefix spills in compile_proc/3. The selector and tuple fields still spill where OTP dispatches in X registers and preserves only values live across calls. [Evidence](validation/part-1431/README.md).

Part 1430 removes the child_name/2 frames in httpc and httpd. The generated code still tests the tuple and nil shapes that OTP proves from callers. The shared planner still needs cons-arm construction for global:find_node_tag2/2. [Evidence](validation/part-1430/README.md).

Part 1429 aligns tag_nums_ext/3 after label normalization. check_unique2/3 still allocates before the two-cell pattern and dynamic element guard. [Evidence](validation/part-1429/README.md).

Part 1428 reduces excess-raise functions in the cumulative audit from 53 to 39. parse_hier/2 now retains throw where OTP emits raise. Nested C# handler slots still increase where OTP uses decreasing slots. [Evidence](validation/part-1428/README.md).

Part 1427 aligns throw and raise call counts in 469 changed functions. Fifty-three changed functions still emit more raises than OTP. The collect_quoted/3 frame and terminal call form remain different. [Evidence](validation/part-1427/README.md).

Part 1426 aligns the throw path and completes normalized instruction equality in collect_string/3. The nested call and constructed throw reason in the selector branches of collect_quoted/3 still differ. [Collector](validation/part-1426/collect-string.txt) and [throw-call audit](validation/part-1426/throw-forms.txt).

Part 1425 aligns the entry and successful branches in collect_string/3 and the entry in collect_quoted/3. The former still emits a stackful throw at end of input. The latter still uses six slots for each selector case. [Instruction comparison](validation/part-1425/token-summary.txt).

Part 1424 aligns asn1ct_imm:bit_flags/2 after label normalization. The shared list entry verifies the exhausted input before selecting a nil branch. When the tested list is a later argument, OTP can keep separate nil tests in the terminal branches. [Release instructions](validation/part-1424/bit-flags.txt) and [later argument](validation/part-1424/late-list-input.txt).

Part 1423 aligns asn1ct_check:merge_tags2/2 after label normalization. The tagged list head with a caught call and cons fallback in componentrelation_leadingattr/5 still uses a shared stack frame. [Instruction evidence](validation/part-1423/asn1-before.txt) and [normalized result](validation/part-1423/merge-tags2-summary.txt).

Part 1422 aligns mod_alias:append_index/2 after label normalization. The module still has other instruction and metadata differences. The remaining untagged tuple selector in asn1ct:remove_name_collisions2/4 requires shared field projection and dispatch. [Evidence](validation/part-1422/README.md).

Part 1421 aligns asn1ct_check:check_unique_sequence_tags1/3 after label normalization. A list-head call prefix leading to a tuple selector still differs in asn1ct:remove_name_collisions2/4. [Guarded continuation](validation/part-1421/asn1-guarded-list-case.txt) and [selector prefix](validation/part-1421/list-selector-prefix.txt).

Part 1420 removes the five-slot frame from asn1ct_check:check_object_set_objset_list_1/3 and aligns its instructions after label normalization. [Record continuation](validation/part-1420/asn1-list-record-tail.txt).

Part 1419 closes the whole-image difference in xmerl_b64Bin_scan by aligning its tuple-result construction and shared guarded list entry. [Release comparison](validation/part-1419/xmerl-release.txt).

Part 1418 closes the whole-image difference in xmerl_xpath_lib by aligning dynamic tail branches, tuple arity dispatch, and the fallback. [Release comparison](validation/part-1418/xmerl-release.txt).

Part 1417 aligns ttb:format/2 after normalizing labels. A shared tuple slot covers fields used together at a later call. Mixed schedules with several consumers and argument constructors still differ. [Release instructions](validation/part-1417/normalized-functions.txt).

Part 1416 aligns fourteen functions after normalizing labels and saves individual tuple fields across calls. [Release instructions](validation/part-1416/normalized-functions.txt).

Part 1415 aligns et_wx_viewer:revert_main_window/1, io_lib_fread:fread_float_cs/2 and io_lib_fread:fread_int_cs/2 after normalizing labels. [Release instructions](validation/part-1415/normalized-functions.txt).

Part 1414 aligns mnesia_recover:what_happened_remotely/3 and snmp_notification_mib:find_targets/0 after normalizing labels. [Release instructions](validation/part-1414/normalized-functions.txt).

Part 1413 aligns ct_config_xml:list_to_term/1 after normalizing labels. asn1ct_parser2:parse_ActualParameterList/2 still differs in tagged list-head dispatch. [Release instructions](validation/part-1413/operands-after.txt).

Part 1412 aligns asn1ct_func:start_link/0 and preserves the ttb:write_binary/2 schedule. ct_logs:all_runs_header/1 still differs in list construction and frame allocation. The strict whole-image totals are 380 Equal and 986 Different. [Release instructions](validation/part-1412/operands-after.txt).

Part 1411 aligns the instructions of parse_FieldName/1, parse_Enumeration/1, ct_logs:year/0 and remove_xmlnsattr_for_tag/2 after normalizing labels. The strict whole-image totals are 380 Equal and 986 Different. [Release instructions](validation/part-1411/operands-after.txt).

Part 1410 aligns mod_auth_plain:list_groups/1 and xref:get_xref_ignorelist/1 after normalizing labels and closure uniqueness. ct_release_test:fetch_all_apps/1 reaches the OTP frame size and still differs in closure identity operands. [Release instructions](validation/part-1410/operands-after.txt).

Part 1409 aligns tuple-field calls in 128 more normalized function bodies. mod_auth_plain:list_groups/1 still saves the closure and its list input in separate stack slots. The strict whole-image totals are 380 Equal and 986 Different. [Release instructions](validation/part-1409/operands-after.txt).

Part 1408 aligns the instructions of asn1ct_constructed_per:gen_dec_get_extension/1 apart from labels and closure identity metadata. The strict whole-image totals are 380 Equal and 986 Different. [Release instructions](validation/part-1408/operands-after.txt).

Part 1407 aligns asn1ct_check:check_duplicate_defs/2 and its comprehension helper after label normalization. Other instructions and sections in asn1ct_check still differ. [Release instructions](validation/part-1407/operands-after.txt).

Part 1406 matches asn1ct_parser2:resolve_module/1 after normalizing labels. Other functions in the module still differ. [Release instructions](validation/part-1406/operands-after.txt).

Part 1405 matches the complete wxRegion image, including its tuple and record overloads. Three other modules gain normalized instruction matches in functions that accept tuple arguments. [Release targets](validation/part-1405/release-targets.txt). [Function comparison](validation/part-1405/normalized-functions.txt).

Part 1404 matches the complete wxDC image and the normalized instructions in wxRegion:union/3 and union/4. [Release targets](validation/part-1404/release-targets.txt).

Part 1403 matches the complete wxCalendarCtrl and wxDatePickerCtrl images, including option closures and tuple arguments. [Release targets](validation/part-1403/release-targets.txt).

Part 1402 matches wxListItemAttr and five normalized function sequences involving paired colour conversion. [Release targets](validation/part-1402/release-targets.txt).

Part 1401 matches the complete release images for wxCursor, wxStaticBoxSizer and wxStyledTextCtrl. [Release targets](validation/part-1401/release-targets.txt). [Instructions](validation/part-1401/operands-after.txt).

Part 1400 matches the normalized instructions in diameter_exprecs:fname/2 and four other release functions. The containing modules still differ as complete images. [Function comparison](validation/part-1400/normalized-functions.txt).

Part 1399 matches the dynamic tail calls in four release functions after normalizing labels. The three containing modules still differ as complete images. [Function comparison](validation/part-1399/normalized-functions.txt).

Part 1398 matches hash_xof/3 and pack/4 after normalizing labels. [Release instructions](validation/part-1398/operands-after.txt).

Part 1397 matches the do_share_switch/3 instructions and the do_match_value/5 and validate_bs_get_all/8 frame sizes. The latter two still differ in closure names and identifiers. [Release instructions](validation/part-1397/operands-after.txt).

Part 1396 matches normalized pre_process_oid/1 and reduces do_match_value/5 to two slots. The latter still differs from OTP's one-slot frame and literal capture, which reaches the function through a forwarding parameter. [Release instructions](validation/part-1396/tail-after.txt).

Part 1395 retains live values through ordinary calls into supported case arms. The includes/2 sequence matches OTP after normalizing labels. The compile_proc/3 sequence still differs when a record is constructed between calls. [Release instructions](validation/part-1395/release-case-prefix.txt).

Part 1394 matches the first tuple test in 352 additional functions. Multiple same-arity clauses can still allocate and spill before testing a tuple tag. Mixed tuple and non-tuple prefixes still need ordered dispatch. [Entry instructions](validation/part-1394/tuple-entry-after.txt). [Remaining source groups](validation/part-1394/remaining-tuple-entry-groups.txt).

Part 1393 matches the first list test in 265 additional functions. The ASN.1 examples still differ in list extraction and saved values after the first test. Tuple tests before allocation now have the highest module count. [Entry instructions](validation/part-1393/list-entry-instructions.txt). [Frame groups](validation/part-1393/frame-shapes.txt).

Part 1392 can construct call arguments before replacing a dying stack value with an older call result. A slot still needed by a direct argument load cannot be reused at that point. The general list-entry and call-prefix continuation shapes remain open. [Evidence](validation/part-1392/README.md).

Part 1390 aligns ten call-based case functions after label normalization. [Evidence](validation/part-1390/README.md).


Part 1389 aligns asn1ct_pretty_format:complex_list/1 after label normalization. Case arms with additional calls and constructions still prevent the same lowering in asn1ct_imm:enc_pre_cg_1/3. [Evidence](validation/part-1389/asn1-list-branches.txt).

Part 1388 aligns asn1ct_check:make_constr_set_vs_1/1 after label normalization. The singleton and open cons clauses share a list test and extraction. The proper-list proof follows private forwarding parameters and direct comprehension bindings. General case continuations still differ. [Evidence](validation/part-1388/asn1-singleton-cons.txt).

Part 1387 aligns eight list-entry call sequences after label normalization. The planner can extract the tail directly to a saved slot when the first call needs only the head. [Evidence](validation/part-1387/README.md).


Part 1386 aligns asn1ct_gen:get_fieldtype/2 after label normalization. The first checked element access proves the tuple shape for smaller indices in the nested case. Multi-shape tuple dispatch in asn1ct_check:range_intersection/2 still differs. [Element case instructions](validation/part-1386/asn1-list-element.txt). [Range intersection instructions](validation/part-1385/next-list-entry.txt).

Part 1385 tracks results that survive intervening ordinary calls in a straight sequence. When a constructed argument consumes X0 and the result must also be saved, the generated code can spill before construction where OTP constructs in another X register first. [Evidence](validation/part-1385/README.md).

Part 1384 aligns the normalized instructions of asn1ct:generated_refed_func/1. asn1ct:compile_proc/3 still needs frame reuse when a constructed state and a case selector follow the initial call. [Evidence](validation/part-1384/asn1ct-call-frames.txt).

Part 1383 aligns ssh_tcpip_forward_acceptor_sup.beam and eight more normalized functions across the corpus. prim_net:debug/1 reaches the OTP stack size, with instruction differences remaining. The map scheduler covers one terminal dynamic map with unique constant keys. [Evidence](validation/part-1383/README.md).

Part 1382 aligns httpc_handler_sup.beam and ssh_lsocket_sup.beam. [Evidence](validation/part-1382/README.md).

Part 1381 aligns component_index1/4 and 15 other normalized functions. check_enum_number_root/4 tests the list before allocation and still has a wider frame in its gb_sets call branch. [Evidence](validation/part-1381/README.md).

Part 1380 aligns tag_nums_root2/3 with OTP and makes component_index1/4 frame-free. The latter still extracts a tested tuple field into X5 where OTP reuses X4. [Evidence](validation/part-1380/README.md).

Part 1379 aligns do_enc_OCTET_STRING/5 in six Megaco modules after label normalization. The arithmetic tail of leading_attr_index1/5 is frame-free. Its matching branch still saves five slots where OTP saves three. [Evidence](validation/part-1379/README.md).

Part 1378 moves case comparisons before branch frames. leading_attr_index1/5 uses five slots at peak where OTP uses three. Six Megaco images still duplicate a six-slot frame across arms that should prepare arithmetic in registers. [Evidence](validation/part-1378/README.md).

Part 1377 checks list shape before allocation in asn1ct_check:leading_attr_index1/5. The empty path returns without a frame. The nonempty path retains twelve slots where OTP uses three and still schedules the record selector after allocation. [Evidence](validation/part-1377/README.md).

Part 1376 aligns normalized instructions in asn1ct:common_imports/2, et_wx_contents_viewer:menuitem/4 and proc_lib:get_messages/1. cth_conn_log:init/2 has the OTP frame size and still needs its trim before the final call. [Evidence](validation/part-1376/README.md).

Part 1375 aligns normalized instructions for 24 functions, including enc_obj_id_tail/2 in 23 generated modules. Whole-image differences remain in all 24 modules. [Evidence](validation/part-1375/README.md).

Part 1374 checks both lists before allocating three slots in CryptographicMessageSyntaxAlgorithms-2009:check_oid/3. OTP also allocates three slots and still differs in second-list extraction, typed operands, boolean branch layout and exception calls. [Evidence](validation/part-1374/README.md).

Part 1373 moves literal checks before frame allocation in AttributeCertificateVersion1-2009:is_default_1/1 and mk_object_val/3. The try branch in is_default_1/1 uses seven slots where OTP uses one. The frame comparison recovers 45 of the 47 regressions measured in part 1372. [Evidence](validation/part-1373/README.md).

Part 1372 moved the nonempty-list test before the frame in the AttributeCertificateVersion1-2009 release example. That branch uses ten stack slots and OTP uses four. [Evidence](validation/part-1372/README.md).

Part 1371 aligns the normalized instructions of AlgorithmInformation-2009:decode_disp/2 and encode_disp/2. [Evidence](validation/part-1371/README.md).

Part 1370 aligns the normalized instructions of seven release functions, including file:make_link/2, make_symlink/2, rename/2 and write_file/2. [Evidence](validation/part-1370/README.md).

Part 1369 aligns ct_netconfc:connect/2 and dialyzer_dataflow:format_cerl/1 after instruction normalization. The Code size gap widened by 12 bytes in ex_sizers. [Evidence](validation/part-1369/README.md).

Part 1368 reached the one-slot frame in dialyzer_dataflow:format_cerl/1 and identified two remaining constructor register differences. [Evidence](validation/part-1368/README.md).

Part 1367 reaches the OTP peak frame size in 99 functions. The release corpus still has 997 different images. The ex_sizers Code size gap grew by ten bytes even as handle_info/2 moved from four to three frame slots. OTP uses one. [Evidence](validation/part-1367/README.md).

Part 1366 aligns disk_log_1:truncate_at_close2/3 after label normalization. The complete disk_log_1 image still differs. The chunk audit finds Code differences in all 997 differing images. [Evidence](validation/part-1366/README.md).

Part 1365 aligns ttb:write_binary/2 after label normalization. release_handler:prepare_restart_new_emulator/7 uses the release frame size and delays projection. Its first X0 load and final argument move order still differ. [Evidence](validation/part-1365/README.md).

Part 1364 forwards payloads through ordinary call sequences and fuses the tests in longer continuations. [Evidence](validation/part-1364/README.md).

Part 1363 aligns all chunks in wxGLCanvas. dbg_iserver:safe_call/1 and safe_cast/1 match normalized instructions. The containing image still differs. [Evidence](validation/part-1363/README.md).

Part 1362 aligns all chunks in wxPrintDialog, wxProgressDialog and wxSystemOptions. [Evidence](validation/part-1362/README.md).

Part 1361 aligns xmerl_xsd:keysearch_delete/4 after label normalization. Every differing image in that run has a Code payload difference. All 999 differing export tables still name the same exports. All 974 differing Attr payloads decode to identical attributes after removing vsn. [Evidence](validation/part-1361/README.md).

Part 1360 reduces the expanded_name/3 frame from eight slots to two. Its entry record check remains until the caller's record shape can be proven. In 989 release images, two or more BEAM sections still differ in size. [Evidence](validation/part-1360/README.md).

Part 1359 aligns seven more normalized functions. keysearch_delete/4 still retains two arguments specialized by OTP, and the generated expanded_name/3 is unchanged in that part. [Evidence](validation/part-1359/README.md).

Part 1358 aligns decode_pem_file/1 and three more functions after instruction normalization. Nested case patterns and delayed field extraction still differ. [Evidence](validation/part-1358/README.md).

Part 1357 aligns encoding/1, all_or_nothing/0 and 57 more functions after instruction normalization. Their complete release images still differ. [Evidence](validation/part-1357/README.md).

Part 1356 aligns ceiling/2 and floor/2 in xmerl_xpath_pred after label normalization. The complete module still differs. [Evidence](validation/part-1356/README.md).

Part 1355 aligns check_NMTOKENS/1 and seven other functions after label and closure ID normalization. The full release comparison still differs in 1,001 images. [Evidence](validation/part-1355/README.md).

Part 1354 aligns the peak frame sizes of four functions with OTP. Differences elsewhere in the instructions still prevent full function and module matches. [Evidence](validation/part-1354/README.md).

Part 1353 brings 140 functions into normalized instruction agreement. The Code size gap widened by one or two bytes each in beam_types, ssl_handshake and erl_expand_records. Across 989 differing images, at least two section lengths still differ. [Evidence](validation/part-1353/README.md).

Part 1352 aligns the boolean case, tail arguments and frame layout in xmerl_scan:update_attributes1/2. Eleven functions gained normalized instruction matches. The complete xmerl_scan image still differs. [Evidence](validation/part-1352/README.md).

Part 1351 restores normalized matches for the eight lifted functions in dbg_ieval and erl_eval. The Code size gaps in those modules grew by six and nine bytes. Seven functions reduce their peak frame size through branch-local case planning. [Evidence](validation/part-1351/README.md).

Part 1350 emits complex branch bodies after shared list and tuple checks. The focused complex branches still allocate more slots than OTP. The Code size gap widened in 21 modules. [Evidence](validation/part-1350/README.md).

Part 1349 aligns failure targets, field reads and stack slots in xmerl_lib:simplify_attributes/1. The full xmerl_lib image still differs. [Evidence](validation/part-1349/README.md).

Part 1348 shares tuple arity dispatch across list-head clauses. Both parse_pcdata functions match normalized OTP instructions. [Evidence](validation/part-1348/README.md).

Part 1347 moves literal list head dispatch before the frame in check_date/1 and other function entries. The remaining check_date/1 body still differs. The Code size gap in diameter_make grew by two bytes. [Evidence](validation/part-1347/README.md).

Part 1346 keeps the final call result in X0 when building an inline tuple or record for a tail call. asn1_db:init/2 now matches normalized OTP instructions. The Code size gap in diameter_config grew by one byte. [Evidence](validation/part-1346/README.md).

Part 1345 uses a zero-slot tail call for queue-only wx option wrappers. The pattern requires the wx environment check and option mapper. wx_misc still differs from its release image. [Evidence](validation/part-1345/README.md).

Part 1344 schedules ten more conversion BIF signatures through the ordinary call planner. The Code size gap widened in asn1ct_parser2 and erl_lint. [Evidence](validation/part-1344/README.md).

Part 1343 schedules a nested ordinary call chain inside a guarded clause. Multiple sibling calls and further external conversions remain open. [Evidence](validation/part-1343/README.md).

Part 1342 shares list tests across mixed guarded and literal branches. Common tuple shape checks still require planning. [Evidence](validation/part-1342/README.md).

Part 1341 schedules a single call in the head and another in the tail of a cons result. Multiple head calls and mixed clause continuations remain open. [Evidence](validation/part-1341/README.md).

Part 1340 checks repeated variables across list patterns and other arguments. [Evidence](validation/part-1340/README.md).

Part 1339 checks nested cons entries and repeated tuple bindings in registers. Shared tagged tuple checks and inner arity dispatch still differ from OTP in bs_seq_match_fixup/1. [Evidence](validation/part-1339/README.md).

Part 1338 keeps a cons head or tuple fields live across the tail expression's call. [Evidence](validation/part-1338/README.md).

Part 1337 matches the full error_handler release image. The registered-process emitter covers the two-append branch shape. Singleton literal prefixes still need the OTP cons optimization for byte parity. [Evidence](validation/part-1337/README.md).

Part 1256 keeps simple guarded list clauses in registers until a result requires a frame. Repeated list extraction and redundant shape checks still differ from the release. [Evidence](validation/part-1256/README.md).

Part 1255 emits local tails in guarded atom cases with fresh branch bindings. Frame placement and live register counts still differ in the ASN.1 enumeration functions. [Evidence](validation/part-1255/README.md).

Part 1254 supports computed local tail arguments and merges identical simple tail blocks. [Evidence](validation/part-1254/README.md).

Part 1253 emits direct local tails with simple operands in terminal atom cases. [Evidence](validation/part-1253/README.md).

Part 1252 folds a terminal identity case into a local tail call. [Evidence](validation/part-1252/README.md).

Part 1251 restores the outer atom case failure location in asn1ct:check/2. Branch-local register lifetimes still differ. [Evidence](validation/part-1251/README.md).

Part 1250 dispatches prepared atom selectors in X0 and preserves the unmatched value in case_clause errors. [Evidence](validation/part-1250/README.md).

Part 1249 matches the snmpa_error caller by inferring the local selector's return shape and keeping the dynamic apply target in an X register. Other entry-frame liveness differences remain. [Evidence](validation/part-1249/README.md).

Part 1231 emits an immediate timeout for eligible receive expressions when direct private callers prove a single value. [Evidence](validation/part-1231/README.md).

Part 1195 records verified header paths in Common Test, Debugger, SSL and SSH release metadata. [Evidence](validation/part-1195/README.md).

Part 1194 defers record field extraction until the fields are needed and records the Windows release header paths. [Evidence](validation/part-1194/README.md).

Part 1193 folds constant bindings into register tuple returns. [Evidence](validation/part-1193/README.md).

Part 1191 retains tagged tuple selectors in X0 when the wildcard fallback discards the selector. [Evidence](validation/part-1191/README.md).

Part 1190 omits the selector save before select_val when scalar literal branches end in a wildcard. [Evidence](validation/part-1190/README.md).

Part 1189 omits the temporary Y slot for case selectors tested against a literal with a wildcard fallback. [Evidence](validation/part-1189/README.md).

Part 1188 forwards fallback call results directly to terminal call operands when the forwarded-call planner accepts them. [Evidence](validation/part-1188/README.md).

Part 1187 tests the call result in X0 for a literal case with a wildcard fallback. [Evidence](validation/part-1187/README.md).

Part 1186 retains a call result in X0 for tuple returns after a fully emitted entry guard. [Evidence](validation/part-1186/README.md).

Part 1185 omits first-call-only inputs after a fully consumed entry guard. [Evidence](validation/part-1185/README.md).

Part 1184 performs clause tests before allocating frames for supported call-result tuples. [Evidence](validation/part-1184/README.md).

Part 1183 skips stack copies for arguments unused by every clause when the final clause is unconditional. [Evidence](validation/part-1183/README.md).

Part 1182 shares shape and literal checks before allocating a frame for the selected tuple clause. [Evidence](validation/part-1182/README.md).

Part 1181 executes tuple type guards before allocating a stack frame and retains original arguments on failure. [Evidence](validation/part-1181/README.md).

Part 1180 keeps original X arguments available to an unconditional fallback before stack allocation. [Evidence](validation/part-1180/README.md).

Part 1179 checks nested tuple patterns in X registers and allocates the stack on the success path. [Evidence](validation/part-1179/README.md).

Part 1178 allocates framed tuple clauses before extracting referenced fields directly into Y registers. [Evidence](validation/part-1178/README.md).

Part 1177 tests flat tuple patterns before allocating the stack and omits Y slots for unused bindings. [Evidence](validation/part-1177/README.md).

Part 1176 checks common tuple literals before allocating a frame and preserves the arguments on the failure path. [Evidence](validation/part-1176/README.md).

Part 1175 reuses an X register for tuple literal comparisons during entry matching. [Evidence](validation/part-1175/README.md).

Part 1173 shares the integer guard prefix of scalar and two-element tuple wx command overloads. [Evidence](validation/part-1173/README.md).

Part 1172 shares the framed return across nonconsecutive wx class checks. Thirteen images remain in the smallest difference group of five chunks. [Evidence](validation/part-1172/README.md).

Part 1080 avoids initialization for directly written entry slots. The general path still saves parameters and bindings that OTP can retain in X registers. [Evidence](validation/part-1080/README.md).

Part 1079 avoids temporary slots for literal list heads. The general path still tests these literals after allocation. [Evidence](validation/part-1079/README.md).

Part 1078 keeps intermediate tails out of the frame for simple entry patterns. Literal heads and nested patterns can still use temporary Y slots. [Evidence](validation/part-1078/README.md).

Part 1077 removes wildcard field reads from general list patterns. Intermediate tails still occupy frame slots. [Evidence](validation/part-1077/README.md).

Part 1076 skips fields ignored by all shared cons patterns. Values required by a pattern still occupy Y slots. [Evidence](validation/part-1076/README.md).

Part 1075 shares early cons extraction but still saves both fields in the frame for subsequent pattern checks. [Evidence](validation/part-1075/README.md).

Part 1074 checks simple tuple heads before allocation. It still reserves both cons fields and copies surviving bindings to Y registers. [Evidence](validation/part-1074/README.md).

Part 1073 selects get_hd or get_tl when only one list field is read. Simple branches can still allocate a zero-slot frame where OTP uses no frame. [Evidence](validation/part-1073/README.md).

Part 1072 extracts simple cons fields before allocation. Wildcard fields are still extracted and named fields still spill into the body frame. [Evidence](validation/part-1072/README.md).

Part 1071 delays allocation past the entry list test. The generated cons branch still extracts fields in the frame. [Evidence](validation/part-1071/README.md).

Part 1070 moves defaulting list cases ahead of frame allocation. The generated dbstart still saves three values where OTP uses no Y slots. [Evidence](validation/part-1070/README.md).

Part 1069 eliminates the frame for simple tuple field tail calls. OTP uses lower scratch registers and can extract directly to argument registers. [Evidence](validation/part-1069/README.md).

Part 1068 shrinks tuple branch frames from measured slot peaks. OTP still avoids more spills through cross-call liveness. [Evidence](validation/part-1068/README.md).

Part 1067 tests and extracts simple tuple fields in X registers. OTP still uses smaller frames and can preserve the original tuple across a call. [Evidence](validation/part-1067/README.md).

Part 1066 moves tuple arity selection before frame allocation. Each branch still allocates a shared maximum frame and extracts tuple fields in Y registers. [Evidence](validation/part-1066/README.md).

Part 1065 shares is_tuple across differing arities. The generated code still tests each arity inside the frame while OTP selects arity up front. [Evidence](validation/part-1065/README.md).

Part 1064 hoists tuple checks shared by all clauses. The general emitter still extracts fields and dispatches on tags inside individual clauses. [Evidence](validation/part-1064/README.md).

Part 1063 removes the frame from simple tail calls with supported nonallocating BIFs. Multi-clause bodies still often allocate before entry shape tests. [Evidence](validation/part-1063/README.md).

Part 1062 removes stack frames from simple BIF tuple returns. OTP additionally reuses identical dictionary lookups in the probe. [Evidence](validation/part-1062/README.md).

Part 1061 eliminates the Y spill of a sole computed tuple member. Entry arguments still occupy Y slots in the general path. [Evidence](validation/part-1061/README.md).

Part 1060 eliminates the Y spill of a sole computed call argument. The probe still allocates slots for incoming parameters and the computed member of the return tuple. [Evidence](validation/part-1060/README.md).

Part 1059 omits unused general-emitter parameters from the frame. Used parameters and expression temporaries may still be stored in Y registers even when they never cross a call. [Evidence](validation/part-1059/README.md).

Part 1058 aligns get/1 and self/0 opcodes in asn1_db:req/1. Register allocation and receive markers still differ. [Evidence](validation/part-1058/README.md).

Part 1057 aligns register-based supervisor dispatch in two Diameter modules. The identity branch in the synthetic probe retains a redundant move. [Evidence](validation/part-1057/README.md).

Part 1015 aligns Dbgi for 31 of the 33 wx examples. Demo_html_tagger and ex_gl retain incomplete abstract code. [Evidence](validation/part-1015/README.md).

Part 1012 aligns compile metadata for xmerl, observer, dialyzer, mnesia and debugger. Code differences remain in those applications. [Evidence](validation/part-1012/README.md).

Part 1011 aligns the Diameter compile metadata. Forty-one Diameter images still differ in Code and Attr. [Evidence](validation/part-1011/README.md).

Part 991 aligns wx create overloads with tuple field guards. Integer and text dispatch and multiple conversions still differ. [Evidence](validation/part-991/README.md).

Part 968 preserves binary receive match results and mailbox ordering. Inet_gethost_native retains release byte differences. [Evidence](validation/part-968/README.md).

Part 967 aligns the tested equiv formatting. Global and string retain release byte differences. [Evidence](validation/part-967/README.md).

Part 966 preserves list generator scope during inlining. The fixture and gen_udp_socket retain byte differences. [Evidence](validation/part-966/README.md).

Part 965 verifies scoped closure inlining behavior. The probe and newly generated release images still differ. [Evidence](validation/part-965/README.md).

Part 964 aligns comprehension filter behavior. The fixture and newly generated images retain byte differences. [Evidence](validation/part-964/README.md).

Part 963 matches the fixture extraction and failure results. The emitted instructions still differ. [Evidence](validation/part-963/README.md).

Part 962 preserves nested branch bindings and the result value. The fixture and new images retain byte differences. [Evidence](validation/part-962/README.md).

Part 961 adds GC-aware rounding guard emission. The fixture and qlc_pt retain byte differences. [Evidence](validation/part-961/README.md).

Part 960 verifies guard BIF behavior. The fixture and newly generated images retain byte differences. [Evidence](validation/part-960/README.md).

Part 959 verifies receive guard behavior. The fixture and newly generated release images still differ in bytecode. [Evidence](validation/part-959/README.md).

Part 958 matches the OTP external fun literal for direct auto-imported references. The five newly generated release images still differ. [Evidence](validation/part-958/README.md).

Part 957 aligns nested guard grouping. Snmp_config retains Code and Dbgi differences. [Evidence](validation/part-957/README.md).

Part 956 aligns grouped operand debug forms. The new eunit images retain instruction differences. [Evidence](validation/part-956/README.md).

Part 955 aligns sigil debug annotations. The generated beam_asm image retains instruction and debug chunk differences. [Evidence](validation/part-955/README.md).

Part 954 aligns comprehension debug forms. The two new dialyzer images retain instruction and metadata differences. [Evidence](validation/part-954/README.md).

Part 953 aligns map update and catch coordinates in debug forms. The release typer image still differs in its code and other chunks. [Evidence](validation/part-953/README.md).

Part 952 emits live branch bindings for match operands. The fixture still differs from OTP in its instructions and metadata. [Evidence](validation/part-952/README.md).

Part 951 uses shared frame slots for live conditional and receive bindings. The fixture still differs in instructions and metadata. [Evidence](validation/part-951/README.md).

Part 950 emits the nil-pattern body through the general emitter. OTP folds the entire probe result to a literal. [Evidence](validation/part-950/README.md).

Part 949 carries live case bindings in frame slots. The OTP probe instead shares tuple tests and uses X-register moves. [Evidence](validation/part-949/README.md).

Part 948 accepts warning and verbosity switches for emission. OTP warning output is outside the implemented behavior. [Evidence](validation/part-948/README.md).

Part 947 estimates automatic inlining cost in the source tree. OTP measures cost during Core transformation. [Evidence](validation/part-947/README.md).

Part 946 preserves maybe_expr keyword and Meta behavior. Other feature names remain unsupported. [Evidence](validation/part-946/README.md).

Part 945 handles the sigils at the source gate. The shell_docs_markdown and zip modules now reach separate emission gaps. [Evidence](validation/part-945/README.md).

Part 944 emits explicit on_load markers in BEAM. Legacy implicit load_nif marking remains open. [Evidence](validation/part-944/README.md).

Part 943 generates Diameter record accessors. The generated instruction layout and abstract debug forms remain open. [Evidence](validation/part-943/README.md).

Part 942 supports the observed ETS match specification subset. The probe still differs in Code, ExpT, LitT and Attr. [Evidence](validation/part-942/README.md).

Part 941 records relative file documentation locations. The erlang module still requires an additional Markdown input. [Evidence](validation/part-941/README.md).

After supplying release macro inputs, five modules reach debug-form gaps and two reach the ms_transform source gate. [Evidence](validation/part-940/README.md).

Direct extraction guard comparisons match the pinned probe. General guard layouts still differ and socket reaches a separate cancel/2 blocker. [Evidence](validation/part-939/README.md).

Part 938 lowers more inline templates. The deterministic probe retains differences in Code, ExpT, Attr and Type. [Evidence](validation/part-938/README.md).

Explicit NIF declarations emit entry markers. The local stub call probe still differs, and legacy implicit load_nif marking is outside the change. [Evidence](validation/part-937/README.md).

Part 936 covers the equiv forms observed at the release metadata gate. General erl_pp line wrapping and arbitrary fun clauses remain outside this formatter. [Evidence](validation/part-936/README.md).

Part 935 extends selective receive guards. The probe still differs in instruction layout, and the supervisor guard remains unsupported. [Evidence](validation/part-935/README.md).

Part 934 emits zipped list loops inside their functions. OTP uses recursive comprehension helpers. Mixed source kinds remain unsupported. [Evidence](validation/part-934/README.md).

Part 933 uses OTP predicate opcodes. The deterministic predicate probe retains differences in Code, ExpT and Attr. [Evidence](validation/part-933/README.md).

Direct xor-family operations match OTP instruction encoding. The general guard emitter can still differ in register layout and boolean normalization. [Evidence](validation/part-932/README.md).

After restoring shared ASN.1 metadata, 28 public_key ASN.1 images still differ. Remaining differences include instructions, tables and line encoding. [Evidence](validation/part-931/README.md).

Part 930 exposes the r26 compile-option gate in test_server_node and four emitter gaps after resolving includes. [Evidence](validation/part-930/README.md).

Restored public_key input headers expose code and metadata differences in the resulting BEAM. Twelve sources still first fail on include lookup. [Evidence](validation/part-929/README.md).

Complex fold callbacks retain lists:foldl/3. The general emitter still differs in caller frame and tail-call layout. [Evidence](validation/part-928/README.md).

Guarded inline dispatch now emits failure helpers. Full stack, tail placement and remaining template patterns remain open. [Evidence](validation/part-927/README.md).

Computed receive guard operands now preserve live scratch registers. Computed semicolon alternatives and further receive operations remain open. [Evidence](validation/part-926/README.md).

Selective receive now emits literal-key map patterns and supported pure type guard alternatives. Other guard forms and catch-all validation remain open. [Evidence](validation/part-925/README.md).

Docs now records file and location for included forms. Release include paths can still require a recorded profile mapping. [Evidence](validation/part-924/README.md).

Targeted inline record construction and update now preserve field substitution. Included Docs forms and other inline templates remain separate. [Evidence](validation/part-923/README.md).

Targeted inline sites now evaluate computed arguments once and keep referenced definitions. Remaining template, multi-clause and optimizer layout differences still affect release parity. [Evidence](validation/part-922/README.md).

Fixed-width binary generators now emit BEAM. Lifted helper layout, dynamic segment widths and zip generators remain open. [Evidence](validation/part-921/README.md).

Binary comprehensions with list or map generators now emit a writable accumulator. Lifted iteration, binary generator patterns and zip generators remain open. [Evidence](validation/part-920/README.md).

Named recursive funs now emit local self calls and can return the self value. General register placement and lifted body layout still differ from the release. [Evidence](validation/part-919/README.md).

The generic guard path now emits abs, bit_size, trunc and map_get. The affected release images still differ in instruction or metadata layout. [Evidence](validation/part-918/README.md).

Maybe expressions and map_size guards now emit BEAM in the general path. OTP failure-helper placement and register allocation remain separate release alignment work. [Evidence](validation/part-917/README.md).

Map comprehensions with supported qualifiers now emit BEAM. Lifted comprehension helpers, the from_keys optimization and remaining generator forms still affect release alignment. [Evidence](validation/part-916/README.md).

Bitstring, reference and map key guards now emit BEAM in the general path. Twenty formerly unsupported modules produce images that still differ from the release. [Evidence](validation/part-915/README.md).

wx option mapping followed by a reply and object Unicode conversion followed by a reply now match the pinned reference for the covered forms. Nested wx record options and other mixed overloads remain open. [Evidence](validation/part-914/README.md).

Record-guarded wx option forwarding and object commands with one receiver and an option list match the pinned images. Options with additional live arguments or a following receive remain open. [Evidence](validation/part-913/README.md).

Docs keeps types reachable from callbacks and release include paths. Six metadata-only candidates still differ in Dbgi. [Evidence](validation/part-894/README.md).

Direct fixed-width bit extraction matches the OTP fixture. General fixed-bit register layout and dynamic field widths remain open. [Evidence](validation/part-893/README.md).

Header type Docs preserve the OTP file and location entry annotation and plain source_anno metadata. [Evidence](validation/part-892/README.md).

A direct map field pattern matches the OTP fixture. General list qualifiers use loop accumulation and reversal and differ from OTP generated helper layout. [Evidence](validation/part-891/README.md).

Direct callback metadata and callback Docs match the selected deterministic fixtures. More complex behaviour helper ordering remains open. [Evidence](validation/part-890/README.md).

The general fallback keeps successful specialized lowering. General call and construction layouts may still differ from OTP. [Evidence](validation/part-889/README.md).

Direct node, port, float and byte size guards match the deterministic OTP fixture. Compound register allocation remains different. [Evidence](validation/part-888/README.md).

A direct zero-argument try after matches the selected OTP fixture. General cleanup code still differs in frame layout and line information. [Evidence](validation/part-887/README.md).

Direct division and list operator forms use OTP opcode families. General register allocation and operand typing remain open. [Evidence](validation/part-886/README.md).

Direct head, number and map guards match the deterministic OTP fixture. The compound layout still differs. [Evidence](validation/part-885/README.md).

Direct bit construction matches the selected OTP fixture. Nested register allocation and constant segment coalescing still differ. [Evidence](validation/part-884/README.md).

Direct length and function guards match the deterministic OTP fixture. Compound guards still differ in register allocation and type operands. [Evidence](validation/part-883/README.md).

General maps evaluate the base, keys and values before applying updates. Mixed update instruction and register layouts still differ from OTP. [Evidence](validation/part-882/README.md).

Direct element guards, boolean negation and multiplication match the selected OTP fixture. Compound guard and arithmetic layouts still differ. [Evidence](validation/part-881/README.md).

Direct PID and tuple arity guards match the OTP fixture. General tuple size comparisons still differ in register allocation and type operands. [Evidence](validation/part-880/README.md).

Guarded record access rejects the current guard on an invalid tuple tag or arity. Ordinary record access still raises badrecord. [Evidence](validation/part-879/README.md).

Direct function-value calls match the selected OTP layouts. Computed function-value calls preserve evaluation order, with register allocation and typed call selection still differing. [Evidence](validation/part-878/README.md).

General selective receive uses scratch registers during matching and spills selected bindings after message removal. Receive code layout still differs from OTP. [Evidence](validation/part-877/README.md).

Local function references use zero-capture fun entries, including within lifted closure bodies. The selected deterministic forms match OTP. [Evidence](validation/part-876/README.md).

Binary subtraction and unary minus use the managed BEAM arithmetic BIFs and preserve guard failure. The selected deterministic forms match OTP. [Evidence](validation/part-875/README.md).

Numeric comparisons use the BEAM comparison tests or BIFs with an exact test for atom literals. The verified tagged list case and shared xmerl profile reproduce xmerl_sgml. [Evidence](validation/part-874/README.md).

Traditional record predicates lower to tagged tuple checks. Native record predicates and additional guard BIF forms remain separate. [Evidence](validation/part-873/README.md).

Documentation metadata maps merge with text and support since inheritance and selected equiv expressions. Full OTP pretty-printing and callback metadata remain partial. [Evidence](validation/part-872/README.md).

Imported source calls resolve to external modules while their unqualified syntax and import declarations remain in the abstract debug forms. [Evidence](validation/part-871/README.md).

General catch expressions use BEAM catch and catch_end with an isolated binding scope. Direct call and tuple-result fixtures match OTP. [Evidence](validation/part-870/README.md).

General list append emits the Erlang BIF, with cons construction for known proper prefixes. The selected deterministic append shapes match OTP. [Evidence](validation/part-869/README.md).

General anonymous closures support captured values and nested lifted functions. Closure register allocation, label placement and full release parity remain partial. [Evidence](validation/part-868/README.md).

Traditional record construction emits tagged tuples with defaults and wildcard values. The additional generated wx modules still have instruction and related chunk differences. [Evidence](validation/part-867/README.md).

Dynamic remote calls in the general emitter preserve operand evaluation order and use BEAM apply instructions. Release register allocation and the remaining expression families require separate parity verification. [Evidence](validation/part-866/README.md).

The five generated wx modules in Part865 match their Windows release images. Shared source profiles for other wx modules remain outside this verification. [Evidence](validation/part-865/README.md).

Documentation text supports Unicode character lists and UTF-8 binaries, OTP edge whitespace trimming and leading signatures. Documentation metadata maps and the remaining source annotation cases are partial. [Evidence](validation/part-864/README.md).

Typed documentation emits exported and reachable private type entries before function entries. Header file annotations and full OTP documentation metadata remain partial. [Evidence](validation/part-863/README.md).

Quoted strings and atoms share the character literal decoder for hexadecimal, octal and control escapes. [Evidence](validation/part-862/README.md).

Function context macros in bodies and guards use the expanded function head to determine the name and arity. Calls to these macros before the closing parenthesis of the first head remain outside this change. [Evidence](validation/part-861/README.md).

The output dispatch fixture matches OTP 29.1.1, including record guard failure and term ordering. The full inet_epmd_socket image remains unsupported at output_handler_wait/6. [Evidence](validation/part-860/README.md).

The complete Windows OTP 29.1.1 dtls_socket image matches byte for byte. The pinned OTP probe covers listener reuse, address-in-use and forwarded error paths. [Full image comparison](validation/part-841/README.md).

The complete Windows OTP 29.1.1 dtls_v1 image matches. The pinned OTP probe covers cipher suite filters, DTLS and TLS version mappings, cookie settings and HMAC output. [DTLS comparison](validation/part-828/README.md).

The complete Windows OTP 29.1.1 ssl_client_session_cache_db image matches. The pinned OTP probe covers the ETS cache lifecycle and exception fallbacks. [Cache comparison](validation/part-827/README.md).

The complete Windows OTP 29.1.1 ssl_dist_sup image matches. The pinned OTP behavior probe covers startup with and without configuration and seven configuration error cases with stack comparison. [Distribution supervisor comparison](validation/part-826/README.md).

The complete Windows OTP 29.1.1 ssl_upgrade_server_session_cache_sup image matches. The pinned OTP behavior probe covers cache creation, cache reuse, the already_started response and case errors. [Cache upgrade supervisor comparison](validation/part-825/README.md).

The complete Windows OTP 29.1.1 tls_dyn_connection_sup image matches. The pinned OTP behavior probe covers the child specifications, receiver options message and malformed input errors. [Dynamic supervisor comparison](validation/part-824/README.md).

The Windows OTP 29.1.1 cdv_sched_cb, cdv_mod_cb, cdv_ets_cb, cdv_port_cb and cdv_dist_cb release images match the C# output byte for byte. Runtime behavior and other release profiles have separate validation. [Scheduler evidence](validation/part-786/README.md), [module evidence](validation/part-785/README.md), [ETS evidence](validation/part-784/README.md), [port evidence](validation/part-783/README.md) and [distribution evidence](validation/part-782/README.md).

The Windows OTP 29.1.1 cdv_proc_cb release image also matches the C# output byte for byte. [Process details evidence](validation/part-787/README.md).

The recursive binary digit formatter and protected formatting closures match deterministic OTP fixtures. The complete Windows OTP 29.1.1 cdv_bin_cb image also matches byte for byte. [Binary formatting evidence](validation/part-788/README.md), [closure evidence](validation/part-789/README.md) and [release comparison](validation/part-790/README.md).

The Windows OTP 29.1.1 cdv_int_tab_cb release image matches the C# output byte for byte. [Internal table evidence](validation/part-791/README.md).

Recursive term expansion and protected term formatting match isolated OTP fixtures. The complete Windows OTP 29.1.1 cdv_term_cb image also matches byte for byte. [Expansion evidence](validation/part-796/README.md), [closure evidence](validation/part-797/README.md) and [release comparison](validation/part-798/README.md).

The dynamic detail reply, progress pulse, window setup, tab setup, tagged argument errors and the constant menu list match isolated OTP fixtures. The complete Windows OTP 29.1.1 cdv_detail_wx image matches the release byte for byte, including Dbgi. [Dynamic reply evidence](validation/part-799/README.md), [window evidence](validation/part-800/README.md), [tab evidence](validation/part-801/README.md), [error tuple evidence](validation/part-802/README.md) and [menu and full image evidence](validation/part-803/README.md). The progress dispatch and tail calls now match OTP in an isolated fixture. [Progress dispatch evidence](validation/part-804/README.md). The page comprehension matches an isolated fixture and the full Line chunk now matches OTP. [Page comprehension evidence](validation/part-805/README.md) and [callback and chunk comparison](validation/part-806/README.md). [Full image evidence](validation/part-807/README.md).

The cdv_html_wx guarded init/1 dispatch matches an isolated OTP fixture. The initialization and record update also match in an isolated fixture. The complete Windows OTP 29.1.1 cdv_html_wx image now matches the release byte for byte. [Guarded dispatch evidence](validation/part-808/README.md) and [record update evidence](validation/part-809/README.md). The delayed page callback also matches an isolated OTP fixture. [Callback evidence](validation/part-810/README.md). The detail-window cast update also matches an isolated OTP fixture. [Cast evidence](validation/part-811/README.md) and [full emission evidence](validation/part-812/README.md). The HTML compile information, compressed debug information and documentation now match. [Metadata evidence](validation/part-813/README.md). The window expansion instructions match after accounting for shifted labels, and the import and type chunks match byte for byte. [Expansion evidence](validation/part-814/README.md) and [whole image comparison](validation/part-815/README.md).

The complete Windows OTP 29.1.1 dtls_connection_sup image also matches byte for byte. [DTLS release comparison](validation/part-816/README.md). The DTLS and SSL session cache supervisor release images also match. [Session cache comparison](validation/part-817/README.md). The DTLS listener supervisor image also matches byte for byte. [Listener comparison](validation/part-818/README.md). The dtls_server_sup and dtls_sup images also match. [Supervisor comparison](validation/part-819/README.md). The SSL and TLS root and server supervisors also match their pinned release images. [SSL and TLS comparison](validation/part-820/README.md). Both SSL connection supervisor images also match. [Connection supervisors](validation/part-821/README.md). The SSL distribution admin supervisor also matches. [Admin supervisor comparison](validation/part-822/README.md). The SSL listener tracker and TLS session ticket supervisors match, including the different dispatch shapes of their private and exported name helpers. [Named supervisor comparison](validation/part-823/README.md).

The captured table comprehension, memory field formatting and column mapping match deterministic OTP fixtures. The complete Windows OTP 29.1.1 cdv_mem_cb image also matches byte for byte. [Captured table evidence](validation/part-792/README.md), [memory field evidence](validation/part-793/README.md), [column mapping evidence](validation/part-794/README.md) and [release comparison](validation/part-795/README.md).

The Windows OTP 29.1.1 client_server release image matches the C# output byte for byte. The remaining corpus still contains input rejection and unsupported emission cases. [Evidence](validation/part-750/README.md).

Part751 admits adjacent string literals and retains their merged debug form. Part752 supports dynamic external fun references. Part753 admits named recursive funs and supports the tested source behavior. Part754 emits simple returned closures, with general closure emission and source stack-frame identity still open. [Evidence](validation/part-754/README.md).

sys Part591 implements all37 non-metadata exports in selectable C# alongside unchanged Erlang/BEAM. Both pass137/137 complete focused observations and52 error cases at depths0/1/2/3/8/32 on identical final binaries. Actual file output, original behaviors and five unchanged upstream cases pass. Full CT capture/distribution/platform/resource/metadata acceptance remains outside this finite evidence. [Selection](sys-backends.md), [receipt](validation/part-591/README.md). Statistics time/reduction samples and ephemeral identities have explicit fixture projections; frames and locations are retained. No whole-module compatibility-ledger promotion follows from API counts.

Part590 error_logger has selectable C# and original Erlang alternatives for all29 exports. Both pass143 complete observations and10 unchanged upstream tests; local function identity uses the actual C# assembly. The non-distributed official emulator-suite failure is retained and excluded from accepted rows. Universal distribution/platform/resource/scheduling/metadata and release-startup equivalence remains unverified. No full regression or readiness refresh. [Scope](error-logger-backends.md), [receipt](validation/error-logger-port-20261105/README.md).

Part589 rpc: all35 explicit exports have C#/original Erlang alternatives. Both pass241 complete API observations,18 protocol/lifecycle/I/O scenarios and13 two-runtime distribution-packet checks. Preserve the block_call EXIT distinction, non-abandoning nb_yield, duplicate-node ordering and upstream C-node error-state quirk. Request IDs are verified as two distinct refs then yielded; only one captured callback code identity and the observer PID inside C-node failure-state data are projected, with module/name/arity/environment/full remaining stacks checked. Full peers/EI/platform/resource/scheduler/metadata/release acceptance remains open. [Receipt](validation/rpc-port-20261105/README.md).

Part588 logger: all77 explicit exports have selectable C# and retained original Erlang/BEAM implementations. Each backend passes155 focused observations, including5 unchanged upstream tests, handler events, source-bound configuration-error diagnostics and full tested error stacks. Shared Logger services remain original Erlang on .NET. Report-map enumeration has unspecified order: only exact format fragments with their matching key/value arguments are compared as associations; ordered reports/stacks/pretty-print bytes are unchanged. PID/group-leader/time projections require provenance checks. Full upstream, distribution/overload/resource/platform/release-startup acceptance and canonical readiness remain open. [Backend contract](logger-backends.md), [receipt](validation/logger-port-20261105/README.md).

io Part587 implements the complete53-export selected interface beside unchanged original Erlang, with one common printable_range primitive. Both backends pass666/666 complete focused observations, including error source frames and7 lifecycle/real callback scenarios plus3 unchanged upstream cases. Fixture-owned PIDs and fresh error references alone are alpha-renamed. The library profile restores actual oracle feature state through native persistent_term; full boot, terminal/filesystem, scheduling/resource/platform and old-peer acceptance remain unverified. No universal100% claim. [Receipt](validation/io-port-20261105/README.md).

Part593 implements the native node/dist reference query behind erts_debug and both erts_internal backends. Actual owner/header/storage census, all-scheduler admission, signal/heap classification and retirement are covered by262/262 observations plus59/59 scenarios per backend,13 physical and43 connected groups. Original Erlang/BEAM and98 exports remain. Full regression/readiness were deliberately not run; broader upstream/distributed/resource/platform acceptance and universal compatibility remain unverified. [Receipt](validation/part-593/README.md).

application Part584: all39 non-module_info exports have a selectable C# implementation and unchanged Erlang/BEAM alternative. Both pass101 exact API outcomes/errors/stacks and19 connected scenarios (five unchanged upstream functions). Concurrent startup compares successful app membership; serial order remains exact. Original controller/master/distribution/gen_server helpers execute on .NET for both alternatives. This finite Windows corpus does not cover all distributed scheduling/node-loss, platform/resources, complete suite, full release boot or metadata. No fallback, live state migration, universal100% claim or canonical readiness promotion. See [backend scope](application-backends.md) and [receipt](validation/part-584/README.md).

Supervisor Part583 implements24 managed exports and preserves the original Erlang/BEAM alternative, including OTP record layouts, four strategies, retries, significant children, parallel shutdown, sys/code change and Logger callbacks. Both backends pass287/287 API observations and105/105 scenarios each, including19 unchanged upstream testcases; no universal100% or canonical-readiness promotion. [Scope and evidence](validation/part-583/README.md).

gen_event Part580 retains a scheduling-sensitive upstream_call failure at gen_event_SUITE.erl:853 (depth3 Managed): wait_response(...,0) can see an already queued reply where the testcase expected timeout. No expected values or stacks are normalized. Primary105/105 per backend passes, while universal scheduling/resource/distribution parity is unproved. [Evidence](validation/part-580/README.md).

Part569 ETS: Both backends pass 355 exact observations, 8 stateful scenarios, 12 terminal/regex scenarios and 75 real filesystem/DETS/corruption scenarios each (450 per backend). Official OTP also reads both exported dumps. Original Erlang is unchanged; 25 C# library exports compose 51 shared primitives. Full upstream/platform/resource/scheduling/metadata/startup-manifest acceptance remains open. No full regression or global readiness refresh ran. Worker table references are alpha-renamed; i/0 projects its fixed header and table presence because boot rows/PIDs/memory differ. All other selected outcomes and full exception stacks are compared. Feature state is restored through real persistent_term; original disk_log/file/DETS helpers run over actual native file/buffer resources. Initial missing-native filesystem failures are retained as history. See [receipt](validation/ets-port-20261104/README.md).

Part579 gen_statem:40 non-metadata exports and the C# engine are opt-in beside unchanged original Erlang. Both implementations match74 complete API observations and311 focused engine scenarios, including256 event/mode combinations, invalid actions/modes/results, complete exception stacks and exactly-one termination after sys detours. Preserve the original helper and native timer/receive contracts; no fallback or live state migration. This is finite Windows acceptance, not100% OTP certification. Full upstream-suite, owned-heap/resource/platform/distribution/global/via, actual structured Logger handler and module-metadata acceptance remain open. Logger primary output is disabled in this fixture; formatting is compared separately. Global regression/readiness are deliberately unrun. See [Part579](validation/part-579/README.md) and [backend scope](gen-statem-backends.md).

Part578: the timer alternative has36 explicit exports and named semantic contracts; the clean main-scoped tree passes the focused original comparisons and upstream tests. Preserve [the exact receipt](validation/part-578/README.md) and universal scheduling/resource/distribution/platform limitations. No whole-module ledger promotion or full regression/readiness refresh.

Part566 gen_event: all 44 non-metadata signatures have a managed alternative and unchanged original Erlang. Both pass 105 complete focused observations and 630 additional depth-profile observations, including optional callback and source-stack cases. The synthetic error prefix respects the configured depth. Preserve the failed parallel zero-timeout upstream-test attempt and the passing serial repeat; scheduler equivalence is unverified. The shared BEAM send instruction now preserves erlang:send/2 error metadata and frames. Full upstream/distribution/resource/platform/metadata acceptance is not certified; canonical readiness remains historical. See [gen_event backends](gen-event-backends.md) and [Part566](validation/part-566/README.md).

File alternative checkpoint (2026-11-04): all69 non-metadata APIs have managed bodies/shared native primitive routing and retained original Erlang. Both backends match408 exact focused observations; five independent full filesystem cycles fail in each because native/provider closure remains incomplete. In particular prim_file:open_nif/2 is unbound and RAM driver execution fails. The module is not 100% compatible. Full effects/source-stack/platform/resource and startup-manifest acceptance remain open. See [file backends](file-backends.md) and [Part568](validation/part-568/README.md). Full regression/readiness were deliberately not run.

Part565 timer: all36 library exports are explicitly selectable as C# or unchanged original Erlang. Focused baseline-bound contracts, authentic upstream tests and backtrace-depth profiles are recorded in [the timer receipt](validation/part-565/README.md). Default timer remains unregistered. Preserve physical scheduling/resource/platform/distribution and module-metadata limits; no universal100% or performance claim follows from finite observations. Full regression and canonical readiness are deliberately unrun. An ordinary current-source build still sees unrelated concurrent DETS draft errors; focused timer build evidence uses a recorded exclusion filter.

Current code port (2026-11-04): all69 exports have managed/original alternatives;156 complete observations and25 lifecycle results match per backend, with17 connected groups passing. Native coverage/debug remain the existing non-BEAMASM profile, distinct from OTP JIT. Full release/docs/gzip/platform/resource acceptance remains open. See [code receipt](validation/code-port-20261104/README.md).

[DETS](dets-backends.md): all64 explicit exports now have selectable C#/original Erlang implementations. Native v9 incremental storage, repair, ownership, callbacks/continuations, bchunk/QLC/ETS and connected raw-file/buffer/RAM primitives are implemented. Focused OTP acceptance and limitations are recorded in [the receipt](validation/dets-port-20261104/README.md); no full regression or canonical readiness refresh.

Current Unicode checkpoint: [Part563](validation/part-563/README.md) implements all22 exports with explicit C# and original Erlang selections. Both match66458 primary observations,490 permanent cases and29 backing probes;19 connected regression groups pass. Normalization uses pinned unicode_util instead of the host Unicode database. Universal resource/platform equivalence and global readiness are not inferred; full regression was deliberately excluded by the user.

Current string audit: [Part562](validation/part-562/README.md) closes reproduced converted-pattern, lazy-prefix, native membership/search and numeric/Jaro traversal gaps across all71 APIs. Each final backend matches37145 complete observations; full regression passes9649/9649. Original Erlang and explicit C# selection remain. The32KiB ten-second monitored Jaro profile is still a measured timing boundary; separately identified direct probes return correct values. Universal platform/resource/metadata equivalence is not asserted.

Current string checkpoint: all71 APIs have explicit managed/original selection, source-aligned errors/stacks, binary backing and depth-profile comparisons. The [Part561 evidence](validation/part-561/README.md) passes8522 cases in each backend and bounds C# UTF-8 lookahead instead of copying whole tails. See [string backends](string-backends.md) and retained Part560 failures. Default registration remains1 native primitive; selectable surfaces are71. Finite comparisons do not establish every resource policy or platform.

Current ordsets checkpoint: repaired missing native reverse helper frames in managed `lists:umerge/1`, observable through `ordsets:union/1` on malformed nested lists. Exact corpus evidence and remaining universal acceptance gates are in [ordsets compatibility](ORDSETS_COMPATIBILITY.md). No whole-module promotion.

Current math checkpoint: no differences observed in the Windows OTP29.1.1 corpus for either backend. Other operating systems and complete floating-point equivalence remain unverified;26/26 exports is not100% compatibility. See [math backends](math-backends.md).

Part484: the original BEAM priority save-pointer behavior is now preserved during cooperative length guards;the preceding candidate-revalidation experiment was incompatible and its failed original control is retained. This repairs a scoped receive subcontract and does not promote any full MFA. Part484 executes cooperative length/1 and numeric addition in original BEAM receive guards, closing megaco_trans_sender preparation without rewriting its Erlang algorithm. It reuses existing native operations and typed frame/root/handler checks. A genuine OTP running-trace priority scenario exposed an error in the candidate-revalidation prototype even though its6539 regression tests passed: preserve the running guard result and commit through the current save pointer; only actual host-side unlinking rescans. The new control compares the full result and64017-cell remaining queue (64036 raw ETF bytes). Original576 complete outcomes/queues also match main, warm and cold public .NET consumers. Final affected6539/6539,focus13/13 and final fixture-metadata7/7 pass. Megaco retains150 public checks/2003 prior outcomes and95 source/support assets. All previous1316 primary images remain prepared:1317/1321 (99.7%),Megaco65/65. Four bare-registry native captures remain:erts_internal,erl_ddll,ets,re. Preparation is distinct from full module/API/application/performance compatibility. Catalog8888→8895;708 APIs,118 full contracts,31 known differences,559 pending and1682 source bindings remain unchanged;API/full deltas0/0. Readiness uses an explicit prior8888+current-affected preflight;exact Part484 publication-head full CI/artifact remains required. Part483 mergedfa7afb371;its separate merge CI40183200007 and downloaded artifact independently pass8888/8888,25 whole corpora and17 cold families.


# Known semantic boundaries

Part 352 closes the complete unary contracts for six type predicates on the supported Erlang term model. `is_list/1` accepts improper Cons and nil; `is_function/1` does not inspect arity or code lifetime; PID, port and reference predicates do not inspect liveness. Native records remain separate types. The source partition covers representations not separately sampled, without claiming new native observations. All other 581 classifications and previous Verified scopes and evidence are unchanged.

Part351 implements process scheduling priority and owner admission, with21 production files, eight new HOST checks and three portable scenarios. Actual canonical local validation passes **7297 tests/11 integration outcomes/two packages**, preserving7286 earlier ordered names and twelve bounded child controls. Registry587; compatibility **10 Verified/24 Known differences/553 pending**, with74 current runtime-source guards. Scheduling priority now works; heap/GC/MQD/affinity options remain unsupported. statistics/1 has a source-proven valid-key refusal. Published350 CI passes7286/11 and original passes6313/fd8; its predeclared map stdout variance retains separate strict refusal. Current-head351 CI and new priority original comparisons remain pending publication. [Evidence](validation/part-351/). Physical BEAM scheduling/performance and whole spawn_opt parity are not claimed.

Post-full reporting correction: erlang:spawn_opt/2,3,4,5 move from Needs verification to Known differences because valid OTP priority, message_queue_data, heap, GC and scheduler options are rejected by the managed parser. This source-proven correction changes the final ledger to 10 verified,23 known differences,554 pending (erlang10/6/335); all583 other entries and59 source guards remain unchanged. The immutable full350 input ledger remains10/19/558; this is a later reporting change, not a rewritten full result. No new native execution or runtime implementation is claimed. The first documentation finalizer invocation refused a relative guard path before creating its overlay; the absolute-path invocation completed without SDK or Main mutation.

Part350 adds18 portable scenarios (six predicates/twelve numeric boundaries), with no production C# changes; a separate post-full source audit marks four spawn_opt APIs as Known differences. Actual canonical full passes **7286 tests/11integration outcomes/two package outputs**, preserving7268 old ordered names plus18 new cases and12 bounded child controls. Final registry reporting is587/14modules and **10 verified,23 known differences,554 pending**, with59 runtime-source guards; the immutable full-run input had10/19/558. Exact focused/source/raw evidence and failed harness/preparation/audit histories remain separate. Prior published349 CI independently passes7268/11 with ten verified contracts. Original348 independently passes6295/fd8 semantic contracts; one predeclared map stdout variance retains strict raw refusal without normalization. New original comparisons and current-head350 CI remain pending publication. [Evidence](validation/part-350/). Six new predicate witnesses and finite numeric boundaries do not constitute new complete API compatibility audits.

Part349 verifies exactly **erlang:is_atom/1,is_integer/1,is_float/1,is_number/1,is_tuple/1** using complete pinned-source contract review, actual original345 witnesses, current source and CI348. Registry remains587/14modules: **10 verified,19 known differences,558 pending**; erlang351 splits10/2/339. Preserve582 other entries and extend source guards35→59. Actual original345 passes6290/fd8 with strict raw equality; original346 real-peer8 passes typed/source/normal-cleanup gates. CI348 passes7268/11. Runtime remains3487268/11/two packages, no C# or new full run. Reader correction/EOL/incident histories and native identity/interoperability limits remain explicit. [Evidence](validation/part-349/). Predicates are verified for the supported term model; process_info physical omissions remain known. Native-only peer observations do not prove managed/native TCP compatibility.

Part348 replaces recursive compound-term comparison/internal hashing with explicit traversal frames, preserving scalar order, numeric exactness, node snapshots and the internal hash fold. Actual local validation passes **7268 tests,11 integration outcomes and two package outputs**, preserving all7259 previous ordered tests plus9 new permanent checks and12 bounded child controls. Old tuple compare/hash stack exhaustion at depth32768 is separately demonstrated; private green22/five compiled and permanent9 observations remain managed evidence. Registry remains587/14modules (erlang351,proc_lib39), with **5 verified,19 known differences,563 pending** and35 current source guards. CI346/347 each passes7259/11; original3456290/fd8 and corrected346real-peer8 remain pending. No new original, whole-module, native hash identity or performance parity claim. [Evidence](validation/part-348/). Internal CLR HashCode is process-seeded and is not Erlang phash; synchronous traversal still has no newly implemented scheduler/cancellation cooperation.

Part347 corrects only the reporting classification of erlang:process_info/1,2: default queries omit four native physical fields and explicit queries reject their valid keys. Counts are now **5 verified,19 known differences,563 pending**, preserving all other585 entries/evidence and30source bindings. Registry remains587/14modules. Runtime remains published345 **7259 tests/11integration outcomes/two packages**; no new build, full test or original result is claimed. Published345 CI independently passes7259/11. Fresh original3456290/fd8,CI346 and corrected346 real-peer8 remain pending. [Evidence](validation/part-347/). Physical heap/stack/GC omissions are proven contract differences; other query partitions remain separately scoped. Unverified does not conceal a known differing branch.

Part346 is a source/control correction to stop peer discovery after the first admitted PID, preserving exclusive-write and ownership gates. Runtime remains published345: **7259 tests,11 integration outcomes,two packages,587 APIs/14modules;5 verified,17 known differences,565 pending**. Published344 real-peer8 failed at a duplicate peer ownership-file write; seven normal acquisitions remain partial and the second attempted PID is unknown. Five deterministic controls and source review pass; fresh original8 under corrected manifestC1B3C8 is pending. No new SDK full run, runtime C# change, API promotion or TCP compatibility claim. [Evidence](validation/part-346/). Keep native acquisition/control failures separate from language semantic failures and finite compatibility claims.

Part345 follows pinned OTP guard BIF/local/import/no_auto_import resolution and adds38-category witnesses for five pending type predicates. Actual canonical local validation passes **7259 tests,11 integration outcomes and two package outputs**, preserving all7224 previous ordered tests plus35 new permanent targets. The first35 focused checks passed; a separate stale legacy attribute expectation and a separate invalid import-name admission probe were repaired; corrected41 and permanent35 pass. Registry remains **587 APIs across14 modules** (erlang351,proc_lib39), with **5 Verified compatible,17 Known differences,565 Needs verification**. Published342 original comparison passes6261/fd8; published343 passes6270/fd8 including nine length cases, with strict raw equality separately refused for one predeclared map stdout ordering variance. CI343 and344 each pass7224/11. Published344 real-peer8 failed at duplicate exclusive peer-file creation; seven other acquisitions are partial evidence, not complete8PASS. Fresh345 original verification remains pending. [Evidence](validation/part-345/). No type-predicate promotion follows solely from fixture presence or successful selected cases; retain the complete native/source/current-binding gate.

Part344 changes original-observation tooling only. Four procfs discovery handlers now tolerate a disappearing process; ownership, generation, permission and normal cleanup requirements remain enforced. Exact six-file source and deterministic controls pass independent review. The previous eight-case original run remains failed/incomplete, with seven partial acquisitions; no fresh original result or full SDK run is claimed. Runtime baseline343 remains7224 tests/11 integrations,587 APIs and5/17/565 compatibility counts. A new original eight-case capture requires the reviewed manifest61E52EC3EEE23FD4084772C0E383470C7FD09DAC6A6F16D7BD50AD3A6CB85229. Historical syscall and peer-crash/cleanup ordering are unproven. [Evidence](validation/part-344/).

Part343 implements cooperative erlang:length/1 across ordinary calls, async guards, transactional patterns and receive selection, while retaining the separate synchronous OTP match-spec path. Actual isolated validation passes **7224 tests, 11 integration outcomes and two package outputs**, preserving all7200 previous ordered tests and adding24 controls. The registry remains **587 APIs across14 modules** (erlang351, proc_lib39). Compatibility is **5 Verified compatible,17 Known differences,565 Needs verification**: length moves only to pending verification after its concrete scheduling defect is repaired. Nine proposed original-comparable length cases are not native observations. Published342 Windows CI passes7200/11. The published342 eight-case real-peer collector failed on its first MFA case with ProcessLookupError before peer identity or spawn results were recorded; the other seven have partial observations, not an eight-case comparison PASS. The6261 ordinary342 comparison remains pending. No complete erlang/proc_lib, TCP interoperability, physical VM or performance parity is claimed. [Evidence](validation/part-343/). Keep the historical5/18/564 part342 ledger immutable; only the length scheduling difference changes classification.

Part342 resolves anonymous/captured source functions in a fresh receiving runtime using the exact current/old code image, without executing the receiver constructor. Actual local validation passes **7200/7200 tests, 11 integration outcomes and two package outputs**, preserving all7194 previous ordered test names and adding six tests. Registry remains **587 APIs across14 modules** (erlang351, proc_lib39); compatibility remains **5 Verified compatible,18 Known differences,564 Needs verification**. The separate eight-case original real-peer collector and enlarged6261-case ordinary comparison remain pending; no whole-module, native/SDK TCP or performance compatibility is claimed. Preserve the pre-SDK CRLF-plan refusal and7184/16 sandbox native-pipe failure; the same-DLL approved permission probe and25 existing port cases pass, followed by a complete7200/11 run. Exact owned30 files are published;102 unowned closure differences are explicitly EOL-only, all2178 unowned C# and17 owned C# inputs match, and Main unowned bytes are preserved. Published341 Windows CI independently passes7194/11. [Evidence](validation/part-342/). Captured-function source execution is locally repaired; its known-difference classification remains until independent original observations are compared. Physical fields, spawn tuning and logger breadth remain explicit limitations.

Part341 changes reporting only: **587 implemented APIs**, **5 Verified compatible**, **18 Known differences**, **564 Needs verification**. [Module summary](MODULE_READINESS.md) and [command-level contracts, evidence and remaining checks](API_COMPATIBILITY.md) separate implementation presence from compatibility. The five verified logical contracts are erlang:self/0, node/0, is_map/1, is_binary/1 and is_bitstring/1. Current-source hashes invalidate stale verified claims. The new reporting admission/generator checks and actual CLI generation pass separately against the immutable 7193-test runtime baseline. Published part340 Windows CI is independently verified at7193/11; its pinned-original comparison completes6260/6260 plus fd8/8 with strict equality. No fresh full runtime run or whole-module/performance compatibility is claimed for this tooling increment. Known differences remain actionable; pending also includes commands not yet fully audited. The private captured-source-function repair is not delivered in this report increment.

Part 340 implements all 39 registered proc_lib exports, including six node-qualified spawn/spawn_link/spawn_opt overloads and real remote process labels through rpc/erpc. Actual validation passes 7193/7193 tests and 11 integration/package outcomes after the recorded failed launches. Retain the initial Windows PowerShell preflight exit 1, the second build failure caused by an omitted immutable build/Erlang.Net.targets input, and the third result of 7191 passed/2 failed. The first two did not reach guest tests; all 88 new tests passed in the third. Two historical negative protocol fixtures are explicitly migrated because their previously unsupported spawn/alias packets are now implemented, preserving malformed-frame and missing-feature refusals. The corrected pipeline uses PowerShell 7 and the complete tracked input set outside documentation. All 7105 previous ordered test names and 576 registry rows/custom Limits remain exact. Registry: 587 APIs across 14 modules; proc_lib 39/39, rpc 2/35, erpc 3/33; erlang 351 unchanged. Connected spawn/TT/link/monitor/priority/alias33/34, visible/hidden/current-generation nodes and apply/3 native1023-argument traversal are implemented. The new 44 portable cases, 11 direct contracts, 23 connected HOST checks and 11 RPC/lifecycle HOST checks pass. Original part 339 supplies 12 independently audited observations, with a precise contract crosswalk and explicit unmatched whole-trace fields. Published part 338 original 6216/fd8 and CI 7105/11 plus part 339 CI 7105/11 are complete; the old predeclared map stdout-order strict refusal remains separate. Current part 340 CI/original comparison awaits publication. The receiver must still resolve anonymous/captured source funs when its matching module is loaded but the closure constructor has never run there; current connected fun tests use external functions. This concrete gap is next, with an independent real two-node original observer. API presence is not whole-module compatibility: inherited native tuning, physical heap/stack/scheduler values, broad logger/RPC exports, physical TCP interoperability and performance parity remain partial.

Part338 implements proc_lib init_p/3,5, hibernate/3 and wake_up/3, shared initialization and real crash logger dependencies. Actual frozen-source 7105/7105 tests; a separate exact readiness/integration suffix completes11 outcomes after restoring one omitted immutable comparison report. The initial exit1 is preserved; no uninterrupted full-pass claim. All7017 previous ordered tests and540 registry rows remain exact. Registry576 across12 modules; proc_lib33/39. Add logger26, application4 and error_logger2 source-guided exports with real per-runtime configuration, guest callback delivery, standard streams and iterative term limiting. Local crash reports preserve data and field order but expose heap_size/stack_size as undefined because public CLR process_info deliberately lacks BEAM physical metrics. No physical values are fabricated; the two explicit platform alternatives are recorded. Published337 CI7017/11 and separate original6171/fd8 are independently complete, including all prior6114 ordered contracts and57 new formatter cases. The known map stdout-order variance remains a separate strict refusal. Six node proc_lib overloads, remote RPC label queries, full logger background/overload/configuration/formatting, physical scheduling/resources and performance parity remain unfinished. Current338 original verification awaits publication. API presence is not semantic completion.

Part337 delivers proc_lib format/1,2,3 and report_cb/2 with an independent76/76 original term comparison (57 reports including12 raw iolists,19 backend primitives). Actual full7017/7017 tests and11 integration/package outcomes pass; all6952 prior ordered names and536 registry rows/custom Limits remain exact. Registry540 across9 modules; proc_lib29/39, sys2/36, erlang351 unchanged. Original expectations come solely from independently captured pinned OTP returns. Both source-derived28 pretty assertions and actual callback/deep-continuation controls pass separately. Source37 new production files and exact package6 are bound. Published335 CI6952/11 and original6114/fd8 are independently complete, with the existing map stdout variance retained as a strict refusal. Published336 collector76 and CI6952/11 are verified. Ten proc_lib exports remain: initializer/hibernate/wake-up and six node overloads, with genuine crash logging and connected spawning still being implemented. Generic IO formatting, broad Unicode-version/configured printable-range, native physical/scheduler and performance equivalence remain partial. Current337 CI/enlarged ordinary oracle await publication; API presence is not whole-module completion.

Part336 publishes a standalone original formatter observation collector, not a runtime increment. Published334 original passes6085/6085 plusfd8 with all6042 previous ordered contracts and43 new metadata/label cases verified; one predeclared map stdout-order variance remains a separate strict refusal. Published335 remains the runtime baseline:6952 local tests/11 integrations,536 APIs in9 modules, proc_lib25/39 and sys2/36. Current335 CI/original are pending. The private formatter has57 SDK report observations,19 primitive observations,28 source-derived backend assertions and an actual callback-await control; no original formatter match or four new exports is claimed. Continue the real lifecycle/crash-report and connected-node dependencies.

Part335: proc_lib stop creates its outer monitor before sys dispatch, catches exit only and shares one monotonic timeout budget through the final real DOWN. A different exit before ACK retains the sys wrapper; after ACK its DOWN reason is returned directly as an exit. Local infinite gen calls preserve raw noconnection while finite/remote alias calls translate it to nodedown. Invalid-timeout error-class paths retain the source outer-monitor behavior. System ACK precedes terminate callback/child shutdown; aliases suppress late replies. Broader sys/gen protocol and live connected-peer equivalence remain incomplete.

Part334: proc_lib initial-call queries preserve specific dictionary-item precedence over dictionary-list fallback; initial_call makes Argument__1..N atoms and translate_initial_call retains the native default. Labels are arbitrary real dictionary values, return ok, do not inherit into children, and unavailable/invalid targets return undefined through the source exception boundary. Connected remote RPC and nonlocal numeric PID-channel parsing remain incomplete; lifecycle/crash reports, report formatting and unrestricted native tuning are not completed by these four APIs.

Part333: start_monitor ACK retains its original real monitor. NACK, early DOWN and timeout paths send the same consumed DOWN to the mailbox tail through actual erlang send. Option-bearing startup checks MFA guards before ordered lists:member/keyfind monitor rejection; wrong-arity monitor tuples retain case_clause. Existing option parsing, atomic links/monitors and spawn wrapper metadata are reused. Other native tuning options, remote overloads, full stack/tracing/scheduler/physical parity remain incomplete.

Part332: matching queued messages precede invalid timeout VALUE rejection; the timeout EXPRESSION still executes once before selection, including its side effects/errors. Startup uses actual ack/nack/DOWN and removes a link before timeout kill. Existing zero-timeout snapshot policy is retained without claiming exact VM concurrent-arrival scheduling. Crash logging, complete stack parity and gen/sys integration remain incomplete; original332 is pending.

Part331 proc_lib preserves is_list Cons/Nil guards including improper lists: invalid nonlist inputs fail in the caller, while improper MFA arguments fail in the child. Only ancestry is inherited, arbitrary init_ack terms are accepted, and registered parent/native wrapper initial-call identities are retained. Crash logging, full stack shape, remote/start/options APIs and new proc_lib-specific original tracing remain incomplete. Original331 is pending; corrected330 guest cleanup original2/SDK2 matches exact329 within the recorded depth256/100ms scope.

Part330 closes a test-catalog gap with separate named/anonymous recursive try-after external-kill observations. Both local SDK cases pass; source-built original comparison is pending. The100ms no-cleanup observation is bounded evidence, not a universal interleaving proof. Managed source-image leases and actual Unwound have no direct native object-equivalence claim.

Part236 retains failed235 originalcomplete5726/5727 with1 inherited map-action-order discrepancy;all14new hard-pattern cases match,fd8/8 andexact-headWindowsCI6407/full pass. All4804SDKcontracts/4798comparable/151diag/778expressions/sixexclusions and actualsource-build/job/log/ZIP/extracted-byte provenance retained. SeparateBEAMjobfailedversionpath beforeexport (0fixtures), repairedconsistent PREFIX/lib/erlang paths withconfigureprefix unchanged; newrunpending. Flatmap atom-key indices/backwards values explain contextualfirst/second observations; actualindicesunmeasured andruntimefixpending. Registry484;erlang326/35192.9% presence,wholemodulepartial; no native success promotion or loader proof.

Part235 ports hard-pattern termination/deferred composite traversal/recoverable same-clause diagnostic boundaries and adds standalone original BEAM fixture tooling/manual source-built workflow. Main and isolated6407/6407/full pass;4790 inherited ordered complete source/generated/outcome/context/policy/stream contracts unchanged,14new sources,4804SDK/4798comparable/151diagnoses/sixexclusions/484MFAs. Registryerlang326/35192.9% presence,wholemodulepartial. Historical234original5713+fd8/CI6393 remainsverified; new5727+fd8/CI6407 and genuine5BEAM/4originalexecution outputs pending publication. No CLR loader or physical native resource proof, new dependency or public package publication.

Part234 verifies published233/fd3f150: complete5713/5713 source-built OTP-29.1.1 comparisons (778expressions,4784modules,151diagnoses), independentfd8/8 and exact-headWindowsCI6393/full. Allordered4790SDKcontracts/sixexclusions/source/generatedhashes/outcomes/context/policy/bytes and actualsource-build/job/log/artifactdigests retained. Six corrected232sources and43newscenarios independently match; failed232 remainsimmutable. Registry484;erlang326/35192.9% APIpresence, wholemodule/physicalresources/profiler/loading/distribution/upstream/performance remainpartial. Current4096allocationgatepasses, priorfailurecauseunknown.

Part233 repairs six pinned232 discrepancies with explicit inherited-source corrections and adds real system_profile controls/lifecycle. Main and isolated6393/6393/full pass;4747 inherited contracts retain independent outcomes/order/context/policy/bytes,43new sources,151 diagnoses/six exclusions unchanged. Registry484;erlang326/35192.9% API presence,wholemodulepartial. Native5713+fd8/exact-headCI6393 pending publication; failed232 remains immutable. Original CPU stale exception registers and physical profiler effects remain explicit gaps; no fake events, allocation budget increase or native outcome normalization.

Part232 preserves FAILED original231/d2e2e03 complete5664/5670 with6 source discrepancies,4741modules/151diagnoses/778expressions and no abort;independentfd8/8 passes. WindowsCI6346/6347 failed inherited4096 allocation host with actualbytesunknown, fullpipelinefalse. Exact4747 SDK contracts/order/hashes/outcomes/context/policy/finitebytes/151diagnoses/sixexclusions and pinnedsource/artifact/log/extractedreport provenance retained. Registry482;erlang324/35192.3% presence,wholemodulepartial. Source-guided repairs isolated; no Main behavior/outcome/exclusion change. Failed evidence remains immutable.

Part231 integrates actual CPU configuration,match-spec/local process tracing,queue monitoring,wall/userCPU statistics,owner diagnostic display andEXIT token/origin semantics. Main and isolated6347/6347/full pass;4496 inherited ordered complete contracts,151 diagnoses and six exclusions unchanged,251 new sources and eight hosts. Registry482;erlang324/35192.3% API presence. Original5670+fd8/same-headCI pending232;2305419/5419+fd8/CI6076 remains previous proof. Closed0033 unchanged,new0034; no historical107 snapshot import. Whole erlang/physical resources/scheduler/heaps/loading/distribution/upstream/performance partial.

Part230 verifies published229/029aa00: complete5419/5419 source-built OTP-29.1.1 comparisons (778 expressions,4490 modules,151 diagnoses), independent fd8/8 and exact-head WindowsCI6076/full. Exact ordered contracts/source/generated hashes/outcomes/context/policies/bytes, six exclusions and pinned source/artifact/log provenance retained; stale individual API step states recorded. Registry470;erlang312/35188.9% API presence. Failed226/228 immutable; whole erlang, physical resources/scheduler/heaps/loading/distribution/upstream/performance partial. Combined6347/full candidate is separate unpublished work.

Part229 repairs original228 observer-context/sequence-destination/synchronous tagged-clock/asynchronous initializer discrepancies. Main and isolated6076/6076/full pass;4476 inherited identities/order/independent outcomes retained,8 explicit source corrections recorded,20 new scenarios,151 diagnoses and6 exclusions unchanged. Registry470;erlang312/35188.9% API presence. Raw native abort output/stage retained without decoder/outcome normalization. Original5419+8fd and same-head CI pending230; failed228 remains immutable. Whole erlang and physical heaps/scheduler/resources/code-loading/distribution/upstream/performance remain partial.

Part228 original227 FAILED/ABORTED:5399 planned,5200 executed,5195pass5fail,4422 module rows/no diagnoses;199unexecuted. Raw failure/aborted source/digests retained;fd8/8 and exact-head WindowsCI6056/full pass separately. Two process-info assertions use unequal monitored runner contexts; sequence destination/spawn serial/payload and ETF abort stage require source audits. No source/runtime repair, exclusion or outcome weakening in this checkpoint. Main implementation remains227/9f716f4; registry470,erlang312/35188.9% API presence, whole erlang partial. Prior LinuxCI wording corrected: CI windows-latest; original ubuntu-latest.

Part227 repairs the actual original225 discrepancies: spawn_request wrapper redispatch/callback error metadata, native field access permissions, and constructor-only underscore validation. Main and isolated6056/6056/full pass. All4454 inherited contracts remain, exactly3 source corrections recorded,22 scenarios appended and exact aborted source becomes diagnostic151; six exclusions unchanged. Registry470;erlang312/351 (88.9% API presence). Original5399+8fd and same-head CI pending228; whole erlang remains partial. Failed226 evidence and initial6053/6054 fixture error retained.

Part226: original225 FAILED/ABORTED planned5376/executed5071,5069pass2fail,4293module rows/no diagnoses;305unexecuted. Wrapper atom-argument redispatch and anonymous-private field access differ;quoted underscore local construction originalvalidatorabort. Raw failures/provenance retained;fd8/8 andCI6033/full separatelypass. Source repair in isolatedcopy,Main225 frozen. Registry470;erlang312/35188.9% presence notfullcompatibility. No exclusions/outcomeweakening;whole erlang continues.

Part225: source-guided spawn_request,native records/types/defaults/definitions,observable process_info and actual sequential/system reply tracing.6033/6033/full Windows main/draft,470 directMFAs;all4186 inherited contracts preserved,268 appended modules and18 new diagnoses.4454 SDK/4448 comparable/150 diagnoses;original5376+8fd and same-head CI pending226. Registry470;erlang312/35188.9% API presence,records8/8,lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53. Native2 numeric hash assertions remain SDK-derived pending original proof;physical heaps/scheduler/resources,complete lint/warnings/stacks/code staging/distribution/upstream/performance partial. Unpublished token assertion corrected from original producer sentinel;raw prior/cancelled evidence retained,no published outcome/exclusion changed. Closed0032 unchanged,new0033 active. Whole erlang goal continues.

Part224: frozen223 original5090/5090 + independent inherited fd8/8; same-head Linux CI5720/5720/full with identical ordered Windows test names. Selected CPU formatter and major/minor managed GC request/reply cases agree with OTP. Registry449;erlang299/351 (85.2% API presence,52 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Full module,physical BEAM heap/scheduler/resources/distribution/upstream and performance remain partial.

Part223: pure format_cpu_topology/1 and real garbage_collect/0,1,2 sync/async process requests;garbage_collect_message_area/0 follows actual original undefined stub. **5720/5720/full Windows main and isolated composite draft**,54 new compiled scenarios plus actual GC/death-race host check;all4132 inherited source/outcome/generated hash/context/policy/finite byte contracts,132 diagnoses and six exclusions preserved. Registry449;erlang299/351 (85.2% API presence,52 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Original5090 corpus plus8 fd and exact-head CI pending224;2225036/5036+8/8 and5660/full CI historical. Global CLR collection differs from BEAM per-process heaps;physical scheduling/resources/distribution/full upstream contracts remain partial. Continue whole erlang with completed isolated spawn_request/native-record foundation and further agent slices.

Part222: repaired221 head **5036/5036** original corpus comparisons plus **8/8 inherited fd IO**,**5660/full exact-head Linux CI**. Hibernate255/256 wake boundaries now agree with original undef-after-wake behavior;all39 hibernate/delay scenarios pass. Failed initial2205033/5034 remains in221,without exclusions. All4132 source/outcome/generated hashes/context/policy/finite bytes,132 diagnoses,six exclusions preserved. Registry444;erlang294/351 (83.8% API presence,57 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Physical BEAM heap/shrinking/GC/scheduler/resources/distribution/full upstream remain partial. Continue whole erlang with CPU/GC,spawn/native and agent process-info/seq-trace/native syntax slices.

Part221: initial220 original **5033/5034** plus **8/8 inherited fd**,exact-head5658/full CI;sole mismatch hibernate3 arity256 after wake. Source distinguishes courtesy MAX_ARG255 from apply MAX_REG1024,so missing arity256 export raises undef after wake. Removed incorrect wake cap;one explicit fixture assertion correction keeps independent done,two new dormant255/256 boundary scenarios appended. **5660/5660/full Windows repair**,all4129 other source/hash contracts and all4130 independent outcomes/context/policy/finite bytes preserved,132 diagnoses/six exclusions unchanged. Registry444;erlang294/351 (83.8% API presence,57 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Patched original5036+8 and CI pending222. Physical heaps/GC/scheduler/resource/distribution/full upstream remain partial. Continue whole erlang goal;CPU/GC/spawn/native prototypes are separate unpublished next slices.

Part220: hibernate/0,3 and delay_trap/2 implemented with actual mailbox waiting,discarded old catch/after stack for MFA hibernation and preserved continuation for hibernate0. Unsigned32 receive timeouts use segmented CLR waits. **5658/5658/full Windows main and isolated draft**,37 new source contracts;all4093 inherited source/outcome/hash/context/policy/finite-byte contracts,132 diagnoses and six exclusions preserved. Registry444;erlang294/351 (83.8% API presence,57 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Original5034 corpus plus8 inherited fd and exact-head CI pending221;2194997/4997+8/8 and5618/full CI remain historical. Physical per-process heap shrinking/GC,scheduling/distribution/resources/full module contracts remain partial. Continue whole erlang goal and integrate independently audited agent work.

Part219:218 spawn_opt head **4997/4997** original corpus comparisons plus **8/8 inherited fd IO**,**5618/full exact-head Linux CI**. All46 new spawn_opt scenarios execute with the original,covering overloads,atomic link/monitor,tag/alias/priority delivery,duplicate/reset/badopt metadata,self-node/unconnected and child failures. Registry441;erlang291/351 (82.9% API presence,60 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. CPU scheduling priority,heap/GC/message-queue allocation/scheduler/async_dist options,connected distribution,physical resources and whole erlang/upstream/performance remain partial. Continue the whole erlang goal with hibernate/delay prototypes.

Part218: four real spawn_opt2/3/4/5 exports with atomic link/monitor installation,custom tags/aliases/priority-delivery flags,duplicate overwrite/type/option/error-info contracts and self-node/unconnected fallback. **5618/full Windows main and isolated draft**,46 new source contracts;all4047 inherited sources/outcomes/hashes/contexts/policies/finite bytes,132 diagnoses and six exclusions preserved. Registry441;erlang291/351 (82.9% API presence,60 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53. Original4997 corpus plus8 inherited fd IO/same-head CI pending219;2174951/4951+8/8 with5568/full CI remains separate. Non-link/monitor CPU priority/heap/GC/message queue allocation/scheduler/async_dist,connected distribution/resources/full contracts remain partial;continue whole erlang goal.

Part217: corrected216 head **4951/4951** source-built original corpus comparisons plus **8/8 independent inherited fd IO** and **5568/full exact-head Linux CI**. All five initial corpus/fd IO mismatches from216 are fixed;initial4946/4951 and3/8 remain immutable failures. Registry437;erlang287/351 (81.8% API presence,64 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Selected fd tuple/types/settings/metadata,actual stream/list/packet1/2/4/line/EOF/no-child status behavior verified. Windows local5568/full and idle-reader cancellation belong to216;native original Linux proof is separate. FD TTY/control/flags/concurrent external descriptors/missing standard handles,other platforms/native ABI/resources/scheduling/distribution/upstream/performance remain unfinished. Continue the whole erlang goal with source-guided spawn_opt and hibernate prototypes.

Part216: original215 run completed **4946/4951** plus independent inherited fd **3/8**,with five fd metadata/settings failures in each track;no historical failures. Exact-head Linux CI5567/full passed separately. Fixed fd os_pid to {os_pid,undefined} in singleton/full info and rejected exit_status per original. **5568/full Windows local repair**,all4033 old contracts preserved;five explicit new-fixture corrections retain independent done outcomes. Patched original4951 plus8 inherited fd comparisons pending217. Reporter checkpoint batching retains every final/failure/abort row. Registry437;erlang287/351 (81.8% API presence,64 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. FD TTY/flags/control/concurrent external resource use,native ABI/platform/distribution/resources/upstream/performance remain unfinished;continue whole erlang goal.

Part215: borrowed fd resource transport, actual Linux descriptors/Windows handles, no child os_pid/exit_status;14 compiled parsing/metadata contracts plus8 real pipe/file IO checks and1 idle-reader cancellation. **5567/full Windows local pipeline**, all4033 inherited source/outcome/generated hash/context/policy/finite byte contracts and132 diagnoses/six exclusions preserved. Registry437;erlang287/351 (81.8% API presence,64 missing),lists91/91,maps34/34,gen_server10/41,supervisor14/23,io1/53 unchanged. Original4951 corpus plus8 independent inherited Linux fd IO comparisons and same-head CI pending216. Historical2144937/4937 with5544/full CI remains separate. FD TTY/window-size/flags/concurrent external descriptor lifecycle, native ABI/platform/errno/resource/scheduling/distribution/upstream/performance remain unfinished;continue the whole erlang goal.

Part214:source-built original **4937/4937** and **5544/full same-head CI** atcc37f72. All selected comparisons pass;continue remaining erlang/port scope. [Proof](validation/part-214/differential.json),[guard](validation/part-214/strict-guard.json).170 port source cases (28base/21line/47lifecycle/12packet-boundary/19output-pressure/18message-pressure/25native-options) execute actual owned resources and preserved failure/exit/monitor semantics. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native driver ABI/fd/remaining options/metadata/msgq busy/full scheduling/platform/distribution/resources/upstream/performance remain unfinished.

Part213: remaining spawn-port option parsing now controls real Windows handles and Linux64 GNU libc descriptors: in/out, shared stderr_to_stdout, independent arg0, nouse_stdio and overlapped_io. **5544/full main and isolated draft on Windows**, 25 new source scenarios; all 4008 inherited contracts/hashes/policies/bytes and six exclusions preserved. [Tests](validation/part-213/tests.json), [corpus](validation/part-213/corpus-audit.json). Linux execution, original4937 and same-head CI are pending214; historical2124912/4912 and5519/full remain separate. Registry437; erlang287/351,64missing,81.8% API presence, not semantic completion. Native driver ABI/fd, Windows batch/DOS formats, other libc/32-bit/macOS transport, all errno/resource/scheduling/distribution/upstream/performance contracts remain unfinished.

Part212:source-built original **4912/4912** and **5519/full same-head CI** at064d136. All selected comparisons pass;continue remaining erlang/port scope. [Proof](validation/part-212/differential.json),[guard](validation/part-212/strict-guard.json).145 port source cases (28base/21line/47lifecycle/12packet-boundary/19output-pressure/18message-pressure) execute actual owned resources and preserved failure/exit/monitor semantics. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native driver ABI/fd/remaining options/metadata/msgq busy/full scheduling/platform/distribution/resources/upstream/performance remain unfinished.

Part211:distinct process-to-port signal byte queue with busy_limits_msgq high> and low< hysteresis,disabled/clamped positive limits;Erlang !/send2/send3/send_nosuspend await actual pressure or decline without sending. Protocol close drains accepted writes below the driver high watermark. **5519/full main and isolated draft**,18 new source cases plus OS file-drain check;all3990 inherited fixture contracts/hashes/policies/bytes and six exclusions preserved. [Tests](validation/part-211/tests.json),[corpus](validation/part-211/corpus-audit.json). Original4912 and same-head CI pending212;historical2104894/4894 and5500/full remain separate. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native partial-write accounting/task memory/physical scheduling,C# synchronous host helper,fd/stdio/arg0/shared stderr/remaining options/distribution/resources/upstream/performance remain unfinished.

Part210:source-built original **4894/4894** and **5500/full same-head CI** at2e42c46. All selected comparisons pass;continue remaining erlang/port scope. [Proof](validation/part-210/differential.json),[guard](validation/part-210/strict-guard.json).127 port source cases (28base/21line/47lifecycle/12packet-boundary/19pressure) execute actual owned resources and preserved failure/exit/monitor semantics. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native driver ABI/fd/remaining options/metadata/msgq busy/full scheduling/platform/distribution/resources/upstream/performance remain unfinished.

Part209:real asynchronous output byte queue replaces semaphore-based busy detection. busy_limits_port high/low hysteresis,disabled mode,nosuspend rejection and regular admission resume;queue_size,parallelism scheduling hint and atom line default512. **5500/full local pipeline**,19 new source cases;all3971 inherited fixture contracts/hashes/policies and six exclusions preserved. [Tests](validation/part-209/tests.json),[corpus](validation/part-209/corpus-audit.json). Original4894 and same-head CI pending210;historical2084875/4875 and5481/full remain separate. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native partial-write accounting/scheduling,msgq backpressure,fd/stdio/arg0/shared stderr/remaining options/resources/distribution/upstream/performance remain unfinished.

Part208:source-built original **4875/4875** and **5481/full same-head CI** at99a0c02. All selected comparisons pass;continue remaining erlang/port scope. [Proof](validation/part-208/differential.json),[guard](validation/part-208/strict-guard.json).108 port source cases (28base/21line/47lifecycle/12packet-boundary) execute actual owned resources and preserved failure/exit/monitor semantics. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native driver ABI/fd/remaining options/metadata/busy/full scheduling/platform/distribution/resources/upstream/performance remain unfinished.

Part207:packet1/2 output overflow now accepts the command then closes the real driver with einval/EXIT/DOWN;255/65535 inclusive boundaries,empty frames and malformed iodata distinguish driver failure from badarg. **5481/full main/draft**,12 new source cases. [Tests](validation/part-207/tests.json),[corpus](validation/part-207/corpus-audit.json). Registry437/erlang287/351 unchanged;64missing,81.8% presence,not semantic completion. Original4875 pending208;2064863/4863 with5469/full CI remains separate. Native ABI/fd/remaining options/metadata/busy/platform/full resources/upstream/performance unfinished.

Part206:source-built original **4863/4863** and **5469/full same-head CI** at7485cd7. All selected comparisons pass;continue remaining erlang/port contracts. [Proof](validation/part-206/differential.json),[guard](validation/part-206/strict-guard.json).96 port scenarios (28base/21line/47lifecycle) execute actual owned probes,monitors/aliases/tags/priority/demonitor/death/exit/links. Registry437;erlang287/351,64missing,81.8% API presence,not semantic completion. Native ABI/fd/other options/metadata/busy/full signal/platform/distribution/resource/upstream/performance remain unfinished.

Part205:source-directed port monitors/DOWN/name/alias/tag/priority/demonitor and observer/owner death cleanup;explicit exit/exit_signal normal/custom/kill=>killed,priority/dead/foreign links and actual monitored_by. **5469/full main/draft**,47 new source scenarios. [Tests](validation/part-205/tests.json),[corpus](validation/part-205/corpus-audit.json). Registry437/erlang287/351 unchanged (81.8% presence,64 missing). Original4863 pending206;2044816/4816 with5422/full same-head CI remains historical proof. Native ABI/fd/other options/metadata/busy/full signals/platform/distribution/resources/upstream/performance unfinished;continue the whole goal.

Part204:source-built original **4816/4816** and **5422/full same-head CI** atd86498f. All selected comparisons pass;continue remaining contracts. [Proof](validation/part-204/differential.json),[guard](validation/part-204/strict-guard.json).49 port scenarios execute actual portable child processes,IO/packet/EOF/status,ownership/names/data,callback rejection and command information barrier. Registry437;erlang287/351,64 missing,81.8% API presence,not semantic completion. Full native drivers/fd/all options/metadata/port monitors/exit2/priority links/busy/platform errno/distribution/resources/upstream/performance remain unfinished. Source-directed port monitors and exit are the next unit;whole goal continues.

Part203:source-directed {line,N} mode with original four byte states,CRLF/exact-cap eol/noeol,EOF tail delivery and explicit-close discard. **5422/full main and draft**,21 new source scenarios;437 MFAs unchanged,erlang287/351 (81.8% presence,64 missing). [Tests](validation/part-203/tests.json),[corpus](validation/part-203/corpus-audit.json). Original4816 pending204;2024795/4795 with5401/full CI verified independently. Native drivers/fd/all settings/metadata/monitors/exit2/priority links/busy/platform errno/distribution/resources/upstream/performance remain unfinished. Continue port lifecycle and the whole erlang goal.

Part202:source-built original **4795/4795** and **5401/full same-head CI** at2e345a1. All selected comparisons pass;continue remaining contracts. [Proof](validation/part-202/differential.json),[guard](validation/part-202/strict-guard.json).28 new port scenarios execute actual portable child processes,IO/packet/EOF/status,ownership/names/data,callback rejection and command information barrier. Registry437;erlang287/351,64 missing,81.8% API presence,not semantic completion. Full native drivers/fd/all options/metadata/port monitors/exit2/priority links/busy/platform errno/distribution/resources/upstream/performance remain unfinished. Source-directed line mode is being validated in an isolated prototype;whole goal continues.

Part201: implement13 port MFAs over real owned external-process resources;spawn/spawn_executable,stream/binary/packet1/2/4,args/cd/env,eof/exit status,owner links/shared names/data,close/death cleanup and driver callback rejection. **5401/full local** with28 new source scenarios and an actual executable/OS-PID/owner-death host check. [Tests](validation/part-201/tests.json),[corpus](validation/part-201/corpus-audit.json). Registry437;erlang287/351,64 missing,81.8% API presence,not semantic completion. Original4795 pending202;historical2004767/4767 with5359/full CI remains separate. Native drivers/fd/all options/metadata/busy watermarks/full signal scheduling/port monitors/platform errno/distribution/resource/performance remain unfinished;incoming packets have explicit16MiB managed cap. Continue port coverage and the whole erlang goal.

Part200:source-built original **4767/4767** and **5359/full same-head CI** atb842b49. All selected comparisons pass;continue remaining APIs. [Proof](validation/part-200/differential.json),[guard](validation/part-200/strict-guard.json).46 cookie contracts and all68 independent output contracts checked;one finite map-order alternative and one source-preserving SSA policy unchanged from199. Registry424;erlang274/351,77 missing,78.1% API presence. Full auth/distribution/ports/HAMT/native metadata/resources/upstream/performance remain unfinished;whole goal continues.

Part199:real owned save_calls ring and queued process_flag3 with target/death handling;extend existingprocess_flag2 and process_info2 last_calls. **5359/full main and draft**;42 new source scenarios and32 queued-death/ring host. All3821 inherited source/outcome/hash/context/policy/finite byte contracts preserved. [Audit](validation/part-199/corpus-audit.json),[tests](validation/part-199/tests.json). Registry424;erlang274/351,77 missing,78.1% API presence. Original4767 pending200;1984725/4725 with5315/full same-head CI verified separately. Native BEAM/JIT/internal/sensitive/physical scheduling/heap/resources/upstream/performance parity unfinished;whole goal continues.

Part198:source-built original **4725/4725** and **5315/full same-head CI** at7b90493. All selected comparisons pass;continue remaining APIs. [Proof](validation/part-198/differential.json),[guard](validation/part-198/strict-guard.json).46 cookie contracts and all68 independent output contracts checked;one finite map-order alternative and one source-preserving SSA policy unchanged from197. Registry423;erlang273/351,78 missing,77.8% API presence. Full auth/distribution/ports/HAMT/native metadata/resources/upstream/performance remain unfinished;whole goal continues.

Part197:source-guided current-state monitor_node2/3,dmonitor_node3,disconnect_node1 with actual nodedown and kernel gen_server infinity/retry protocol;**5315/full main and draft**.72 new source scenarios and named host;all3749 old contracts/policies/finite bytes preserved. [Audit](validation/part-197/corpus-audit.json),[tests](validation/part-197/tests.json). Registry423;erlang273/351,78 missing,77.8% API presence. Original4725 pending198;1964653/4653 and5238/full same-head CI remain separate proof. Connected monitor/channel lifetime,active-connect,dynamic naming,transport/resources/upstream/performance unfinished;whole goal continues.

Part196:source-built original **4653/4653** and **5238/full same-head CI** ateb24acd. All selected comparisons pass;continue remaining APIs. [Proof](validation/part-196/differential.json),[guard](validation/part-196/strict-guard.json).46 cookie contracts and all68 independent output contracts checked;one finite map-order alternative and one source-preserving SSA policy unchanged from195. Registry419;erlang269/351,82 missing,76.6% API presence. Full auth/distribution/ports/HAMT/native metadata/resources/upstream/performance remain unfinished;whole goal continues.

Part195:actual auth-service cookie wrappers get_cookie0/1,set_cookie1/2 and source-guided internal atom-key map printing;**5238/full main and draft**.46 new compiled cases;3703 old sources/outcomes/generated hashes/contexts/primary bytes preserved,exactly two194-backed metadata migrations. [Audit](validation/part-195/corpus-audit.json),[tests](validation/part-195/tests.json). Registry419;erlang269/351,82 missing,76.6% API presence. Original4653 pending196;1924605/4607 and194120 diagnoses remain separate. Automatic auth startup/cookie initialization/networking/HAMT/platform/resources/upstream/performance unfinished;whole goal continues.

Part194:120 native compiler/pass/context diagnoses complete,zero compatibility-corpus coverage;**5186/full same-head CI** at3e39638. Node-qualified tuple spawn returns PID after default SSA continuation removal;isolated no_ssa_opt retains done in all three contexts. Raw map printer uses internal atom identity order. [Matrix](validation/part-194/outcome-matrix.json),[guard](validation/part-194/strict-guard.json). Sources/expectations/policies/exclusions unchanged;retain1924605/4607. Registry415;erlang265/351,86 missing,75.5% API presence. Next internal map printer order and narrowly audited fixture policy;whole module continues.

Part193:isolated native investigation now includes both retained192 failures in120 compiler/pass/context evaluations. **5186/full local**;production/MFAs/all3703 fixture contracts and six exclusions unchanged. Native matrix pending194;192 remains4605/4607. [Audit](validation/part-193/reference-audit.json),[tests](validation/part-193/tests.json). Pinned erl_utils.h confirms flatmap atoms use internal identities;display order is not name order. Registry415;erlang265/351,86 missing,75.5% API presence. Repair only after independent diagnosis;whole goal continues.

Part192:source-built original **4605/4607** and **5186/full same-head CI** atde1ea33. 2 discrepancies retained;repair before promotion. [Proof](validation/part-192/differential.json),[guard](validation/part-192/strict-guard.json). All3703 SDK contracts/3697 comparable modules,778 expressions,132 diagnoses,six exclusions and68 independent byte expectations preserved. Registry415;erlang265/351,86 missing,75.5% API presence,not semantic completion. Preserve1884488/4525 and explicit37 output migrations;networking/dynamic naming/nativeGC/host imports/platform/resources/grammar/upstream/performance and remaining APIs continue.

Part191:node-qualified spawn/spawn_link/spawn_monitor2/4 execute actual local callbacks/MFA or create owned local crasher processes for unconnected remote nodes. Atomic monitor/link,group leader inheritance,noconnection EXIT/DOWN/slot cleanup and input/error frames guarded. **5186/full main and draft**,49 new source scenarios;all3654 inherited contracts unchanged. Registry415;erlang265/351,86 missing,**75.5% API presence**,not semantic completion. [Tests](validation/part-191/tests.json),[audit](validation/part-191/reference-audit.json). Original4607 pending192;latest190 evidence is separate. Connected transport/remote execution/handshakes/logger warning parity/resources/upstream/performance and remaining APIs continue.

Part190:source-built original **4558/4558** and **5130/full same-head CI** at565aa8b. All selected comparisons match;continue remaining APIs. [Proof](validation/part-190/differential.json),[guard](validation/part-190/strict-guard.json). All3654 SDK contracts/3648 comparable modules,778 expressions,132 diagnoses,six exclusions and68 independent byte expectations preserved. Registry409;erlang259/351,92 missing,73.8% API presence,not semantic completion. Preserve1884488/4525 and explicit37 output migrations;networking/dynamic naming/nativeGC/host imports/platform/resources/grammar/upstream/performance and remaining APIs continue.

Part189:owned nodes0/1/2/is_alive0 query actual local/named identity and weakly retained foreign PID/port/reference containers decoded into this runtime. Native printf output inserts CR before every LF,repairing188 byte discrepancies;raw display_string/delimiter logic unchanged. **5130/full main and draft**,33 new source cases;3621 inherited sources/outcomes/hashes/contexts/policies unchanged,37 explicit output migrations. Registry409;erlang259/351,92 missing,**73.8% API presence**,not semantic completion. [Tests](validation/part-189/tests.json),[migrations](validation/part-189/output-migrations.json). Original4558 pending190;retain1884488/4525 and1864485/4486. Networking/dynamic naming/nativeGC/host imports/platform/resources/grammar/upstream/performance unfinished. Close0027 after188;open0028.

Part188:original **4488/4525** complete and **5092/full same-head CI** atc1702e7. 37 byte discrepancies retained;repair before promotion. All33 suspension/timer scenarios pass,including corrected public-reference lifetime and wrapped delivery;37 display payloads differ by added CR before LF. [Proof](validation/part-188/differential.json),[mismatches](validation/part-188/mismatches.json),[guard](validation/part-188/strict-guard.json). All3621 source/contracts and68 byte expectations preserved. Registry405;erlang255/351,96 missing,72.6% API presence,not semantic completion. Trace native printf newline layer,repair in189 and continue remaining APIs.

Part187:real erlang display1 uses iterative native term formatting/raw stdout and qualified literal fun references lower through make_fun. Paused timers now invalidate old public references and resume preserved messages/deadlines with fresh private identity,repairing original186 discrepancy. **5092/full main and draft**,39 new modules and one explicit disproved fixture migration;3581 other contracts unchanged. Registry405;erlang255/351,96 missing,**72.6% API presence**,not semantic completion. [Tests](validation/part-187/tests.json),[migration](validation/part-187/fixture-migration.json). Original4525 pending188;retain1864485/4486 and latest full success1844455/4455. Native platform/metadata/HAMT/grammar/scheduler/resources/upstream/performance scope and remaining APIs continue.

Part186:source-built original **4485/4486** and **5051/full same-head CI** at4c0b4a5. 1 native discrepancies retained;repair before promotion.31 process suspension scenarios and31 inherited independent raw-output fixtures executed. [Proof](validation/part-186/differential.json),[guard](validation/part-186/strict-guard.json). Preserve3582 SDK/3576 comparable modules,778 expressions,132 diagnoses and six exclusions. Registry404;erlang254/351,97 missing,72.4% API presence,not semantic completion. Native scheduler/preemption/signal races/cycles/resources/upstream/performance and remaining APIs continue.

Part185:erlang suspend_process1/2 and resume_process1 use real execution pauses,per-owner counts/unless/asynchronous acknowledgments and owner cleanup. Paused receives/PID BIF timers freeze,name timers continue,CPC/kill controls remain admitted. **5051/full main and draft**,31 new source scenarios;all3551 prior contracts preserved. Registry404;erlang254/351,97 missing,**72.4% API presence**,not semantic completion. [Tests](validation/part-185/tests.json),[audit](validation/part-185/reference-audit.json). Original4486 pending186;latest1844455/4455. Native scheduler/preemption/signal races/cycles/resources/upstream suites/performance and remaining APIs continue.

Part184:source-built OTP-29.1.1 original **4455/4455** and **5016/full same-head CI** at13374f5. All31 standard-stream fixtures match independent raw stdout/stderr byte expectations in both runtimes. [Proof](validation/part-184/differential.json),[guard](validation/part-184/strict-guard.json). Preserve3551 SDK/3545 comparable modules,778 expressions,132 diagnoses and six exclusions. Registry401;erlang251/351,100 missing,71.5% API presence,not semantic completion. Native errno/TTY/resources/schedulers/upstream suites/performance and remaining APIs continue.

Part183:erlang display_string1/2 writes real raw standard streams;one-argument form defaults to stderr,flat scalar lists become UTF8,binaries remain byte-exact. **5016/full main and draft**,31 independent output/error fixtures added;all3520 inherited corpus contracts preserved. Registry401;erlang251/351,100 missing,**71.5% API presence**,not semantic completion. [Tests](validation/part-183/tests.json),[audit](validation/part-183/reference-audit.json). Original4455/output-byte comparisons pending184;latest1824424/4424. Native errno cause/resource/scheduler/partialwrite/real Unix TTY coverage and remaining APIs continue. No terminal input injected by checks.

Part182:source-built OTP-29.1.1 original **4424/4424** and **4981/full same-head CI** at2513736. All selected comparisons match;continue remaining APIs. [Proof](validation/part-182/differential.json),[guard](validation/part-182/strict-guard.json). All3520 SDK fixture contracts/3514 comparable modules,778 expressions,132 diagnoses and six exclusions preserved. Twenty purge scenarios match original outcomes;host concurrent admission/current retention/owner type checks remain distinct from native hot-load tests. Registry399;erlang249/351,102 missing,70.9% API presence,not semantic completion. Native resources/global system actor/capacity/autoload/full suites/performance and remaining APIs continue. Close0026 after181;open0027.

Part181:real owned erlang:purge_module/1 prepares the old image,blocks new old-function entries,checks actual executing images,kills old users and waits for monitor/managed unwind before retiring only the old slot. **4981/full main and draft**,twenty native fixtures added;all3500 prior corpus contracts preserved. Registry399;erlang249/351,102 missing,**70.9% API presence**,not semantic completion. [Tests](validation/part-181/final-owner-guard/tests.json),[audit](validation/part-181/reference-audit.json). Original4424 pending182;latest1804404/4404. Current matching functions remain usable;incompatible retired functions fail. Native system actor/process capacity/autoload/physical resources/full suites/performance and remaining APIs continue.

Part180:source-built OTP-29.1.1 original **4404/4404** and **4959/full same-head CI** at0198891. All selected comparisons match;continue remaining APIs. [Proof](validation/part-180/differential.json),[guard](validation/part-180/strict-guard.json). All3500 SDK fixture contracts/3494 comparable modules,778 expressions,132 diagnoses and six exclusions preserved. Nine anonymous static-tail cases added in179 match native outcomes;host matching-code reload checks remain distinct. Registry398;erlang248/351,103 missing,70.7% API presence,not semantic completion. Actual purge admission/old-user termination and remaining APIs continue;native resources/full suites/performance unfinished.

Part179: saved compiler-owned functions select the current image when canonical code matches; different code keeps the captured old image. Anonymous static tail transfers unwind their frames. **4959/full local**, all3491 previous corpus contracts preserved; nine native fixtures added,4404 comparisons pending180. Registry398;erlang248/351,103 missing,**70.7% API presence**,not semantic completion. [Tests](validation/part-179/tests.json),[audit](validation/part-179/reference-audit.json). Latest original1784395/4395 remains historical. Atomic purge admission,old-code-user termination,retired fun errors,native resources/full suites/performance remain unfinished.

Part178: complete source-built pinned original **4395/4395** atfd06982 and **4949/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-178/differential.json),[guard](validation/part-178/strict-guard.json).3485 comparable modules/132 diagnoses/778 expressions/six exclusions preserve177 contracts,including25 new process-code scenarios and inherited static tail cases. Historical158 incomplete4004/4167 remains immutable. Registry398;erlang248/351,103 missing,**70.7% API presence**,not semantic completion. Twenty-five owned process-code scenarios match original OTP;purge admission/matching-code fun retarget remains under development. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part177:erlang check_process_code2/3 uses actual old-image execution and queued process requests;**4949/full local**,all3466 prior corpus contracts preserved,25 native scenarios added. Registry398;erlang248/351,103missing,**70.7% API presence**,not semantic completion. [Audit](validation/part-177/reference-audit.json),[tests](validation/part-177/tests.json). Original4395 pending178;latest1764370/4370. Self/async/other/dead/option-cause/request-identity boundaries and owned callback lifetimes guarded;native priority/dirty/physical stacks/resources/distribution/full suites,purge admission and matching-code fun retarget remain unfinished. Continue whole erlang goal.

Part176: complete source-built pinned original **4370/4370** at49b9792 and **4921/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-176/differential.json),[guard](validation/part-176/strict-guard.json).3460 comparable modules/132 diagnoses/778 expressions/six exclusions preserve175 contracts,includingeight inherited static runtime callback scenarios. Historical158 incomplete4004/4167 remains immutable. Registry396;erlang246/351,105 missing,**70.1% API presence**,not semantic completion. All inherited callback/source/fun scenarios match original OTP;CPC/purge admission remains under development. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part175:real current/old source images and continuation references pass **4921/full main**;draft combined semantic/package proof preserves37 DLL hashes after copying missing unchanged README. Registry396;erlang246/351,105missing,**70.1% API presence**,not semantic completion. All3466 native fixture contracts unchanged;4370 original pending176,latest1744370/4370. [Audit](validation/part-175/reference-audit.json),[tests](validation/part-175/tests.json). Generic fun calls retain caller continuation;heap-held values do not count. CPC/purge APIs,atomic purge admission/matching-code fun retarget/physical resources/full suites/performance remain unfinished. Close0025/open0026;continue whole erlang goal.

Part174: complete source-built pinned original **4370/4370** at5173169 and **4920/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-174/differential.json),[guard](validation/part-174/strict-guard.json).3460 comparable modules/132 diagnoses/778 expressions/six exclusions preserve173 contracts,includingeight new static runtime callback scenarios. Historical158 incomplete4004/4167 remains immutable. Registry396;erlang246/351,105 missing,**70.1% API presence**,not semantic completion. Eight runtime callback scenarios match original OTP;general dynamic fun continuation/code-version admission remains under audit. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part173:static callback tail frame release pass **4920/full local**;all3458 old corpus contracts unchanged,eight original-comparison fixtures added. Registry396;erlang246/351,105missing,**70.1% API presence**,not semantic completion. Original4370 pending174;latest complete1724362/4362. [Audit](validation/part-173/reference-audit.json),[tests](validation/part-173/tests.json). Generic fun continuation/code-image ownership audit,code-version admission/CPC/purge,NIF/full suites/resources/performance unfinished.

Part172: complete source-built pinned original **4362/4362** at8065c3b and **4911/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-172/differential.json),[guard](validation/part-172/strict-guard.json).3452 comparable modules/132 diagnoses/778 expressions/six exclusions preserve171 contracts,includingten new owned source-tail scenarios. Historical158 incomplete4004/4167 remains immutable. Registry396;erlang246/351,105 missing,**70.1% API presence**,not semantic completion. Ten owned source-tail scenarios match original OTP;general dynamic fun continuation/code-version admission remains under audit. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part171:owned source tail dispatch/current-entry owners pass **4911/full local**;all3448 old corpus contracts unchanged,ten original-comparison fixtures added. Registry396;erlang246/351,105missing,**70.1% API presence**,not semantic completion. Original4362 pending172;latest complete1704352/4352. [Audit](validation/part-171/reference-audit.json),[tests](validation/part-171/tests.json). General local/anonymous and non-source tail handling,code-version admission/CPC/purge,NIF/full suites/resources/performance unfinished.

Part170: complete source-built pinned original **4352/4352** at23bbf47 and **4900/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-170/differential.json),[guard](validation/part-170/strict-guard.json).3442 comparable modules/132 diagnoses/778 expressions/six exclusions preserve169 contracts,including13 current-to-old module deletion scenarios. Historical158 incomplete4004/4167 remains immutable. Registry396;erlang246/351,105 missing,**70.1% API presence**,not semantic completion. Selected get_module_info1/2 and predefined module_info0/1 queries now match original OTP;backend digests/compiler inventories remain distinct. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part169:actual delete_module1/check_old_code1 and explicit current-export dispatch pass **4900/full local**;all3435 previous corpus contracts unchanged,13 native fixtures added. Registry396;erlang246/351,105missing,**70.1% API presence**,not semantic completion. Original4352 pending170;latest complete1684339/4339. [Audit](validation/part-169/reference-audit.json),[tests](validation/part-169/tests.json). Old callbacks/IR/descriptors remain owned;purge/process-code admission/tail-frame tracking/hot code/native images/NIF/full suites/resources/performance unfinished.

Part168: complete source-built pinned original **4339/4339** atf9703a3 and **4884/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-168/differential.json),[guard](validation/part-168/strict-guard.json).3429 comparable modules/132 diagnoses/778 expressions/six exclusions preserve167 contracts,including28 literal-attribute scenarios. Historical158 incomplete4004/4167 remains immutable. Registry394;erlang244/351,107 missing,**69.5% API presence**,not semantic completion. Selected get_module_info1/2 and predefined module_info0/1 queries now match original OTP;backend digests/compiler inventories remain distinct. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part167:improper literal attributes now preserve Cons/Nil directly, matching v3_core.erl is_list/1. **4884/4884/full local**,four new boundary fixtures;registry394,erlang244/351,107missing,69.5% API presence,not semantic completion. Prior native166 **4334/4335** discrepancy retained;corrected one wrong published assertion with explicit ledger,3430 other prior fixtures unchanged. Next exact-head4339 original/4884 CI verification and actual current/old module lifecycle. [Tests](validation/part-167/tests.json),[correction](validation/part-167/corrected-fixture-contract.json).

Part166: complete source-built pinned original **4334/4335** atb153f03 and **4880/full same-head CI**. 1 native discrepancies retained;repair next. [Proof](validation/part-166/differential.json),[guard](validation/part-166/strict-guard.json).3425 comparable modules/132 diagnoses/778 expressions/six exclusions preserve165 contracts,including24 new literal-attribute scenarios. Historical158 incomplete4004/4167 remains immutable. Registry394;erlang244/351,107 missing,**69.5% API presence**,not semantic completion. Selected get_module_info1/2 and predefined module_info0/1 queries now match original OTP;backend digests/compiler inventories remain distinct. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part165:literal source attributes/explicit vsn and metadata-free code/fun fingerprints pass **4880/full local**. All3407 prior sources/expectations/generated hashes/contexts/policies,132 diagnoses,778 expressions/six exclusions preserved. [Audit](validation/part-165/reference-audit.json),[tests](validation/part-165/tests.json). Registry394;erlang244/351,107missing,**69.5% API presence**,not completion. Actual C# declaration/assembly metadata,not copied BEAM checksums/compiler settings. Original4335/same-head CI4880 pending166;latest complete native1644311/4311. Remote fun attributes/docs/type/compiler directives/optimizer inventories/hot code/NIF/distribution/resources/full suites/performance unfinished;continue whole goal.

Part164: complete source-built pinned original **4311/4311** atf64c951 and **4855/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-164/differential.json),[guard](validation/part-164/strict-guard.json).3401 comparable modules/132 diagnoses/778 expressions/six exclusions preserve163 contracts,including18 new owned module-information scenarios. Historical158 incomplete4004/4167 remains immutable. Registry394;erlang244/351,107 missing,**69.5% API presence**,not semantic completion. Selected get_module_info1/2 and predefined module_info0/1 queries now match original OTP;backend digests/compiler inventories remain distinct. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part163:owned current-code get_module_info1/2 and generated module_info0/1 pass **4855/full local**. All3389 prior sources/expectations/generated hashes/contexts/policies,132 diagnoses,778 expressions/six exclusions preserved. [Audit](validation/part-163/reference-audit.json),[tests](validation/part-163/tests.json). Registry394;erlang244/351,107missing,**69.5% API presence**,not completion. Actual C# declaration/assembly metadata,not copied BEAM checksums/compiler settings. Original4311/same-head CI4855 pending164;latest complete native1624293/4293. User attributes/optimizer inventories/hot code/NIF/distribution/resources/full suites/performance unfinished;continue whole goal.

Part162: complete source-built pinned original **4293/4293** at4dcc7c6 and **4834/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-162/differential.json),[guard](validation/part-162/strict-guard.json).3383 comparable modules/132 diagnoses/778 expressions/six exclusions preserve161 contracts,including five whitespace-only form-boundary migrations. Historical158 incomplete4004/4167 remains immutable. Registry392;erlang242/351,109 missing,**68.9% API presence**,not semantic completion. Selected bootstrap pre_loaded, named BIF/private references and scanner form boundaries now match original OTP. Native schedulers/magic resources/distribution/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part161:actual pre_loaded boot catalogue,named BIF/private references and form-boundary repair pass **4834/full local**. Five whitespace-only source migrations retain all3354 generated hashes/expectations/policies;other3349 sources/127 old diagnostic commands/778 expressions/six exclusions unchanged. [Ledger](validation/part-161/fixture-migration.json),[audit](validation/part-161/reference-audit.json). Registry392;erlang242/351,109 missing,**68.9% API presence**,not completion. Original4293/same-head CI pending162;historical1604119/4253 incomplete,all18 loading/28 process-table/71 reference-text cases actually matched;latest complete1564121. Native images/resources/hot code/remote captures/import attributes/full suites/performance unfinished;continue whole goal.

Part160:incomplete source-built original **4119/4253 passed,134 unexecuted**,zero executed mismatches. All18 loading queries,28 process-table and71 reference-text modules match;erl_parse rejects the next private-function fixture because done.loaded lacks form-separating whitespace. **4792/full same-head CI** at1dc4be2. [Raw failure](validation/part-160/differential.json),[guard](validation/part-160/strict-guard.json). Sources/expectations/hashes/policies/exclusions unchanged;latest complete success1564121. Registry391;erlang241/351,110 missing,68.7% API presence. Retain exact rejected source,repair scanner form-dot boundaries and ledger whitespace-only fixture corrections. Isolated pre_loaded/named-reference draft passes4823/full but remains unpromoted;whole goal continues.

Part159:local reference text/allocator carry and exact183-signature function resolution pass **4792/full local**. Three loaded sources qualified with [migration ledger](validation/part-159/fixture-migration.json);other3272 modules/all old expectations/policies,120 diagnoses,778 expressions/six exclusions preserved. Registry391;erlang241/351,110 missing,**68.7% API presence**,not completion. New-head native4253/same-head CI pending160;latest complete native1564121;158 incomplete4004/4167 retained. pre_loaded0 is auto-imported. Connected channels/magic resources/schedulers/import attributes/full suites/performance unfinished;continue whole erlang goal.

Part158:incomplete source-built original **4004/4167 passed,163 unexecuted**,zero executed mismatches;erl_lint rejects unqualified loaded/0 before comparison. **4702/full same-head CI** at5128765. [Raw failure](validation/part-158/differential.json),[guard](validation/part-158/strict-guard.json). Sources/expectations/hashes/policies/exclusions unchanged;latest complete success1564121. Registry389;erlang239/351,112 missing,68.1% API presence. Preserve rejected source as a permanent diagnosis,audit compiler auto-import/call resolution and explicitly qualify only invalid fixtures before rerun. Reference text remains isolated;whole goal continues.

Part157:actual current-code module_loaded1/loaded0 and bounded process-table processes_iterator0/processes_next1 pass **4702/full local**.46 new modules/two lifecycle host checks/four exports. [Audit](validation/part-157/reference-audit.json),[preservation](validation/part-157/corpus-audit.json). All3229 old modules/120 diagnoses/778 expressions/six exclusions preserved. Registry389;erlang239/351,112 missing,**68.1% API presence**,not module completion. Original4167/same-head CI pending158;latest complete native1564121. Hot-code/on_load/NIF/lock-free/reduction/distribution/resource prerequisites unfinished;continue whole goal.

Part156: complete pinned original **4121/4121** at b4db97e and **4650/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-156/differential.json), [strict guard](validation/part-156/strict-guard.json). All3223 sources/independent expectations/hashes/contexts/policies,120 diagnoses,778 expression inputs and six exclusions preserved from155, including320 packet sources. Registry385; erlang235/351,116 missing,67.0% API presence,not whole-module completion. Module-loading queries pass4671 local tests in an isolated draft; their native proof remains pending. Continue the whole goal.

Part155: source-directed decode_packet/3 implements all packet selectors, options, framing, HTTP and TLS; **4650/full local**, 320 new independent module contracts. [Audit](validation/part-155/reference-audit.json), [preservation](validation/part-155/corpus-audit.json). All2909 old modules/120 diagnoses/778 expressions/six exclusions preserved. Registry385; erlang235/351, 116 missing, **67.0% API presence**. New-head4121 original and4650 CI pending156; latest fully successful original1543801. Managed binary copies/resources/error-stack metadata remain separate scope. Continue the whole goal.

Part154: complete pinned original **3801/3801** at1122925 and **4329/full same-head CI**. all selected comparisons match;continue remaining APIs. [Proof](validation/part-154/differential.json),[strict guard](validation/part-154/strict-guard.json). All2903 sources/independent expectations/hashes/contexts/policies,120 diagnoses,778 expression inputs and six exclusions preserved from153. Registry384;erlang234/351,117 missing,66.7% API presence,not whole-module completion. Packet289/289 isolated draft remains unverified natively;whole goal continues.

Part153: record expansion/compiled tuple-test repair plus four identifier-text exports and character literals pass **4329/full local**. [Three-premise correction ledger](validation/part-153/record-premise-corrections.json),[audit](validation/part-153/reference-audit.json). All2782 prior sources unchanged;three explicit expected corrections/two generated hash changes only;all103 old diagnoses/six exclusions/policies preserved.77 identifier/41 character/9 record modules,17 scanner diagnoses,two host checks. Registry384;erlang234/351,117 missing,66.7% API presence. Original3801/same-head4329 CI pending154;152 failures retained. Packet75/75 is isolated;whole goal continues.

Part152: complete original **3654/3657**,three record-predicate discrepancies preserved;**4179/full same-head CI** at5dae8cf. All16 startup/18 inspection/28 clock modules and103 diagnoses match;literal guard repair verified. [Failures](validation/part-152/mismatches.json),[guard](validation/part-152/strict-guard.json). Signed negative literals evaluate arguments natively;one compiled big-size apply differs from weak-BIF premise. Sources/expectations/hashes/policies/exclusions unchanged. Registry380;erlang230/351,121 missing;latest complete success140. Audit then repair;whole goal continues.

Part151: repair is_record3 guard literal-name/literal-integer-or-atom-size restrictions;**4179/full local**. Preserve exact150 aborted source as a rejection diagnosis;new valid literal guard slot,four positives minusone invalid andsix diagnoses. [Migration](validation/part-151/guard-fixture-migration.json),[audit](validation/part-151/reference-audit.json). All other2778 modules/all97 old diagnoses/six exclusions/policies unchanged. Registry380;erlang230/351,121 missing. Original3657/same-head4179 CI pending152;whole goal continues.

Part150: incomplete original run **3517/3648 passed,131 unexecuted**,zero executed mismatches;native erl_lint rejects dynamic is_record3 guard before comparison. **4170/full same-head CI** at f42149b. [Raw failure](validation/part-150/differential.json),[guard](validation/part-150/strict-guard.json). No source/expected/policy/exclusion changes;latest complete native success140. Registry380;erlang230/351,121 missing,65.5% API presence. Repair guard restrictions and rerun full pinned corpus;whole goal continues.

Part149: actual startup link/monitor/handshake,atomic named registration,four-key process_info2,owned clock monitors and tuple-record predicates pass **4170/full local**.107 new modules/four compiler diagnoses/three exports;[audit](validation/part-149/reference-audit.json). All2672 prior source/expectation/generated hashes and six exclusions preserved. Explicit36 historical trapped caller contexts and exactly two no_ssa_opt spawn policies retain failed default-native evidence. Original3648/same-head CI pending150. Registry380;erlang230/351,121 missing,65.5% API presence;native records/time-warp/full inspection/ports/distribution/resources unfinished. Continue whole goal.

Part148: **90 native diagnoses completed**,zero compatibility-corpus coverage;**4054/full same-head CI** at3d9a71c. SSA optimization is isolated as the cause of missing continuation in two unchanged spawn cases;no_copt/no_postopt do not repair it. [Matrix](validation/part-148/outcome-matrix.json),[guard](validation/part-148/strict-guard.json). Four startup errors retain trap-context dependency. Preserve failed144 and default compiler outcomes. Next actual startup/state/clock/record implementation with explicit context and narrowly documented no_ssa_opt policy. Registry377;erlang227/351,124 missing;whole goal continues.

Part147: isolated native compiler-pass tooling passes **4054/full local**,one new permanent protocol check;90 native diagnoses planned148,zero local native coverage. Pinned spawn/1 type inference conflicts with accepted fun/MFA wrappers;actual pass remains unverified. [Audit](validation/part-147/reference-audit.json). Runtime/MFAs/fixtures/expectations unchanged;144 failures retained. Registry377;erlang227/351,124 missing,64.7% API presence. Continue startup repair and whole goal.

Part146: **36/36 native diagnostic evaluations completed**,zero compatibility-corpus coverage;**4053/full same-head CI** at11f8b8e. Four startup discrepancies depend on trap_exit. Two spawn sources lose monitor/receive/done continuation in default native compiled run/0;disabled optimizations retain it and return done. [Matrix](validation/part-146/outcome-matrix.json),[guard](validation/part-146/strict-guard.json). No source/expectation/exclusion rewrite or resolved144 claim. Registry377;erlang227/351,124 missing,64.7% API presence. Continue startup repair and isolate compiler pass;whole goal remains unfinished.

Part145: native investigation tooling passes **4053/full local**;36 targeted native compiler/context evaluations planned146,zero run locally. Source confirms adapters defer startup links incorrectly;repair follows actual native diagnosis. All production sources,2672 fixture sources/expectations/hashes and six exclusions unchanged. [Audit](validation/part-145/reference-audit.json). Failed1443531/3537 retained;latest successful original140. Registry377;erlang227/351,124 missing,64.7% API presence. Whole goal continues.

Part144: complete **3531/3537 original**,six discrepancies preserved;**4052/full same-head CI** at e8a5cc9. All79 new exit/link contracts match. Fresh caller repairs two trap_exit premises;four historical linked-init outcomes change and two PID results persist. All sources,independent expectations,generated hashes and six exclusions preserved. [Failures](validation/part-144/mismatches.json),[proof](validation/part-144/remote-checkpoint.json). Registry377;erlang227/351,124 missing,64.7% API presence. Latest fully successful original140. Audit native linked-init signals and compiled PID outcomes before promoting clock/record drafts. Whole goal continues.

Part143: four priority link/exit exports,pending alias/link signal resolution and safe terminal cleanup;repair monitored-spawn group-leader inheritance. **4052/full local**,79 new modules and128-child host check. [Audit](validation/part-143/reference-audit.json). Registry377;erlang227/351,124 missing,64.7% API presence. All141 fixtures/six exclusions preserved;original3537 and same-head4052 CI pending144. Whole erlang goal continues;real ports/distribution/native scheduler/full metadata and resources unfinished.

Part142: complete **3454/3458 original**,four new spawn discrepancies preserved;**3968/full same-head CI** at59992ef. [Failures](validation/part-142/mismatches.json), [proof](validation/part-142/remote-checkpoint.json). All old successful native results/all141 sources/six exclusions preserved. Registry373;erlang223/351,128 missing,63.5% API presence. Latest successful original140. Next: align native fresh-child execution context without altering fixtures,resolve PID outcomes,then priority signals/links and monitored-spawn inheritance. Whole goal continues.

Part141: group-leader inheritance, five group_leader/MFA spawn exports, spawn/1 child-apply repair and monitored io:format/2 protocol; **3968/full local**,60 new module contracts and128-writer host check. [Audit](validation/part-141/reference-audit.json). Registry373; erlang223/351,128 missing,63.5% API presence. All139 fixtures and six exclusions preserved. Original3458 and same-head3968 CI pending142; latest verified140. Whole erlang goal active; connected distribution/native scheduler/full I/O prerequisites unfinished.

All current compatibility entries are implemented or partially compatible, not globally verified compatible. Source-driven regressions do not replace running OTP 29.1.1. Latest selected proof:1380/1380 at4992a98 (778 expressions,523 compiled modules,79 diagnostics), with1574 same-head CI tests; see [part098](validation/part-098/remote-checkpoint.json). The historical exact-version 183-case corpus passed at 3990623 in run 37999487360, including float/UTF/numeric-coercion and literal-string additions; only these value/error cases are verified, not full modules, stacks, signal effects or reference suites. Earlier failed-run diagnoses remain in CHANGELOG.

Anonymous fun matching now shadows captured values separately per clause, preserving a capture in fallback clauses whose own head does not bind that name. Three regressions cover tuple-head mismatch, guard failure and bit-pattern fallback. Named/external fun forms and full scope combinations remain pending.



- Scheduling uses CLR continuation execution with expression reductions; arbitrary C# callbacks cannot be preempted. Local process completion denotes logical death and signal publication, not guaranteed completion of arbitrary host callback cleanup. No BEAM scheduler fairness guarantee.
- Local links/monitors and mailbox delivery are lock-serialized. Atomic local spawn_monitor and iterative link-failure propagation are implemented. Full Erlang signal queues, priority messages, aliases, spawn_request, distributed identity and monitor option variants are pending.
- PID/port/reference identities currently have 64-bit IDs. Wider reference ID vectors, exact distributed identity ordering and external fun terms are incomplete.
- Signed zeros are distinct under exact equality for OTP 29.1.1. Numeric equality ignores the sign. Map key ordering puts integer types before float types. Atom ordering compares Unicode codepoints. Float nonfinite values are rejected. Atom interning/table limits and complete Unicode literal escapes are pending.
- Map source construction/updates/patterns now use exact keys and support the existing guard-expression subset. Map comprehensions, the full map BIF/guard set and complete oracle verification are pending. Bitstring source construction/patterns support integer/binary/float/UTF segments, sizes/units and byte order, signed integer extraction, guard sizes and prior-segment bindings. Float construction uses 16/32/64 bits with direct binary16 rounding and explicit nearest-even integer conversion; narrowing may retain infinity bits, but decoding infinity/NaN fails matching. Zero-width float patterns bind positive zero. Local tests pass; 183 selected cases, including 31 UTF and 23 new literal-string cases, match OTP at 3990623. UTF8/16/32 use strict scalar decoding, including noncharacters and unaligned offsets. UTF literal strings expand into scalar segments. All explicit UTF sizes/units, including literal undefined, are rejected. The earlier explicit-size abort and correction remain historical evidence in part 016; the corrected current run is complete. Integer/float literal-string construction now supports per-codepoint sizes/units/endian, one size evaluation and empty-string validation. Non-default non-UTF strings remain forbidden in patterns by the pinned lint contract. Static all sizes are rejected for non-binary segments during parsing; a variable evaluating to all remains a runtime size check. Complete scopes, error ordering/resource boundaries and compiled-reference comparison remain pending. Remaining source gaps include records, includes/macros, comprehensions, try/catch/after, if, types/specs, external/named fun references and dynamic module calls. Unsupported syntax produces diagnostics rather than a language rewrite approximation.
- Local module tail calls through sequences/case/receive are trampolined. Anonymous/named fun tail calls and all other compiled continuation forms are not complete; deeply nested non-tail calls may exhaust CLR stack.
- Error stack frames are currently placeholder empty lists. Complete Erlang exception shapes, stacktrace capture, badfun/badarity/undef argument details and complete arithmetic error contracts need differential hardening. Shared nearest-even integer conversion and per-operand finite checks now cover mixed arithmetic and integer /; six selected arithmetic oracle cases match in the complete 183-case corpus; broader inputs/errors remain unverified.
- Map guards is_map/map_size/map_get/is_map_key use the existing exact map-key contract. Nonmaps raise {badmap,Value}; map_get missing keys raise {badkey,Key}; guard errors reject the guard alternative. Arbitrary maps module calls remain illegal in guards. Differential outcomes now compare value or exception class/reason; stack traces, signals and side effects require separate future comparison tracks.
- is_bitstring accepts binary and non-octet BitString values; bit_size returns bits and byte_size rounds up for partial bytes. Non-bitstrings produce badarg for size BIFs. Current representations have int-sized bit lengths; larger BEAM/resource boundaries and error stack fidelity are unverified.
- gen_server has call/cast/info/stop, C# callbacks and reference-monitor replies. It lacks alias-based late-reply suppression, full OTP exception context, callback module adaptation, sys protocol, hibernation, continue/timeouts, name variants and hot upgrades. Late replies after timed-out calls can remain in the caller mailbox.
- Supervisor strategies/policies/intensity and ordered shutdown exist. Dynamic child APIs, auto_shutdown/significant children, complete childspec validation, restart retries, complete startup-failure contracts and nested-supervisor shutdown contracts are pending. Child factories must return a live, long-running process.
- io:format supports only ~s, ~p, ~w, ~n and ~~; many formatting directives, devices and Unicode/iodata variants are absent. BIF/MFA error compatibility is only partial.
- Hybrid blocks are recognized at selected C# boundaries and require async context with `erlangProcess`. There is no implicit C# local conversion. Each block has a fresh Erlang binding scope. Synchronous receive, arbitrary inline tuples/calls/operators in C#, cross-block bindings, diagnostics column maps and debugger stepping are pending.
- Compiler output currently constructs an AST executed by the C# evaluator. Direct operation lowering/optimized IR is pending. Module AST collections are trusted compiler structures and not a hostile-input sandbox.
- The package supports the pinned SDK/net10.0 prototype. Other SDKs/TFMs, design-time IDE builds and publish/trimming/AOT compatibility are unverified.
- Inventory exports are extracted before conditional/macro expansion. Source modules, explicit exports and BIF declarations do not establish complete effective public-contract coverage.
- Distributed Erlang, ETS/DETS, code loading/upgrades, NIF/ports execution, applications, gen_statem/gen_event and the remaining OTP libraries are Not started.

Reference comparison currently evaluates expressions with erl_eval. Compiled-reference-module/optimizer equivalence and full reference compiler suites are separate unfinished tracks.

Parts 018–019 are code-organization refactors. Domain constants and compiler diagnostic builders preserve the existing literal values, diagnostic texts/codes/offsets and grammar. Local validation was rerun; reference results remain attributed to their historical tested heads. These parts add no semantic compatibility claim or new boundary.

Part 020 changes source-file organization only: namespace-level types now each have a matching file, with nested declarations preserved. Source/C# debug file and line locations change; no grammar, term, API or runtime algorithm changes. The local full pipeline and type-token/layout audit pass; no new reference-suite coverage is claimed.

Part 021 is a style/constant/journal refactor, with a development-only SDK Roslyn checker. Existing code tokens and literal values match after domain-constant/message-builder expansion; no grammar/runtime/MFA change is claimed. C# source/debug line positions and journal locations change. Historical reference results remain attached to their actual head. The style utility's SDK dependency is not a production BEAM or runtime dependency.

Part 022 only names existing ETF numbers. Nesting depth 256, default byte limit and support for at most two reference ID words remain implementation bounds. Numeric tags, field/header sizes, thresholds and compression buffer behavior are unchanged; no ETF compatibility expansion or new pinned-oracle case is claimed.

## Part 023 — Readable switch expressions

Switch expressions place the opening brace, each arm and the closing brace on separate lines. The SDK-backed style tool enforces this throughout ordinary C# sources and full validation checks it. TermOrder.Rank, Parser.Precedence and Semantics.Variables preserve their exact non-trivia token streams, values and pattern order. The independent guarded-pattern probe rejects compact layout, repairs it and passes a second check while preserving a string containing switch-like text. No new Erlang feature, MFA, compatibility promotion or dependency. Current 299-test/full integration reports are saved in validation/part-023; the 160-case oracle evidence remains historical at 666c4b5 and was not rerun.

## Part 027 — List sequence resource boundary

lists:nth/2 and nthtail/2 use iterative Cons traversal, including valid improper prefixes. Sequence values/count calculations use BigInteger. Materialized seq/2,3 results exceeding int.MaxValue elements raise system_limit before allocation; this is an implementation bound, not claimed OTP behavior. Smaller allocations may still exhaust host resources. Selected function_clause/badarg/value contracts have permanent local tests and 36 pending oracle cases; complete stacks/resources/applicable inputs remain unverified. Readiness declaration percentages do not quantify semantic compatibility.

## Part 028 — Evidence update

All 36 selected lists values/class/reason cases now match within a complete **219/219** exact-version result at 6f34dc9. This supersedes part-027's pending reference status only for those selected cases. The sequence count resource bound and full stack/input/side-effect limitations remain. No new runtime/compiler semantics or dependency in this part.

## Part 029 — Key-search architecture scope

Key-search position and small-key fast path model the pinned 64-bit reference small integer representation. 32-bit BEAM position boundaries are not verified. The small integer/float element rounded-double comparison is deliberately local to key search; global numeric equality, exact member/2 and map keys are unchanged. Reverse/1 now distinguishes short-clause function_clause from reverse/2 badarg on longer improper lists. Value/class/reason regressions pass locally; full stacks/resources/reductions remain unverified.

## Part 030 — Selected reference confirmation

All 30 added key-search/reverse cases match within complete 249/249 at 85bd19f, superseding the part-029 pending status for those cases only. Position architecture scope, full stacks/resources/reductions and compiled-reference-module boundary remain. No runtime/compiler semantics changed in this evidence checkpoint.

## Part 031 — Module comparison scope

The new reference track compiles full fixture modules with normal OTP compiler optimizations and invokes run/0; .NET emits actual generated C# Release assemblies and runs their AST/evaluator definitions. This checks selected compiled-module behavior and generation integrity, not full optimizer equivalence/native lowering or stack/debug mappings. Existing expression reports remain erl_eval evidence. No language/runtime semantics changed; compile/load infrastructure failure is kept separate from a compared value/class/reason mismatch. The initial host SDK memory failure and bounded-process retry are preserved separately.

## Part 032 — Compiled-module evidence boundary

Selected 14 generated C# Release assembly/default-optimized BEAM module outcomes now match within complete 263/263 at b13b56a, alongside the preserved 249 expression cases. Earlier pending module status is superseded only for those fixtures. Generated code remains AST/evaluator-based; full optimizer equivalence/native lowering/compile warning diagnostics/stack mappings/signals/resources/reference suites remain unverified. No semantic implementation changed in this evidence-only checkpoint.

## Part 033 — Atom representation boundary

A fixed ten-name immutable object cache reduces repeated factory allocations; arbitrary names remain uncached. Name-based equality/hash/order is preserved, including freshly constructed Atom objects. CLR reference identity is not a semantic test for Erlang equality. This is not the BEAM global atom table and does not establish its atom-count/length/resource limits. Existing oracle evidence remains at b13b56a; no new oracle/performance measurement here. Independent test fixture atoms are intentionally kept with their source examples; only protocol wrappers share domain constants.

## Part 034 — Compiled bit-order and closure regressions

Added twelve original compiled module fixtures: observable value/size order via process dictionary; value-before-size and size-before-next-value exceptions; delayed invalid integer/size/UTF validation versus a later throw; size binding used by the next segment; captured binary size; failed bit-clause binding rollback; guarded shadow/fallback; captured map key/shadowed value; nested body-local capture. The permanent fixture loop now executes **26** actual generated C# Release assemblies, with independent expected values. Local test count is **381**; no grammar/runtime MFA change.

The existing **249 expression cases** and earlier **14 module sources/expected values/generated hashes** remain unchanged. The current runner plans **275 cases:249 expressions +26 modules**. New optimized-reference outcomes await the exact-version published-head workflow; historical263/263 at b13b56a remains scoped part-032 evidence. No optimizer/full stack/resource/signal compatibility promotion. Readiness compares immutable part-033 tests; **56 MFAs**, delta0, lists13/91 unchanged. Full assignment approximately5%.

## Part 035 — Bit construction scope correction and explicit compiler rejection track

Initial published-head oracle38014541956 atc9ca96a was incomplete:249 expressions and19 modules matched (268 executed), then OTP rejected <<1:(S=4),2:S>> with unbound_var S. The implementation had incorrectly accepted a binding from one construction subexpression in a sibling. The raw aborted report and same-head381-test CI are preserved in validation/part-035; they are not275/275 proof.

Corrected Semantics: check each field value and size against independent copies of the binary's incoming scope, then merge/export bindings after the complete binary. Runtime value/size evaluation order and pattern segment binding rules are unchanged. The invalid fixture moved into a dedicated compiler-diagnostic corpus; its positive replacement reads S after the binary. Added independent-binding/prebound positives. Current runner plans **249 expressions +28 compiled modules +5 compiler-diagnostic cases =282**; diagnostics compare specific normalized OTP reasons and independent ERL006/message expectations.

Full local pipeline passes **389/389** plus warning-free build, generated/hybrid/incremental/negative/clean/local package/CLI/readiness checks. Current28 module fixtures execute generated C# Release assemblies. All249 expression sources and earlier14 reference-confirmed module sources/expectations/generated hashes are preserved. New corrected-head reference comparison remains pending. Registry56/delta0 versus immutable part034; lists13/91 and approximately5% overall unchanged. [Local tests](validation/part-035/tests.json), [initial failure](validation/part-035/initial-aborted-differential.json). Historical complete263/263 remains part032; no full grammar/optimizer/stack/resource promotion.

## Part 036 — Verified construction scope and three comparison tracks

Corrected-head f0ff362 passes **249/249 expressions +28/28 compiled modules +5/5 compiler-diagnostic cases =282/282**, complete/version verified, zero mismatches/aborts. The reference compiler now confirms all five expected unbound_var rejections; C# independently returns ERL006 and the expected variable message. Positive modules confirm selected value/size order, delayed-validation error priority, exported-after-binary bindings, prebound/independent bindings and closure scope outcomes.

Same-head Windows CI passes **389/389** and full build/generated/hybrid/incremental/disabled-preprocessing/clean/local PackageReference/CLI/readiness integration. [Raw proof/provenance](validation/part-036/remote-checkpoint.json), [source/expected/hash audit](validation/part-036/corpus-audit.json). The initial incomplete268-case run at c9ca96a remains preserved in part 035; it is superseded only for the corrected fixture corpus, not reclassified as successful. Registry 56/delta 0, lists 13/91 and approximately 5% overall remain unchanged. Readiness compares immutable part 035 local tests.

All249 expression sources and current 28 module sources/independent outcomes/generated hashes and 5 diagnostic sources/reasons/C# code/messages match. Default-optimized selected compiled outcomes and normalized unbound-variable reasons are verified; full grammar/scoping/optimizer/native lowering/warnings/annotations/debug stacks/resources/signals/reference suites remain unfinished. No runtime MFA or whole-module status promotion.

## Part 037 — Iterative lists:last/1 and lists:split/2

Implemented and registered last/1 and split/2 from audited OTP-29.1.1 contracts. last requires a nonempty proper list; empty/nonlist/improper inputs raise function_clause. split validates integer/nonnegative count and initial list shape; zero/exact prefix succeeds even with an arbitrary improper tail, proper overrun raises badarg, improper overrun raises function_clause. BigInteger counts are not narrowed. Returned elements/suffix retain identity; traversal is iterative with no artificial length cap.

Local full pipeline passes **414/414**, including 58 direct MFA tests, 22 new value/error cases and a 100001-element traversal/identity regression. Build, style, generated/hybrid/incremental/negative/clean/local PackageReference/CLI/readiness pass. [Immutable local evidence](validation/part-037/tests.json), [integration](validation/part-037/integration-results.json), [corpus preservation](validation/part-037/corpus-audit.json). Registry **58 (+2)**; lists **15/91 (16.5% declaration presence)**; erlang40/351, maps2/34, io1/53 unchanged. Readiness delta uses immutable part036 CI tests. Whole modules remain Partially compatible; approximately5% overall is a coarse estimate, not semantic coverage.

The exact-version runner now plans **271 expressions +28 modules +5 diagnostics =304**. All earlier249 expression inputs/dynamic initializer and 28 module source/expected/generated hashes are preserved. New-head reference and CI checks are pending publication/dispatch. Historical282/282 at f0ff362 remains previous-head proof only. No stack/resource/full-suite equivalence or whole-module promotion.

## Part 038 — Verified last/split reference boundaries

Exact implementation head **f20113cbda21d1335ef00a71b1fdffa226679bb4** passes **271/271 expressions +28/28 compiled modules +5/5 diagnostics =304/304**, complete/version verified, no failures/aborts. Reference OTP-29.1.1 was built from pinned source. The22 new last/split cases confirm proper/improper/empty/nonlist/count boundaries, including arbitrary returned tails and badarg versus function_clause. Earlier249 expression sources and28 module sources/independent outcomes/generated hashes plus5 diagnostic reasons/code/messages match preserved fixtures.

Same-head Windows CI passes **414/414** and full build/generated/hybrid/incremental/disabled-preprocessing/clean/local packages/consumer/CLI/readiness. [Raw proof/provenance](validation/part-038/remote-checkpoint.json). Oracle run [38017470376](https://github.com/develmax/Erlang.Net/actions/runs/38017470376); CI run [38017461106](https://github.com/develmax/Erlang.Net/actions/runs/38017461106). Readiness uses immutable part037 local tests:58 MFAs/delta0 in this evidence part; implementation part037 added2, lists15/91 (16.5% declaration presence), erlang40/351, maps2/34, io1/53. No semantic percentage or whole-module promotion; approximately5% overall remains a coarse estimate.

Current type inventory136, zero filename/multiple-type violations; no executable source changes from f20113c. Resource exhaustion, complete error annotations/stacks, full module contracts/reference suites and broader compiler/OTP services remain unfinished. Next audited member/2 exact equality and append/1 improper-tail/error contracts, then grammar and Erlang callback adapters/signals. Historical282/282 at f0ff362 and prior aborted268-case attempt retain their original scope.

## Part 039 — append/1 and existing member/2 boundaries

Implemented lists:append/1 with iterative outer-spine collection and right-to-left concatenation. Empty outer list returns []; singleton returns its arbitrary element unchanged; final elements may be improper lists or any term. Nonlist/improper outer spines raise function_clause before validating earlier prefixes; invalid/improper nonfinal prefixes raise badarg. Reused existing append/2 primitives and retained existing member/2 exact-equality/lazy early-match implementation, with stronger nested/map/bit/signed-zero/improper-tail regressions. Added list-domain names for both existing exports.

Full local pipeline passes **442/442**, including59 direct MFA cases,26 new expression contracts and a100001-element outer traversal/suffix-identity test. Initial441/442 failure was an incorrect new test expectation for signed-zero exact membership, corrected to false using historical pinned proof; runtime code was already correct. [Tests](validation/part-039/tests.json), [integration](validation/part-039/integration-results.json), [initial failure](validation/part-039/initial-tests.json). Registry **59 (+1)**, lists **16/91 (17.6% declaration presence)**, erlang40/351/maps2/34/io1/53 unchanged. Readiness delta uses immutable part038 CI414/58. member/2 is not a newly added MFA; earlier next-step wording is corrected here.

Differential plan **297 expressions +28 modules +5 diagnostics =330**, all271 prior expression sources/dynamic input and28 module sources/independent outcomes/generated hashes preserved. New-head reference/CI comparison awaits publication/dispatch; historical304/304 at f20113c remains scoped prior-head proof. Resource/reduction/stack/full module/reference suites remain incomplete; approximately5% overall is an engineering estimate, not semantic coverage.

## Part 040 — Verified append and exact membership contracts

Exact implementation head **605ef3d3f7d9b6813a0e4d823c7002b2236894a2** passes **297/297 expressions +28/28 compiled modules +5/5 diagnostics =330/330**, complete/version verified, zero failures/aborts. Pinned OTP-29.1.1 source build succeeded. The 26 new append/member cases confirm arbitrary final tails, outer function_clause precedence, malformed-prefix badarg, nested exact equality, signed-zero distinction and early success before improper termination. All 271 prior expression sources and28 module sources/independent outcomes/generated hashes plus5 specific diagnostic reasons/code/messages match preserved fixtures.

Same-head Windows CI passes **442/442** and full build/generated/hybrid/incremental/disabled-preprocessing/clean/local packages/consumer/CLI/readiness. [Raw proof/provenance](validation/part-040/remote-checkpoint.json). Oracle [38018926748](https://github.com/develmax/Erlang.Net/actions/runs/38018926748); CI [38018908551](https://github.com/develmax/Erlang.Net/actions/runs/38018908551). Readiness compares immutable part039 local tests:59 MFAs/delta0 in this evidence part. Implementation part039 added append/1 only; member/2 already existed. lists16/91 (17.6% declaration presence), erlang40/351/maps2/34/io1/53. No whole-module promotion or semantic percentage; approximately5% overall is a coarse estimate.

Type inventory136, no filename/multiple-type violations; no executable source changes from 605ef3d. Initial signed-zero test expectation failure remains preserved in part039; runtime did not require correction. Historical304/304 at f20113c retains its original scope. Resource/reduction/stack/error annotation/full module/reference suites and broad compiler/OTP remain unfinished. Next audited duplicate/2 and flatten/1 nested/improper/resource boundaries, then grammar/Erlang callback adapters/signals.

## Part 041 — Iterative duplicate and nested flatten

Implemented lists:duplicate/2 and flatten/1,2. Duplicate validates nonnegative Integer count, creates cells sharing the immutable value, and has a separately owned local int.MaxValue length cap: larger positive counts raise system_limit before narrowing/allocation. This cap is not verified OTP resource equivalence; huge positive allocations are deliberately excluded from the oracle. Invalid count types/negative counts raise function_clause.

Flatten uses explicit tail-first list-spine frames rather than CLR recursion, preserving leaf order and opaque terms. Only Cons/Nil heads flatten; improper input spines (including nested ones) raise function_clause. flatten/2 accepts Cons/Nil tails, including improper Cons, and preserves their identity without flattening or traversing them. Full local pipeline passes **472/472**, including 62 direct MFA tests,25 new value/error contracts, local-only huge-count guard and 100000-depth/width/shared-value/suffix regressions. [Tests](validation/part-041/tests.json), [integration](validation/part-041/integration-results.json), [corpus audit](validation/part-041/corpus-audit.json).

Registry **62 (+3)**; lists **19/91 (20.9% declaration presence)**, erlang40/351/maps2/34/io1/53 unchanged. Readiness compares immutable part040 CI442/59. New differential plan **322 expressions +28 modules +5 diagnostics =355**, preserving all297 old expression sources/dynamic input,28 compiled sources/independent outcomes/generated hashes and5 diagnostics. Published-head exact oracle and CI are pending; historical330/330 at605ef3d is scoped old-head evidence. Whole modules remain Partially compatible; approximately5% overall is a coarse estimate. Resource/reduction/fairness/stack/annotation/full suite contracts remain unfinished.

## Part 042 — Verified duplicate and nested flatten boundaries

Exact implementation head **3c138b22b7eea5d3a2dbc5b0572aa9b5bf1b6e40** passes **322/322 expressions +28/28 compiled modules +5/5 diagnostics =355/355**, complete/version verified, zero mismatches/aborts. Pinned OTP-29.1.1 source build succeeded. All 25 new duplicate/flatten cases confirm ordinary count/type errors, nested opaque leaves/Unicode/order, malformed spines and the unflattened improper suffix. Huge positive duplicate allocations are excluded; its int.MaxValue local cap and system_limit are local-only policy, not reference resource proof. All 297 prior expressions and28 compiled sources/independent outcomes/generated hashes plus5 specific diagnostic reason/code/message contracts match preserved fixtures.

Same-head Windows CI passes **472/472**, including deep/wide100000 and identity regressions, and full build/generated/hybrid/incremental/disabled-preprocessing/clean/local packages/consumer/CLI/readiness. [Raw proof/provenance](validation/part-042/remote-checkpoint.json). Oracle [38021132556](https://github.com/develmax/Erlang.Net/actions/runs/38021132556); CI [38021119097](https://github.com/develmax/Erlang.Net/actions/runs/38021119097). Readiness compares immutable part041 tests:62 MFAs/delta0 here; part041 added3. lists19/91 (20.9% declaration presence), erlang40/351/maps2/34/io1/53. No semantic percentage or whole-module promotion; approximately5% overall is an engineering estimate.

Current type inventory137, no filename/multiple-type violations; no executable source changes from 3c138b2. Resource allocation/reduction/fairness/stack/annotations/full reference suites and broad compiler/OTP remain unfinished. Historical330/330 at 605ef3d keeps its original scope. Next compiler if guards/alternatives, if_clause and branch binding/export rules through interpreted/compiled/hybrid paths, then Erlang OTP callbacks/signals.

## Part 043 — Domain-owned compiler syntax and runtime export names

Replaced319 string literals across Lexer, Parser, Semantics, Execution and CoreModules with semantic owner constants. Shared operators and erlang function identities live in Erlang.Terms; compiler keywords, punctuation, attributes, bit separators, numeric separators, unsafe-variable markers and receive timeout atoms have separate files. Runtime list/map/io function names and process flags have their own owners; monitor process kind reuses its existing catalog. Division, export arity and bit-specifier slash are distinct constants; minus/attribute/specifier and module/bit-size/unit colon contracts remain separate. No atom cache or runtime behavior change.

Current local **472/472 tests and full integration passed**. [Source-token/value audit](validation/part-043/source-token-audit.json) confirms all319 definition values and all five original implementation token streams after inlining constants. [Corpus audit](validation/part-043/corpus-audit.json) preserves322 expression inputs,28 compiled sources/independent outcomes/generated hashes and5 diagnostic inputs. [Layout](validation/part-043/layout-inventory.json):149 namespace-level types, zero filename/multiple-type violations. Readiness compares immutable part042 CI:62 MFAs/delta0, erlang40/351, lists19/91, maps2/34, io1/53. Declaration presence is not semantic readiness; approximately5% overall remains unchanged.

No new language/API feature or whole-module compatibility promotion. Pinned355/355 at3c138b2 remains historical part042 evidence; no live OTP rerun in this constants-only part. Full grammar, resource/reduction/fairness/stack/reference-suite/OTP scope remains unfinished. Generated C# source templates and independent Erlang fixtures retain their appropriate literal contents. Next substantive feature remains compiler if guards/alternatives, if_clause and branch binding/export rules across interpreted/compiled/hybrid paths.

## Part 044 — If expressions in interpreted, compiled and hybrid execution

Added Expr.If and parser guard clauses using existing ordered zero-pattern Clause selection. Comma conjunctions and semicolon alternatives share the existing guard parser; errors/non-true guards fail, first successful body executes, no selected clause raises error:if_clause. Body errors propagate. Common branch bindings export; partial bindings remain unsafe; guards/bodies do not see other branch bindings. Generated C# emits the AST; module-local tail calls through if use the existing trampoline. Hybrid native if supports nested blocks, quoted atoms and parenthesized guards while preserving ordinary C# if/else via candidate parsing. Valid native expressions still require a terminating dot. Separate error/diagnostic constants follow existing catalogs.

Full local pipeline passes **516/516 tests (+44)**, including24 expression outcomes,8 compiled modules (50000 tail calls),5 compiled diagnostic cases and7 parser/guard/scope/hybrid checks. Generated .erl and native hybrid examples execute if checks in direct and real local-package/installed-CLI paths. Initial509/516 attempt is preserved: seven newly written expression fixtures incorrectly used root sequences; wrap them in case bodies, without changing parser semantics. [Local proof](validation/part-044/tests.json), [integration](validation/part-044/integration-results.json), [pinned source audit](validation/part-044/reference-audit.json).

New oracle plan **346 expressions +36 compiled modules +10 diagnostics =392**, preserving322 previous inputs,28 independent compiled sources/outcomes/generated hashes and5 old diagnostic cases. New unsafe-var fixtures retain construct/line information in normalized reasons; full diagnostic text/annotations/warnings equivalence is not claimed. New-head live oracle and same-head CI are pending;355/355 at3c138b2 remains historical part042 evidence. Source layout151 types/no violations. Registry62/delta0, lists19/91, erlang40/351, maps2/34, io1/53; readiness compares immutable part043 tests. No new MFA or whole-module promotion; overall approximately5% coarse estimate. Full grammar/optimizer/native lowering/resources/reductions/signals/OTP/reference suites remain unfinished.

## Part 045 — Verified if guards, errors and variable export

Exact implementation head **c7e575b54ea04ffa6b58cbe8a4510a20aa5b9797** passes **346/346 expressions +36/36 compiled modules +10/10 diagnostics =392/392**, complete/version verified, zero failures/aborts. Pinned OTP-29.1.1 source build succeeded. All24 new expression results match independent expected outcomes;8 compiled modules confirm selected guards/if_clause/body exceptions/side effects/common export/closure capture/50000 tail calls;5 new compiler rejections confirm unbound variables and unsafe branch exports with construct/line reason fields. Original322 expressions,28 compiled sources/outcomes/generated hashes and5 diagnostic contracts remain unchanged.

Same-head Windows CI passes **516/516** and full generated .erl/native hybrid/incremental/negative/clean/local packages/consumer/CLI/readiness. [Raw proof/provenance](validation/part-045/remote-checkpoint.json), [strict source/hash expectations](validation/part-045/corpus-audit.json), [CI tests](validation/part-045/ci-tests.json). Readiness compares immutable part044 tests:62 MFAs/delta0, erlang40/351, lists19/91, maps2/34, io1/53. No new MFA, whole-module or semantic-percentage promotion; overall approximately5% coarse estimate.

Local full pipeline remains part044 evidence; this part verifies remote same-head full pipeline. Source layout151 types/no violations remains unchanged. Full grammar/optimizer/native lowering/annotations/warnings/source stacks/signals/resources/reductions/OTP/reference suites remain unfinished. Next grammar begin blocks and missing operators with binding/error contracts, then Erlang OTP callback adapters/signals. Historical355/355 at3c138b2 keeps its original scope.

## Part 046 — Begin and all ordinary expression operator spellings

Implemented begin sequences with binding export/order/tail position; bnot,band,bor,bxor,bsl,bsr,and,or,xor and catch. Signed BigInteger operations handle negative/huge counts, arithmetic right shifts and zero; reject nonintegers with badarith. Strict boolean operands both execute before badarg checks. Catch returns normal values/throw payload/EXIT reason or EXIT error+stack, rolls back bindings on failure and marks new bindings unsafe. Logical stack frames retain actual operation/call arguments; full OTP stack annotations/call frames are not equivalent.

Corrected existing match/send equal precedence, right-associative andalso/orelse and non-associative comparison syntax. Strict operator operands use independent incoming scopes and merge bindings; short-circuit RHS bindings are not exported. [Operator matrix](operator-support.json) enumerates33 ordinary unary/binary forms (31 spellings). Record syntax/generators/maybe ?= belong to separate unfinished grammar constructs, not this ordinary expression operator inventory.

Full local pipeline passes **614/614 (+98)**:73 independent value/error expressions,12 generated modules (including50000 begin tail calls),7 diagnostic scope cases,6 parser/hybrid/stack checks. Native begin/operators/catch run in real hybrid and local package consumer; generated compiled modules cover the new constructs. [Tests](validation/part-046/tests.json), [integration](validation/part-046/integration-results.json), [pinned audit](validation/part-046/reference-audit.json). Initial build stopped at a duplicate helper method name; corrected BinaryValue naming, preserved initial-build.log, final full pipeline passed with zero test failures.

Oracle plan **419 expressions +48 modules +17 diagnostics =484**; previous346/36/10 inputs and generated hashes preserved. New-head live comparison/CI pending;392/392 atc7e575b remains historical part045.156 namespace-level types/no violations. Registry62/delta0, lists19/91, erlang40/351, maps2/34, io1/53; readiness compares immutable part045 CI. No new MFA/whole-module promotion; approximately5% overall coarse estimate. CLR left-shift result bit index bound int.MaxValue is a local resource limit, not full allocation/reduction/fairness equivalence; no near-bound huge positive allocations ran. Next grammar/OTP callback adapters and complete source/debug/stack contracts.

## Part 047 — Verified begin and ordinary operator subset

Exact implementation head **b7b8c72fe98cebc877b2695bb6189dcd3a31c517** passes **419/419 expressions +48/48 compiled modules +17/17 diagnostics =484/484**, complete/version verified, zero failures/aborts. Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 source build succeeded. All73 independent new expression outcomes match,12 new compiled modules confirm begin/tail calls/bit/strict boolean/precedence/scopes/catch reasons and stack structure,7 diagnostics confirm operator operand isolation/short-circuit/catch unsafe bindings and begin unbound variables. Historical346/36/10 source/expected/generated/hash contracts preserved.

Same-head Windows CI passes **614/614** and full generated/hybrid/incremental/negative/clean/local packages/consumer/CLI/readiness. [Raw proof](validation/part-047/remote-checkpoint.json), [strict provenance](validation/part-047/corpus-audit.json), [CI tests](validation/part-047/ci-tests.json). Readiness compares immutable part046 tests:62 MFAs/delta0; lists19/91, erlang40/351, maps2/34, io1/53. These are declaration ratios, not semantic completion.156 namespace types/layout unchanged. Local full pipeline remains part046 evidence, not rerun in this documentation part.

All ordinary operator spellings and begin are implemented, with selected reference evidence; [matrix](operator-support.json) remains Partially compatible because full operands/resources/annotations/reference suites are not exhaustively verified. Catch logical stack frames are not complete OTP stacks/source annotations; int.MaxValue CLR left-shift result-bit indexing is a local bound, no near-bound allocations were compared. Record/generator/maybe/try grammar and OTP callbacks/signals remain unfinished; approximately5% coarse overall estimate. Historical392/392 atc7e575b retains its scope. Next remaining grammar/Erlang callback adapters, full debugging/stacks/resources.

## Part 048 — Source-directed port policy and expression binding repair

The accepted implementation policy is now close source ports with explicit routine provenance and CLR deviations; see [OTP-PORTS](OTP-PORTS.md). First audit found sibling scope drift. Ported erl_eval expr_list/cons/ordered-dictionary merge and the erl_lint independent-expression walk skeleton for tuples/lists/static and dynamic call arguments. Added20 expression outcomes/6 compiled modules/8 diagnostics, preserving419/48/17 previous contracts. Local **648/648/full integration** passes; [proof](validation/part-048/checkpoint.json). Planned pinned corpus518 (439/54/25) and same-head CI648 await publication. Historical484/484 and CI614 remain scoped to b7b8c72.

Map binding scopes, bit evaluation callbacks, typed variable tables, generated parser/full grammar, optimized execution, source stacks/resources/scheduler/OTP remain incomplete. New Apache-2.0 adapted file retains Ericsson notices; local packages include the port register/full license.158 source types/no layout violations,62 MFAs/delta0; approximately5% overall, no whole-module promotion or public package release.

## Part 049 — Compiled binding mismatch preserved

Same-head648/full CI at9ae76d7 passes, but complete/version-verified pinned518 comparison returns517 passed/one failure:439 expressions,53/54 compiled modules,25 diagnostics. Compiled tuple conflict returns badmatch2 in OTP; shared evaluator/fixture returns badmatch1, which is correct for erl_eval. Preserve [raw evidence](validation/part-049/checkpoint.json); no518/518 claim. Next separate compiled policy with source-guided nonconstant/order/closure checks. Dated map/bit audit correction is in OTP-PORTS.md; runtime sequential evaluation must not be confused with independent static scope checks. Registry62/delta0, approximately5%; full lowering/grammar/traces/resources incomplete.

## Part 050 — Compiled binding correction

Separate ModuleDefinition binding subset policy corrects the preserved compiled badmatch2 versus interpreter badmatch1 mismatch. Independent child/function capture scopes retained; four additional compiled nonconstant/call/cons-order/function-local cases pass. The single old compiled FixtureExpected correction follows official retained part049 output and keeps source/generated hashes. Local **652/652/full integration** passes; [proof](validation/part-050/checkpoint.json). Corpus439/58/25=522 awaits new-head reference/CI after publication; do not claim517/518 failure resolved against OTP until compared. Historical484/484 remains separate.

The adapter is not full Core lowering: internal child-body constraint timing, known-variable states, IR/optimizer/source stacks/resources remain incomplete. Current native hybrid standalone lowering retains interpreter semantics.159 types/no violations; registry62/delta0, approximately5% overall. Source notices/full license/updated register remain in local packages. Next verify522 then targeted map base/static scopes and deeper Core lowering.

## Part 051 — Verified distinct compiled/interpreted binding paths

Implementation head **2bf65a001186e19555b21ca1f44985e4ab830bf3** passes **439/439 expressions +58/58 compiled modules +25/25 diagnostics =522/522**, complete/version verified against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Same-head Windows CI passes **652/652/full integration**. The original compiled tuple conflict now matches official badmatch2 while erl_eval remains badmatch1; added nonconstant/call/cons-side-effect/function-local checks pass. [Raw proof](validation/part-051/remote-checkpoint.json), [strict provenance](validation/part-051/corpus-audit.json). Historical439 expressions/25 diagnostics and54 module sources/generated hashes remain unchanged. The sole prior FixtureExpected correction matches the retained part049 official reference outcome; its517/518 failure is preserved.

159 namespace-level types/no violations, registry62/delta0 against immutable part050 tests. erlang40/351,lists19/91,maps2/34,io1/53 are declaration presence, not semantic completion. Approximately5% coarse overall estimate. Local full pipeline stays part050 evidence; no executable change or rerun in this evidence part. Source-directed port register/notices remain separate from a claim of full compiler port.

Full Core known-variable/IR/internal child-body constraint timing/optimizer/stacks/resources/grammar/lint remain incomplete. Native hybrid standalone expressions retain the interpreter route. Next targeted map base/static scopes and deeper Core lowering; sequential map/bit runtime order must remain distinct from static isolation. No whole-module promotion/new dependency/production BEAM/public NuGet or original-code public license selection.

## Part 052 — Map scopes and indexed exact-key lookup

Local **676/676/full integration** passes; [reports](validation/part-052/checkpoint.json). Map base and fields have independent interpreter input scopes; fields remain sequential, type/key checks precede the final merge. Static base/key/value scopes are independent; compiled modules use the distinct Core subset adapter. New corpus **449 expressions/63 modules/31 diagnostics=543** awaits new-head oracle/CI. Historical522/522 remains part051 at2bf65a0. Prior439/58/25 sources/outcomes/generated hashes unchanged;62 MFAs/delta0,161 namespace types/no violations; approximately5% coarse overall estimate.

MapTerm retains its privately owned exact-key dictionary for Get/TryGet, preserving sorted read-only entries, distinct numeric/signed-zero keys and input immutability. [Release microbenchmark](validation/part-052/map-lookup-benchmark.json) compares the old C# scan with current indexed lookup on identical integer-key queries. Construction/retained memory/BEAM/end-to-end performance are outside this measurement. Full Core/lint/grammar/resources/stacks remain incomplete. Use C# optimizations that preserve observable contracts; cross-runtime superiority requires matched benchmarks. Next verify543/same-head676 CI, then deeper Core match timing and deferred binary errors.

## Part 053 — Verified map scopes and exact-key index

Implementation head **0fe8a7265f827e7c5e9e5010b762ceb2d75162e2** passes **449/449 expressions +63/63 compiled modules +31/31 compiler diagnostics =543/543** against exact source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Same-head Windows CI passes **676/676/full integration**. [Raw provenance](validation/part-053/remote-checkpoint.json), [strict contract/hash audit](validation/part-053/corpus-audit.json). All prior 439/58/25 expression/module/diagnostic contracts and generated hashes remain unchanged; ten independent map expectations, five compiled outcomes and six rejections match. Distinct interpreted base/field conflict and compiled conflict outcomes are verified for these inputs.

161 namespace-level types/no violations,62 MFAs / delta 0 against immutable part052 tests. API declarations erlang40/351,lists19/91,maps2/34,io1/53 are presence, not semantic completion; approximately 5% coarse overall estimate. Local full pipeline and old/new C# lookup measurements remain part052 evidence; no executable change or benchmark rerun here. [Performance policy](PERFORMANCE.md) records the measured workload, retained-memory tradeoff and requirements for a BEAM comparison. No cross-runtime speed claim.

Full Core lowering/internal child-body constraints/optimizer, complete lint/grammar/stacks/resources/runtime/OTP/reference suites remain unfinished. Native hybrid standalone expressions retain interpreter semantics. Next deeper Core match timing and deferred binary error ordering, then missing grammar/Erlang OTP callback adapters. Preserve failed historical evidence; no whole-module promotion/dependency/production BEAM/public NuGet/license change.

## Part 054 — Binary evaluation phases and compiled capture isolation

Local **698/698/full integration** passes; [checkpoint](validation/part-054/checkpoint.json). New BitExpressionBindings.cs adapts eval_bits evaluation-before-materialization, sequential interpreter bindings and post-construction export. Interpreter float conversion now precedes later field validation; compiled modules retain their size-before-float subset behavior and use independent child environments to prevent sibling exports leaking into function captures. [Pinned audit](validation/part-054/reference-audit.json), [corpus](validation/part-054/corpus-audit.json). All historical 449/63/31 source/outcome/generated hash contracts retained. New corpus **461 expressions/69 modules/35 diagnostics =565** awaits new-head OTP/CI; historical543/543 remains part053 at0fe8a72.

Hybrid/PackageReference consumer exercises the corrected interpreter error order. Initial fixture build used the wrong context identifier; corrected to configured erlangProcess and original failure preserved. Source-derived Ericsson AB1999-2025 Apache-2.0 notice and updated register/full license are retained in both local archives.163 namespace types/no violations;62 MFAs/delta0, API erlang40/351,lists19/91,maps2/34,io1/53; approximately5% coarse overall estimate.

Full Core/FUVs/empty-string compiler special lowering/JIT/error_info/resources/grammar/runtime/OTP equivalence remains incomplete. Native standalone hybrid uses interpreter semantics. No benchmark or speed claim in this semantic increment. Next verify565/same-head698 CI, then explicit compiler empty-string checks and more Core match timing before missing grammar/OTP callbacks.

## Part 055 — Pinned binary scope mismatch preserved

Published cd15a42 passes same-head698/full CI but exact-version, complete comparison returns **561/565**, with four compiled capture mismatches. All461 expressions and35 diagnostics pass; compiled track65/69. [Original proof](validation/part-055/checkpoint.json) preserves raw reports/head/run/job/archive hashes. My proposed independent compiled child policy was incorrect for binary pre-expressions: v3_core expr_bin_1 concatenates Eps sequentially, unlike safe_list's sibling groups. Official compiled closures observe earlier bit-segment bindings and fail with badmatch99/4/2/99. Four newly added fixture expectations are also incorrect and must be corrected to this retained reference output; no prior63 compiled fixture changes.

The interpreter float-phase fix and compiled size-before-float probe pass. Do not claim565/565 or promote full scopes from local698 tests. Historical543/543 remains part053. Next restore sequential bit values/sizes for both modes, retain their distinct float materialization order, correct exactly the four new expectations and verify a fresh implementation head. Full Core/FUVs/empty-string special checks/JIT/error metadata/resources remain incomplete; approximately5% coarse overall estimate,62 MFAs/delta0.

## Part 056 — Sequential binary and compiled map pre-expressions

Local **702/702 tests and full integration passed**; immutable [reports](validation/part-056/tests.json), [integration](validation/part-056/integration-results.json), [pinned source audit](validation/part-056/reference-audit.json) and [corpus audit](validation/part-056/corpus-audit.json). Both binary evaluation modes now evaluate value/size pre-expressions sequentially. Compiled map base/key/value pre-expressions are sequential as v3_core expr_map/map_build_pairs_1 specify; interpreter map base/fields retain separate incoming scopes and final merge. Static erl_lint name checks remain independent. This corrects the mistaken generalization of safe_list child scopes to binaries/maps in earlier parts.

Exactly four new part054 binary fixture expectations were corrected to the official outcomes retained in part055. All69 historical compiled sources/generated hashes, the earlier63 expectations,461 expression inputs and35 diagnostic inputs remain unchanged. Four new compiled map capture cases increase the next reference plan to **461/73/35 =569**. Fresh same-head OTP/CI proof is pending; previous failed561/565 and latest fully passing543/543 are preserved. No runtime MFA added:62 total (erlang40/351,lists19/91,maps2/34,io1/53). Overall remains approximately5%, a coarse estimate. No full Core/FUVs/empty-string lowering/JIT/resource/error-metadata/grammar/runtime/OTP or BEAM speed claim. Next verify569 on the published implementation head, then audit compiler empty-string checks and Core match timing.

## Part 057 — Exact-version correction proof

Published implementation **2435ce9** now passes **569/569** complete, version-verified comparisons against source-built OTP-29.1.1: **461 expressions,73 compiled modules,35 compiler diagnostics**, zero mismatches/aborts/infrastructure errors. Same-head Windows CI passes **702/702 tests and full integration**. [Reference provenance](validation/part-057/remote-checkpoint.json), [raw comparison](validation/part-057/differential.json), [CI](validation/part-057/ci-tests.json), [contract audit](validation/part-057/corpus-audit.json). All461 prior expression outcomes, all69 compiled sources/generated hashes and all35 diagnostic contracts are retained; the earlier63 compiled expectations remain unchanged. Exactly four new part054 binary expectations are corrected to the official values preserved in failed part055, and the four new compiled map capture cases independently match. The failed561/565 checkpoint is retained, never overwritten.

This verifies the selected sequential binary/map pre-expression capture correction and distinct interpreter/compiled float phases. It does not establish full Core/FUV/lint/optimizer/JIT/resource/error_info/stack/grammar/module compatibility. Generated C# still uses the AST evaluator. No new MFA:62 (erlang40/351,lists19/91,maps2/34,io1/53), approximately5% coarse overall estimate; no BEAM performance claim. Next audited implementation: compiled empty-string size prechecks and further Core match timing, followed by remaining grammar and Erlang OTP callback adapters.

## Part 058 — Compiled empty-string segment prechecks

Local **716/716 tests and full integration pass**; [tests](validation/part-058/tests.json), [integration](validation/part-058/integration-results.json), [source](validation/part-058/reference-audit.json), [corpus](validation/part-058/corpus-audit.json). Source-directed v3_core bitstr empty integer/float string handling now checks dynamic sizes for integer/nonnegative values before later callbacks and removes valid empty segments without allocating storage. Direct literal failures retain deferred construction; interpreted erl_eval phases remain distinct. Six independent expression cases and eight compiled fixtures test failure/side-effect timing, unused float width/huge size, zero and exported bindings. Native hybrid and PackageReference consumer exercise interpreter timing.

All461 previous expression inputs,73 compiled sources/expectations/generated hashes and35 diagnostics are preserved. Next oracle plan **467/81/35 =583**; latest actual passing reference proof remains569/569 at2435ce9, part057, pending fresh head. Initial715/716 fixture syntax failure and an audit-build/clean overlap are retained. No new dependency or MFA:62 total; approximately5% overall remains a coarse estimate. Full Core/constant folding, invalid binary literal lowering, warnings/error_info/JIT/resources/grammar/runtime/OTP suites and BEAM performance superiority remain unverified. Next publish and verify583/reference plus same-head716 CI, then further Core match timing/remaining grammar and OTP adapters.

## Part 059 — Empty-string exact-version proof

Published implementation **77c4bf7** passes **583/583** complete, version-verified comparisons against source-built OTP-29.1.1: **467 expressions,81 compiled modules,35 compiler diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **716/716 tests and full integration**. [Provenance](validation/part-059/remote-checkpoint.json), [raw comparison](validation/part-059/differential.json), [CI tests](validation/part-059/ci-tests.json), [contract audit](validation/part-059/corpus-audit.json). Previous461 expression contracts,73 compiled sources/expected/actual/generated hashes and35 diagnostics are preserved; six new expression expectations and eight new compiled expectations independently match, with zero expectation corrections.

Selected empty integer/float string cases confirm dynamic size prechecks before following compiled callbacks, omission without storage for huge nonnegative sizes/unused float widths, zero size and binding export; interpreter error/effect timing stays distinct. This is not full Core/constant folding/optimizer/JIT/lint/error_info/resource/grammar/runtime/OTP equivalence.62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53; approximately5% coarse overall estimate and no BEAM performance superiority claim. Next continue audited Core match timing, remaining grammar and Erlang OTP callback adapters. Initial local fixture/audit failures remain preserved in part058.

## Part 060 — Explicit compiled match timing and failure values

Local **725/725 tests and full integration pass**; [tests](validation/part-060/tests.json), [integration](validation/part-060/integration-results.json), [source audit](validation/part-060/reference-audit.json), [corpus](validation/part-060/corpus-audit.json). CompiledBindingScope separates prior sibling match constraints from lexical child environments. Explicit structural matches check those constraints at the match point, retain the whole RHS as badmatch reason and stop subsequent effects. Scope copies preserve constraints; anonymous function captures copy lexical bindings only. Existing tuple/list/call capture contracts stay intact. Full known-variable group stacks/FUV/case-receive pattern constraints and optimizer remain unported.

Exact previous implementation51b5a4b, with only new fixtures copied in, fails six compiled regressions:719/725. All source implementation blobs match the archived commit; [old/new proof](validation/part-060/baseline-regression-proof.json). New cases cover tuple/list/call whole-value failures, block effect suppression, nested tuple constraints and map RHS failures; three interpreted controls preserve distinct outcomes. Hybrid/PackageReference consumer exercises interpreter control. Initial fixture MapTerm constructor compile error is retained and corrected. Previous467 expression inputs,81 compiled contracts/hashes and35 diagnostics are unchanged. Fresh plan **470/87/35 =592** awaits published-head OTP/725 CI; latest actual reference583/583 remains part059.62 MFAs/delta0, approximately5% coarse overall; no full language/module/performance equivalence claim. Next publish and verify592, then further Core match/grammar/OTP adapter work.

## Part 061 — Structural match exact-version proof

Published implementation **df42a94** passes **592/592** complete, version-verified comparisons against source-built OTP-29.1.1: **470 expressions,87 compiled modules,35 compiler diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **725/725 tests and full integration**. [Provenance](validation/part-061/remote-checkpoint.json), [raw comparison](validation/part-061/differential.json), [CI tests](validation/part-061/ci-tests.json), [contract audit](validation/part-061/corpus-audit.json). Previous467 expression contracts,81 compiled sources/expected/actual/generated hashes and35 diagnostics remain unchanged. Three new interpreted controls and six new compiled structural match fixtures independently match, zero expectation corrections.

Selected explicit compiled sibling matches now fail at the pattern check with the whole RHS and suppress following effects, while lexical environments/function captures remain independent. The exact old implementation still demonstrates six failures under new fixtures, retained in part060. Full Core known-group stacks/FUV/case-receive pattern constraints/optimizer/lint/JIT/resource/error_info/grammar/runtime/OTP remain incomplete; no whole-component promotion.62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53; approximately5% coarse overall estimate, no BEAM speed claim. Next substantive language/OTP work: audit and implement remaining try grammar/error paths or Erlang callback adapters, retaining compiler scope regression coverage.

## Part 062 — Source-directed try/of/catch/after

Local **794/794 tests and full integration pass**; [tests](validation/part-062/tests.json), [integration](validation/part-062/integration-results.json), [pinned source](validation/part-062/reference-audit.json), [corpus integrity](validation/part-062/corpus-audit.json). Try AST/parser/code generation and hybrid blocks now support optional of, catch class/reason/stack patterns and guards, and after. Catch protects only the body; of/handler exceptions pass outward, after always runs using original bindings and its failure supersedes prior outcomes. Body bindings reach of only; new variables remain unsafe outside. Stack variables must be fresh and are forbidden in catch guards. Protected local calls complete inside the region; full exception-aware tail-call lowering remains unported.

26 expression cases,29 compiled modules and six unsafe-variable diagnostics expand the fresh plan to **496/116/41 =653**. All470 historical expression inputs,87 compiled sources/expectations/generated hashes and35 diagnostics remain unchanged. Eight local parser/stack/hybrid tests bring69 added tests total. Stack-specific negative diagnostics are local-only, not yet a normalized reference diagnostic track. Initial full build failed because hybrid lexical depth did not recognize try; retained [failure](validation/part-062/initial-validation-failure.log), corrected and full pipeline rerun. Ericsson Apache notices/modification labels, dependency decision and OTP-PORTS register retained in both local packages. No new dependency/MFA;62 registered exports remain partial. Overall approximately5% coarse estimate; no full grammar/Core/lint/stacks/resources/runtime/OTP or performance superiority claim. Historical actual reference remains592/592 atdf42a94; next publish this implementation and verify653 plus same-head794 CI, then remaining grammar/Erlang callback adapters.

## Part 063 — Try exact-version reference proof

Published implementation **f202bca** passes **653/653** complete, version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **496 expressions,116 compiled modules,41 diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI **794/794 and full integration** passes. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38045723169), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38045713583), [immutable provenance](validation/part-063/remote-checkpoint.json), [raw outcomes](validation/part-063/differential.json), [corpus audit](validation/part-063/corpus-audit.json). All470 previous expression contracts,87 compiled sources/expected/actual/source-generated hashes and35 diagnostic contracts remain unchanged;26 new try expressions,29 modules and six unsafe-variable diagnostics independently match. No expectation correction.

Try catch protects only the body, not of/handler bodies; after always runs and may override prior outcomes. Successful body bindings reach of; incoming bindings reach handlers/after. New variables stay unsafe outside. Compiled protected local calls and hybrid/PackageReference paths are covered. Source-built proof does not establish full grammar/Core/lint/warnings/source stacks/exception-tail-call lowering/protected recursion/resources/runtime/OTP or speed superiority. Three local stack-negative diagnostics remain outside the normalized exact-reference diagnostic track. Original trycatch_SUITE/erl_eval_SUITE contracts were inspected read-only; full upstream suites were not executed. Initial hybrid depth failure stays preserved in part062.169 namespace types/no layout violations,62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Overall approximately5%, coarse without weighted denominator; whole components remain partially compatible. A post-corpus local probe found a signed catch reason followed by :Stack is misparsed as a module qualifier; retained signed-reason-probe.erl/log. Next fix that parser edge with new fixtures and fresh-head proof, then maybe/?= grammar, preserving all try/scope regressions.

## Part 064 — Signed catch patterns before stacktrace

Local **800/800 and full pipeline** passes; [tests](validation/part-064/tests.json), [integration](validation/part-064/integration-results.json), [source](validation/part-064/reference-audit.json), [corpus](validation/part-064/corpus-audit.json). Parser now propagates catch-pattern qualifier stop through unary operands: error:-1:S, error:-1.5:S and error:-(1):S parse as class/reason/stack instead of a module call. Three independent expression fixtures and three compiled modules, plus hybrid/PackageReference consumer, cover the defect. Original ERL002 source/log remains immutable in part063. No precedence/module-call/bit syntax change.

Fresh plan **499/119/41 =659** awaits published-head reference and800 CI. All496 historical expression inputs,116 module sources/expectations/generated hashes and41 diagnostics preserved.170 namespace types/no violations,62 MFAs/delta0, approximately5% coarse overall. Existing source notices/register retained in local packages, no new dependency/public NuGet/BEAM requirement. Actual historical proof653/653 remains part063/f202bca. Full pattern/grammar/lint/stacks/Core/resources/OTP remain partial, no speed claim. Next publish and verify659/same-head800, then audited maybe/?= grammar.

## Part 065 — Signed catch patterns exact-version proof

Published implementation **5cbf0e8** passes **659/659** complete, version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **499 expressions,119 compiled modules,41 diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **800/800 and full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38046545340), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38046537141), [immutable head/artifact provenance](validation/part-065/remote-checkpoint.json), [raw outcomes](validation/part-065/differential.json), [strict contracts](validation/part-065/corpus-audit.json). All496 previous expression contracts,116 compiled source/expected/actual/source-generated hashes and41 diagnostics remain unchanged. Three new independent signed integer/float/parenthesized catch expression cases and three modules match, zero expectation corrections.

The valid negative reason followed by :Stack now parses and executes correctly; original ERL002 probe stays immutable in part063. Same-head hybrid/PackageReference consumer also tests the fix. Original try grammar/error/scoping cases remain verified.170 namespace types/no layout violations,62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Whole components partial, overall approximately5% coarse without weighted denominator. Full grammar/patterns/typed lint/warnings/source stacks/Core/optimizer/protected recursion/resources/signals/OTP remain unfinished; stack-specific negative diagnostics are local-only, no cross-runtime speed claim. Local full pipeline remains part064 evidence; proof part changes no executable. Next extend stack-variable guard scope checks (nested guards/function shadowing), then audited maybe/?= grammar, comprehensions and Erlang callback adapters.

## Part 066 — Catch stack guard taint boundary

Local **815/815 and full integration** passes; [tests](validation/part-066/tests.json), [integration](validation/part-066/integration-results.json), [pinned sources/tests](validation/part-066/reference-audit.json), [corpus](validation/part-066/corpus-audit.json). erl_lint catch_clause taints only GuardVt; handler body uses untainted Vt2. C# now uses a temporary catch guard scope, preventing the marker from leaking into body if/case/receive guards, nested catches or captured/shadowed fun parameters. Original valid-source ERL007 is retained in [probe](validation/part-066/old-probe.erl)/[failure](validation/part-066/old-probe.log); fixed CLI accepts that same source. Freshness and direct catch guard restrictions remain.

Six independent expressions/six compiled modules and three normalized stacktrace_guard/stacktrace_bound rejection cases expand the plan to **505/125/44 =674**. DiagnosticCaseKind/independent expected code/message/reason catalog removes the harness's ERL006-only assumption without changing historical41 contracts. All499 historical expression inputs,119 module sources/expectations/generated hashes remain unchanged. Hybrid/PackageReference consumer exercises shadowed fun guard.172 namespace types/no violations,62 MFAs/delta0; compiler447 related tests. Historical actual reference659/659 belongs to part065/5cbf0e8. Fresh674 and815 CI await publication; source notices/register retained in both local archives, no dependency/public NuGet/production BEAM changes. Approximately5% coarse overall; full typed lint/warnings/spans/stack frames/Core/resources/grammar/OTP remain incomplete and no BEAM speed claim. Next publish/verify674, then audited maybe/?= grammar.

## Part 067 — Catch stack-variable scope exact-version proof

Published implementation **c2c516b** passes **674/674** complete, version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **505 expressions,125 compiled modules,44 diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **815/815 and full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38048118767), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38048109596), [immutable provenance](validation/part-067/remote-checkpoint.json), [raw outcomes](validation/part-067/differential.json), [strict contracts](validation/part-067/corpus-audit.json). All499 previous expression contracts,119 compiled sources/expectations/outcomes/generated hashes and41 diagnostics remain unchanged; zero expectation corrections.

Six new expressions and six modules verify stack-variable use in handler if/case/receive guards, captured and shadowed fun guards and a nested catch guard. Three normalized reference rejections now verify direct catch-guard stack access, a previously bound stack variable and reason-variable reuse as the stack variable. These selected diagnostic contracts are reference-backed; complete stack frames, typed lint state, warnings and source annotations remain unfinished. The original local ERL007 probe and its corrected CLI execution remain immutable in part066. Full same-head integration includes the native hybrid and local PackageReference consumer.

172 namespace-level types/no layout violations;62 registered MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Whole components remain partial; overall approximately5% is a coarse estimate without a weighted denominator. No full grammar/Core/optimizer/protected recursion/resources/signals/OTP suite or BEAM performance proof. No executable changes in this evidence part; actual local815 pipeline remains part066. [Read-only next-source audit](validation/part-067/next-maybe-source-audit.json) records pinned grammar/evaluator/lint/Core blobs and candidate contracts; maybe is not implemented. Next implement source-audited maybe/?= mismatch/else/scope contracts across interpreted, generated and hybrid paths, preserving674 cases.

## Part 068 — Source-directed maybe and ?= implementation

Local **892/892 tests and full pipeline** pass: [tests](validation/part-068/tests.json), [integration](validation/part-068/integration-results.json), [sources](validation/part-068/reference-audit.json), [independent corpus](validation/part-068/corpus-audit.json). Parser/AST/evaluator/generated C#/hybrid now support maybe and top-level ?= sequence items. On mismatch return the RHS without else, otherwise select ordered guarded else clauses; no match raises {else_clause,Value}. Ordinary badmatch/error/throw/exit propagate. Matching uses a private candidate scope; incoming bindings are available while body/else bindings never export. Static lint marks body-only variables unsafe in else and all new variables unsafe after the block.

Thirty new independent expressions and thirty compiled modules plus eight normalized unsafe-variable diagnostics expand the plan to **535/155/52 = 742**. Six invalid-grammar cases, one guard rejection and two hybrid boundary checks are permanent regressions. Native hybrid and local PackageReference consumer execute successful nested matches and mismatch else selection. All505 historical expression inputs,125 module sources/expectations/generated hashes and44 diagnostics retained.175 namespace types/no layout violations,62 MFAs/delta0. Actual latest reference proof remains674/674 at c2c516b, part067; fresh742/892 CI pending publication. Source notices/register and exact upstream license retained in both local packages; no new dependency or public NuGet publication.

Existing alias/record patterns, full typed lint/warnings/spans, Core/optimizer/tail-recursion lowering and stack/reduction/resource contracts remain incomplete. Overall approximately5% coarse without a weighted denominator; no whole-component or BEAM performance promotion. Next publish and verify this exact implementation head against pinned OTP, then audited comprehensions and Erlang OTP callback adapters.

## Part 069 — maybe/?= exact-version reference proof

Published implementation **2b99ef0** passes **742/742** complete, version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **535 expressions,155 compiled modules,52 diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **892/892 and full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38049433602), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38049411127), [immutable provenance](validation/part-069/remote-checkpoint.json), [raw outcomes](validation/part-069/differential.json), [strict contracts](validation/part-069/corpus-audit.json). All505 previous expression contracts,125 compiled sources/expectations/outcomes/generated hashes and44 diagnostics remain unchanged; zero expectation corrections.

Thirty new expression and thirty generated/reference module fixtures verify maybe/?= sequence order, first/later mismatch, returned RHS, ordered guarded else selection, else_clause failures, propagation of error/throw/exit and ordinary badmatch, incoming variable constraints, tuple/list/map/bit patterns, nested blocks, RHS bindings and side effects. Eight normalized unsafe-variable rejection contracts verify non-export of body/match/RHS/else bindings and body variables unavailable in else guards/bodies. Same-head full pipeline also verifies native hybrid and local PackageReference consumer. Part068 stores892 local tests/full pipeline, source/reuse/license/layout audits; this part changes no executable code.

175 namespace types/no layout violations;62 registered MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Compiler492 related passed tests; Hybrid19. Whole components remain partial; approximately5% overall is coarse without a weighted denominator. Existing alias/record patterns, typed lint/warnings/spans, full Core/optimizer/tail-recursion lowering, stack/reduction/resource/signal contracts, full upstream suites and remaining OTP services are unfinished. [Read-only upstream maybe_SUITE review](validation/part-069/upstream-suite-audit.json) confirms sequence/mismatch/nesting contracts and existing unsupported record/alias patterns; full suite not run. No BEAM performance claim. Next harden alias/list-prefix patterns, then source-audited comprehensions and Erlang OTP callback adapters, preserving742 reference cases.

## Part 070 — Alias and literal list-prefix pattern implementation

Local **949/949 tests and full pipeline** pass: [tests](validation/part-070/tests.json), [integration](validation/part-070/integration-results.json), [pinned source audit](validation/part-070/reference-audit.json), [corpus](validation/part-070/corpus-audit.json). AliasPattern matches both sides against the same term with transactional bindings and original key/size scope. Parser supports aliases in maybe, case, function/fun arguments and catch reasons; static aliases retain independent incoming key/size checks. Literal strings/proper integer lists before ++ normalize into existing list/tail patterns; variable/noninteger/improper prefixes are rejected. Generated C# and native hybrid/PackageReference consumer cover whole/suffix aliases and catch stack boundaries.

24 independent expression fixtures,25 compiled modules and six diagnostics expand the plan to **559/180/58 = 797**; two direct runtime tests cover whole-term identity and mismatch rollback. All535 historical expression inputs,155 module sources/expectations/generated hashes and52 diagnostic contracts retained.178 namespace types/no layout violations,62 MFAs/delta0. Initial946/949 run exposed two malformed new '=<<' sources (three failures); corrected only to '= <<' while preserving expected semantics; [failures](validation/part-070/initial-failures.json), [log](validation/part-070/initial-tests.log). Latest actual reference remains742/742 at2b99ef0, part069; fresh797/949 CI pending publication. Upstream notices/register and exact license retained in both local packages, no new dependency/public NuGet/original-code license selection.

Full record/constant-expression pattern grammar, typed lint/warnings/spans, Core alias consolidation/optimizer/tail-recursion lowering, stack/reduction/resource/signals and full OTP suites remain incomplete. Overall approximately5% coarse without a weighted denominator; no whole-component or BEAM performance promotion. Next verify this published head against pinned OTP, then source-audited comprehensions and Erlang OTP callback adapters.

## Part 071 — Alias and literal-prefix exact-version proof

Published implementation **64579d4** passes **797/797** complete, version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **559 expressions,180 compiled modules,58 diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **949/949 and full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38051072215), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38051025327), [immutable provenance](validation/part-071/remote-checkpoint.json), [raw outcomes](validation/part-071/differential.json), [strict corpus](validation/part-071/corpus-audit.json). All535 previous expression contracts,155 compiled sources/expectations/outcomes/generated hashes and52 diagnostics retained; zero expectation corrections.

24 independent expression cases and25 compiled modules verify alias whole/parts/reversed/nested patterns, repeated exact matches, fallback, fun shadowing/function parameters, maybe matches/else, catch reasons before stack variables, literal string/integer-list/empty/chained/unicode prefixes and incoming map keys/bit sizes. Six normalized rejections verify independent alias map-key/bit-size scopes and reject variable/noninteger/improper prefixes. Two direct runtime rollback/identity regressions and hybrid/local PackageReference integration also pass CI. Initial malformed '=<<' new sources/failures remain immutable in part070; only their source spelling was corrected before publication, not semantic expectations. Actual local949/full pipeline and source/reuse/layout/package audits remain part070. This evidence part changes no executable code.

178 namespace types/no layout violations;62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Compiler524 related tests; Hybrid19, Runtime57, Core library162. Whole components remain partial; overall approximately5% coarse without a weighted denominator. Full record/constant-expression pattern grammar, typed lint/warnings/spans, Core alias consolidation/optimizer/tail-recursion lowering, stack/reduction/resource/signals, complete OTP services and upstream suites remain unfinished; no BEAM speed claim. Next audited comprehensions and Erlang OTP callback adapters. Development-oracle cache may be evaluated separately with exact commit/config/build provenance verification; no cache change in this part.

## Part 072 — Source-directed list comprehensions

Local **1026/1026 tests and full pipeline** pass: [tests](validation/part-072/tests.json), [integration](validation/part-072/integration-results.json), [source audit](validation/part-072/reference-audit.json), [corpus](validation/part-072/corpus-audit.json). List results support multiple templates, relaxed <-/strict <:- list generators, dependent/nested generators and guard/ordinary filters. Generator source bindings are discarded; each element matches freshly against the original key/size scope; private generator variables shadow incoming bindings. Relaxed mismatch skips; strict mismatch raises badmatch; improper tails raise bad_generator after preceding effects. Guard errors/nontrue values skip; ordinary nonboolean values raise bad_filter. Existing independent template-expression scopes and immutable Cons/Pattern matching are reused. Native hybrid and external PackageReference consumer cover filtering/strict patterns/outer bindings/shadowing.

36 independent expressions and 36 compiled modules plus five normalized scope rejections expand the plan to **595/216/63 =874**. All 559 historical expression inputs,180 compiled sources/expectations/generated hashes and58 diagnostics retained. Initial1022/1022 preceded two additional operator cases; the first full attempt stopped on SDK-generated incremental timestamp during overlapping audit builds. Isolated full run1022/1026 exposed four remote-operator filter failures; guarded remote operator calls now use existing Unary/Binary rather than an unregistered MFA lookup, with expectations unchanged. Final sequential1026/full validation ran in the isolated lc-validation Git worktree. At the owner's request source license banners are removed; upstream attribution/source modification map and the existing exact packaged OTP license are retained. LICENSE.local.txt is a deliberately uncommitted local draft, no original public license selected.

184 namespace types/no layout violations;62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Whole components remain partial, approximately5% coarse overall. Latest actual exact-source evidence remains797/797 at64579d4 with949 CI, part071; fresh874 and same-head1026 CI await publication. Binary/map comprehensions/generators, zip, assignment qualifiers, records/macros/full grammar, typed lint/warnings/spans, full Core lowering/optimization, stacks/resources/signals/services/upstream suites/BEAM speed remain unfinished. Next verify the published increment, then extend comprehensions or Erlang OTP callback adapters.

## Part 073 — List comprehension exact-source proof

Published implementation **e2cee56** passes **874/874** complete/version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **595 expressions,216 compiled modules,63 normalized compiler diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **1026/1026 and full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38053103504), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38053093380), [immutable provenance](validation/part-073/remote-checkpoint.json), [raw outcomes](validation/part-073/differential.json), [strict corpus](validation/part-073/corpus-audit.json). All559 earlier expression source/expected/actual contracts,180 compiled sources/fixture/reference/actual/generated hashes and 58 normalized diagnostic contracts retained; zero expectation corrections.

36 independent expression cases and 36 compiled modules verify relaxed/strict generators, exact repeated patterns, filters/errors, private generator/source/template bindings, dependent/nested order, multiple templates, alias/map/bit patterns, captured closures, side effects/improper-tail timing and explicit remote operator guard filters. Five rejections verify unbound source variables, no generator/filter export, discarded source bindings and template/filter boundaries. Initial part 0721022/1026 remote-operator failures and incremental-build interruption remain immutable; implementation fixed without changing expectations. Actual isolated-worktree1026/full local pipeline and byte-identical mirror audit remain part 072. The main-repository first build hit Roslyn AnalyzerDriver OutOfMemoryException; [initial log](validation/part-073/primary-initial-memory.log) is retained. The first processor-limited repeat still had 17 Roslyn metadata OOM test failures (1009/1026); [failed test snapshot](validation/part-073/primary-initial-tests-memory.json) and [log](validation/part-073/primary-initial-tests-memory.log) retained. A managed-heap-limited attempt ended without a complete report; [interrupted log](validation/part-073/primary-interrupted-gc.log) retained and no passing result attributed to it. The successful final run executes outside the constrained sandbox using DOTNET_PROCESSOR_COUNT=2, MSBUILDDISABLENODEREUSE=1 and DOTNET_GCConserveMemory=7 only within that invocation, without disabling analyzers/tests or changing repository configuration. [Official GC configuration](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector). This part changes no executable code. The main repository also passes a fresh same-head 1026/full pipeline after mirroring: [tests](validation/part-073/primary-tests.json), [integration](validation/part-073/primary-integration-results.json), [log](validation/part-073/primary-validation.log).

Compiler 565 related tests (+41 in implementation), Differential 285 (+41), Hybrid 19, Runtime 57, Core library 162; counts overlap and are not summed as coverage. 184 namespace types/no layout violations; 62 MFAs/delta0:erlang 40/351,lists 19/91,maps 2/34,io 1/53. Whole components remain partial; overall approximately 5% coarse without a weighted denominator. Binary/map comprehensions/generators, zip/assignment qualifiers, records/macros/full grammar, typed lint/warnings/spans, complete Core lowering/optimization, stacks/resources/signals/OTP services/full upstream suites and BEAM performance remain unfinished. Next source-audited remaining generator forms or Erlang OTP callback adapters. The owner requires no C# license banners; source/modification attribution remains in the existing port register with the existing exact packaged OTP license. LICENSE.local.txt remains uncommitted/unpublished; no original-code public license selected.

## Part 074 — Source-directed binary comprehensions and generators

Local **1106/1106 tests and full pipeline** pass: [tests](validation/part-074/tests.json), [integration](validation/part-074/integration-results.json), [source audit](validation/part-074/reference-audit.json), [corpus](validation/part-074/corpus-audit.json). Binary bitstring templates support empty/unaligned chunks, nested/list/binary sources and existing filters. Relaxed <= consumes complete mismatches and ignores incomplete tails; strict <:= raises badmatch with the original remaining bitstring. Dynamic sequential sizes, signed/little-endian fields, UTF and invalid float encodings reuse the scanner shared with ordinary exact BitPattern matching. All comprehension bindings remain private. Input bytes are copied once per generator; result chunks are collected and materialized once, without a BEAM speed/resource-equivalence claim.

36 independent expressions/36 modules/four normalized scope diagnostics expand the plan to **631/252/67=950**. Historical595 expression inputs,216 module sources/expectations/generated hashes and63 diagnostics retained. Four additional local tests cover malformed/unsized/empty patterns, illegal guards, hybrid AST emission and the explicit zero-progress policy. Native hybrid/local PackageReference consumer execute unaligned output and strict decoding. Initial CS0136 build-name conflict was fixed; initial1102/1102 tests precede four boundary checks; no expectation correction.186 namespace types/no layout violations;62 MFAs/delta0. Historical reference proof remains874/874 at e2cee56 with1026 CI, part073; fresh950/same-head1106 CI await publication.

Empty generator patterns remain unsupported; zero-progress dynamic patterns raise system_limit to avoid nontermination. These are local policies, not live OTP-equivalence claims. Full typed string patterns, map/zip/assignment comprehensions/generators, records/macros/full grammar, typed lint/spans/Core/optimizer/resources/stacks/signals/OTP suites remain unfinished. Components remain partial; approximately5% coarse overall, no measured compatibility percentage. No new dependency/backend; copyright/modification attribution retained in existing OTP-PORTS.md/exact OTP-LICENSE.txt. LICENSE.local.txt remains uncommitted/unpublished/unpackaged. Next verify the published head, then source-audited map generators/comprehensions or Erlang callback adapters.

## Part 075 — Binary comprehension exact-source proof

Published implementation **6273efc** passes **950/950** complete/version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **631 expressions,252 compiled modules,67 normalized diagnostics**, zero mismatch/abort/infrastructure failure. Same-head Windows CI passes **1106/1106 and full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38056284823), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38056271327), [immutable provenance](validation/part-075/remote-checkpoint.json), [raw outcomes](validation/part-075/differential.json), [strict contracts](validation/part-075/corpus-audit.json). All595 earlier expression source/expected/actual contracts,216 module sources/expectations/generated hashes and63 diagnostic contracts remain unchanged; zero expectation corrections.

36 independent expressions/36 compiled modules verify binary templates/empty/unaligned/nested results, list/binary/mixed/dependent generators, consuming mismatch, strict remaining-tail errors, sequential/incoming sizes, signed/endian/UTF/invalid-float fields, private shadow/source/body scopes and selected filter/effect timing. Four normalized rejections verify private/unbound variables. Empty/zero-progress resource policies are local-only and excluded from oracle. Initial CS0136 build and the interrupted incremental repeat remain in part074; final sequential1106/full local pipeline passed with all gates enabled. This evidence part changes no executable code. Current readiness milestones are condensed; historical detail remains in the English journal and immutable reports.

Compiler608 related tests (+43 in implementation), Differential325 (+40), Hybrid20 (+1), Runtime57 and Core library162; overlapping counts are not summed as coverage.186 namespace types/no violations;62 MFAs/delta0:erlang40/351,lists19/91,maps2/34,io1/53. Whole components remain partial; approximately5% coarse overall without a weighted denominator. Full typed string patterns, map/zip/assignment comprehensions/generators, records/macros/full grammar, typed lint/spans/Core/optimizer/resources/stacks/signals/OTP services/upstream suites and BEAM performance remain unfinished. Next source-audited map generators/comprehensions or Erlang callback adapters. No new dependency/backend/public package release; source/modification/copyright attribution remains in the existing register/license without source banners. LICENSE.local.txt remains uncommitted/unpublished/unpackaged.

## Part 076 — Map comprehensions and key/value generators

Local **1231/1231 tests and full pipeline** pass: [tests](validation/part-076/tests.json), [integration](validation/part-076/integration-results.json), [audit](validation/part-076/corpus-audit.json). Added #{K=>V || ...}, multiple output fields, relaxed K := V <- Map and strict <:- generators across map/list/binary result forms. Last repeated exact key wins; key/value patterns match together in a fresh private environment. Invalid sources/iterators raise bad_generator; strict mismatch raises {badmatch,{Key,Value}}. Template pair binding/effect timing uses separate interpreted/compiled adapters. Generator/body/source bindings remain private. maps:iterator/1, iterator/2 named undefined/ordered/reversed and next/1 are registered as partial APIs; valid tuple/none/explicit-key/partially consumed states are supported and validated before body effects. Native hybrid and isolated PackageReference consumers exercise multiple fields and strict/reversed generators.

56 independent expression/module fixtures plus six scope rejections expand plan to **687/308/73=1068**. Historical631 expressions/252 module source/expectations/generated hashes and67 diagnostics retained, no correction. Four extra local syntax/guard/hybrid checks and three direct MFA smoke cases added. Initial CS0136 local-name conflict was fixed; initial1211 tests precede the final boundary additions and remain retained.190 namespace types/no layout violations; registry65 (+3), erlang40/351,lists19/91,maps5/34,io1/53. The API counts are presence, not full semantic completion. Fresh1068-case exact-source comparison and same-head1231/full CI await publication; historical950/950 remains part075 at6273efc.

Pinned erl_parse/erl_eval/erl_lint/v3_core/maps and erts_internal sources inspected; [reuse](dependency-decisions.md), [attribution](OTP-PORTS.md), [blobs](validation/part-076/reference-audit.json). Existing CLR terms/collections/ValueTask and transactional patterns reused; no new dependency or production BEAM. Custom comparator iterator/2, opaque nonzero BEAM HAMT path states, exact large default traversal/effect order, full Core/resources/stacks/warnings, zip/assignment forms and full upstream suites remain incomplete. OTP default map order is unspecified; explicit ordered/reversed iterators control order. No speed comparison claimed. Approximately5% coarse overall without a weighted denominator. Source banners are omitted per owner instruction; exact upstream license/register packaged, LICENSE.local.txt remains uncommitted/unpublished/unpackaged, no original public license chosen or public NuGet publication.

Next publish implementation, run exact-source1068 and same-head1231 CI, preserve proof; then comparator iterators/zip generators or Erlang callback adapters.

## Part 077 — Original OTP exposes compiled map scheduling differences

Initial implementation59768e8 passed same-head **1231/full CI** but exact-source run [38058777055](https://github.com/develmax/Erlang.Net/actions/runs/38058777055) completed **1066/1068 with two compiled-module mismatches**: [retained initial report](validation/part-077/initial-differential.json), [metadata](validation/part-077/initial-oracle-metadata.json). All687 expressions and73 diagnostics matched; compiled mc-key-value-conflict officially raises badmatch1 (our provisional expected/actual2), compiled mc-effect-order officially ends with key (our provisional value). These are observable v3_core:mc_pairs ordering rules, not an erl_eval change.

New MapComprehensionBindings adapts mc_pairs/mc_tuples/lc_tq2 safe-prefix and first fallback pre-expression scheduling: reverse eligible keys, then first noneligible value/key/reversed prefix, then remaining key/value tuples. Generator/filter-bound values disable the optimization; existing compiled constraints handle conflicts. Eight independent effect-order expressions/modules cover full prefix, first/later fallback, distinct safe values and incoming/generator/filter bindings. Exactly two provisional map module expectations now match retained original outputs: [explicit corrections](validation/part-077/official-expectation-corrections.json). Sources/generated AST hashes for all308 existing modules remain unchanged; historical631 expressions/252 modules/67 diagnostics unchanged. Added domain-owned MapIteratorLayout path/arity/offset constants with distinct meanings for equal numbers.

Corrected local **1247/1247/full pipeline** passes: [tests](validation/part-077/tests.json), [integration](validation/part-077/integration-results.json), [log](validation/part-077/validation.log), [corpus](validation/part-077/corpus-audit.json). Plan **695 expressions/316 modules/73 diagnostics=1084**;193 namespace types/no violations, registry65/delta0 against part076. Fresh exact-source/same-head1247 CI must run after publication; do not promote the two original mismatches as verified yet. Historical successful950 remains separate. Full Core canonicalization, nested filter pre-expression metadata, custom comparator/opaque HAMT iterators/resources/warnings/stacks/zip/assignment/full suites remain unfinished. No dependency/backend/license change; source attribution remains consolidated and exact notices packaged without banners, LICENSE.local.txt uncommitted/unpublished/unpackaged. Approximately5% coarse overall; no BEAM speed claim.

Next publish correction and run exact-source1084 plus same-head1247/full CI. Preserve initial failures and all prior immutable checkpoints.

## Part 078 — Map comprehension exact-source proof

Published correction **7a35cdd** passes **1084/1084** complete/version-verified comparisons against source-built OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5: **695 expressions,316 compiled modules,73 normalized diagnostics**. Same-head Windows CI passes **1247/1247/full integration**. [Reference run](https://github.com/develmax/Erlang.Net/actions/runs/38060226859), [CI run](https://github.com/develmax/Erlang.Net/actions/runs/38060215435), [raw results](validation/part-078/differential.json), [provenance](validation/part-078/remote-checkpoint.json), [strict preservation](validation/part-078/corpus-audit.json). All631 old expressions/252 module contracts/generated hashes/67 diagnostics unchanged. All308 initial map module sources/generated hashes retained. Exactly two provisional expectations were corrected in part077 to retained original outputs; no further correction. [Two resolved initial mismatches](validation/part-078/resolved-initial-mismatches.json), [original failed1066/1068](validation/part-077/initial-differential.json) remain explicit.

Verified64 map/iterator/template-order expressions/modules and six scope rejections cover multiple fields, last exact key wins, mixed/dependent generators, relaxed/strict tuple matching, private bindings, named/tuple/partial iterators, validation-before-effects and source-directed compiled safe-prefix/fallback scheduling. Original erl_eval and compiled-module order are deliberately compared separately; native hybrid/package consumer passes. All checks planned for this increment are complete; no executable change or local full pipeline rerun in this evidence part. [Local full pipeline](validation/part-077/validation.log) belongs to part077.193 namespace types/no violations;65 MFAs, maps5/34 (+3 in part076), erlang40/351,lists19/91,io1/53; current readiness evidence-part delta0. Related test counts overlap, API presence is not semantic completion; whole components remain partial, approximately5% coarse overall without a weighted denominator.

Full Core canonicalization/nested filter pre-expression metadata, custom comparison iterator/2/opaque nonzero BEAM HAMT states, exact large default traversal/effect order/resources/stacks/warnings, zip/assignment/full grammar/OTP suites and BEAM performance remain incomplete. No new dependency/backend/public package release. Consolidated copyright/modification register and exact upstream license remain without source banners; LICENSE.local.txt uncommitted/unpublished/unpackaged. Next source-audited comparator iterators/zip generators or Erlang callback adapters; preserve all evidence and independently expected fixtures.

## Part 087 — Dynamic supervisor child management (2026-10-11)

Four registered Erlang APIs implement start_child/2, terminate_child/2, restart_child/2 and delete_child/2 for ordinary one_for_one/one_for_all/rest_for_one supervisors. All management runs in the owning mailbox. Preserve inactive permanent/transient specs, remove temporary children, keep source query/restart order, preserve Extra, source-shaped start failure child records and manual restart errors; validate before duplicate lookup. Supervisor requests now use gen_server infinity calls and call error context. BCL ordered lists and existing runtime/immutable terms/ValueTask adapt pinned lib/stdlib/src/supervisor.erl handle_call/start/save/del routines; consolidated Ericsson AB 1996-2026 Apache notices retained, no banners/new package/production BEAM, license draft excluded.

26 independently expected compiled Erlang modules cover lifecycle/missing/running/duplicate states, legacy/named specs, ignore, extra, failed/retry/throw/exit/raw starts, shutdown acknowledgement, temporary/transient exits, query order/counts and dynamic group restart schedules. Prior778 expressions/436 compiled module sources/expectations/generated hashes and79 diagnostics are unchanged. Registry85 (+4), supervisor9/23 and gen_server10/41 explicit-declaration API presence; these are partial APIs, not compatibility percentages. Local validation/original-source checkpoints are recorded in parts087/088; source comparison is pending until part088 is complete. Historical1293/1293 belongs to2fd52f8/part086.

Simple_one_for_one, significant/auto_shutdown, automatic restart retry/restarting states, returned dead/nonlinked PIDs, complete intensity/shutdown/signal races, sys/upgrades/stacks/resources and full upstream/performance suites remain unfinished. Approximately5% coarse overall. Next exact-source comparison and lifecycle/retry hardening; no full-module or BEAM speed claim.

## Part 088 — Exact-source dynamic supervisor verification (2026-10-11)

Implementation **32c827a517c7e372b0181fe08dfbde978200b16f** passes **1319/1319=778 expressions+462 compiled modules+79 diagnostics** against OTP-29.1.1 built from exact SHAad05823719d77c8faee87348ea39513d4e2f99c5. [Raw proof](validation/part-088/differential.json), [provenance](validation/part-088/remote-checkpoint.json), [preservation](validation/part-088/corpus-audit.json). Same-head CI **1512/1512 and full pipeline**, test names match local part087 exactly. Original version/build/head/completeness/tracks/artifacts and independent expectations verified. All prior778/436/79 contracts/hashes unchanged;26 new dynamic child contracts match with zero expectation corrections. Initial local failures retained part087. Local full1512 execution preceded commit with the same executable sources; no repeated full pipeline in this evidence-only part.

Ordinary-supervisor start/terminate/restart/delete transitions, errors/child-record payloads, Extra/ignore, temporary/transient exits, query order/counts, names, shutdown acknowledgement and dynamic group restart order selected contracts verified. Registry85, supervisor9/23 and gen_server10/41 partial API presence;218 namespace types/no violations. Consolidated notices packaged, draft excluded; no new dependency/BEAM/banners. Whole task approximately5% coarse, full modules/upstream suites/performance unfinished.

Source inspection confirms automatic restart failures require a restarting marker and tagged retry cast that yields to the mailbox; the current automatic engine exits on these failures. Next port this retry schedule and test shutdown/control interleaving, dead/nonlinked PIDs and intensity/time boundaries. Simple_one_for_one, significant/auto_shutdown, full sys/proc_lib/aliases/options/upgrades/distribution/hibernate/stacks/resources/races remain unfinished.

## Part 089 — Automatic supervisor restart retries (2026-10-11)

Failed automatic starts now publish restarting and append a private-reference-tagged retry cast, returning to mailbox dispatch. Query/count, restart/delete errors, terminate cancellation, replacement and shutdown can run between attempts. Each attempt consumes intensity; zero/exhausted budgets shut down. Ignored starts clear restarting. Group startup aborts at the first failure, preserves earlier active/later inactive children, removes temporary peers and retries the whole group or failed suffix according to strategy. Active count checks process liveness. CLR lists/sets/snapshots/ValueTask and a monotonic whole-second inclusive window adapt pinned supervisor routines; no timer/backoff/new dependency/production BEAM.

Local **1526/1526 and full integration**, [tests](validation/part-089/tests.json), [pipeline](validation/part-089/validation.log). **14 independently expected compiled modules** added; planned1333=778 expressions/476 modules/79 diagnostics. All prior778/462/79 sources/expectations/generated hashes preserved: [audit](validation/part-089/corpus-audit.json).219 namespace types/no violations.85 registered MFAs, delta0; supervisor9/23 and gen_server10/41 partial API presence. Source/license/reuse and package notice proof retained; draft excluded. Exact-source comparison and same-head CI are pending; historical1319/1512 proof belongs to32c827a/part088.

Full modules, upstream suites and performance remain unfinished, approximately5% coarse. Clock phase/dirty optimization/full time boundaries, dead/nonlinked PIDs, shutdown/signals/sys/proc_lib/stacks/resources, simple_one_for_one and significant/auto_shutdown remain partial. Next publish this implementation and verify exact-source1333 plus same-head1526 CI, then harden lifecycle and timing boundaries.

## Parts 090–091 — Invalid retry cast source correction (2026-10-11)

Additional pinned supervisor handle_cast inspection found no fallback clause. Part089 incorrectly ignored a wrong-tag cast and expected successful recovery in one provisional fixture. Correctly tagged stale retries are ignored; invalid shapes/private tags now raise function_clause. [Inspection](validation/part-090/inspection.json). The forged-tag fixture now traps exits and monitors the selected function_clause reason, leaving source stack contents wildcarded as an explicit existing limitation. All prior778/462/79 and13 other retry fixtures are unchanged; one provisional expectation/source corrected before the first original run completed. Initial9dec6df run remains historical and cannot serve as final proof; raw results will be retained separately in part092.

Correction passes **1526/1526 and full integration**, [tests](validation/part-091/tests.json), [log](validation/part-091/validation.log). Plan1333=778/476/79,85 MFAs/delta0,219 namespace types/no violations (layout unchanged from part089). [Audit](validation/part-091/corpus-audit.json). No new dependency/BEAM/banners; consolidated notices/draft exclusion retained. Next publish corrected implementation and inspect exact-source1333/same-head1526 CI; complete module/upstream/performance and lifecycle/time/sys/simple_one_for_one/significant/stacks/resource boundaries remain partial, approximately5% coarse.

## Part 092 — Exact-source automatic supervisor retry verification (2026-10-11)

Implementation **c20e9903f367bb4c0d1c42e0bc76cb9dff4f23de** passes **1333/1333=778 expressions+476 compiled modules+79 diagnostics** against exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. [Raw proof](validation/part-092/differential.json), [provenance](validation/part-092/remote-checkpoint.json), [preservation](validation/part-092/corpus-audit.json). Same-head CI **1526/1526 and full pipeline**, test names match local part091 exactly. All prior 778/462/79 sources/expectations/generated hashes unchanged;14 independent retry modules match with one provisional cast expectation/source corrected from pinned inspection before initial original comparison completed; final independent expectations match. Local full1526 execution preceded commit with identical executable sources; this evidence-only part does not repeat it.

Selected recovery/error/throw/exit/Extra/ignore, restarting/API/query/count, cancellation/replacement/shutdown, zero/exhausted intensity, forged tags and group abort/retry transitions are verified.85 MFAs, delta0; supervisor9/23 and gen_server10/41 partial API presence,219 namespace types/no violations. Consolidated notices package-checked in part089; license draft remains excluded, no banners/new dependency/production BEAM.

Initial 9dec6df original run completed 1332/1333 with one wrong-tag mismatch and no infrastructure failure; the 13 other retry cases matched. The source-directed correction was made before that run completed. [Initial raw history](validation/part-092/initial-9dec6df/checkpoint.json) retains the failed report and same-head1526 CI separately; it is not final compatibility proof. Current local package notices match exact consolidated files and exclude the draft: [audit](validation/part-092/package-notices.json).

Approximately5% coarse overall; full modules/upstream suites/performance remain unfinished. Next harden dead/nonlinked PID and shutdown/signal races, intensity/window/clock phase boundaries, then simple_one_for_one/significant/auto_shutdown and sys/proc_lib/aliases/options/upgrades/distribution/hibernate/stacks/resources. Group/scheduler/resource/clock equivalence is not established beyond the selected fixtures.

## Part 093 — Supervisor PID ownership and monitored shutdown (2026-10-11)

Erlang child callbacks now own their links and can return an already terminated PID, matching pinned supervisor.erl. PID identity stays in child queries and running/duplicate checks; only active counts require liveness. Shutdown monitors the child before signalling, receives the matching DOWN, escalates finite timeouts to kill, then unlinks and flushes queued EXIT. Selective shutdown preserves queued management calls. C# host startup convenience retains its live-handle/autolink contract.

Local **1541/1541 and full pipeline** pass: [tests](validation/part-093/tests.json), [integration](validation/part-093/integration-results.json). [15 independently expected modules](validation/part-093/contract-matrix.json) cover static/dynamic/manual/automatic dead PID returns, linked normal completion, detached/unlinked children, survival after supervisor death, infinite/finite/brutal shutdown, shared PID EXIT flush and queued calls. [Source audit](validation/part-093/reference-audit.json), [corpus preservation](validation/part-093/corpus-audit.json). All prior778/476/79 source/expectation/generated hashes unchanged. Planned original comparison **1348=778/491/79**; pending until part094. Latest completed original proof remains1333 atc20e990/part092.

85 registered MFAs unchanged, supervisor9/23 and gen_server10/41 remain partial API presence.220 namespace types/no layout violations. Consolidated notices checked in both packages; draft excluded/no banners/new dependency. Full shutdown logging/reason classification, all signal/timing races, sys/proc_lib/significant/simple_one_for_one/upgrades/stacks/resources/upstream suites/performance unfinished; approximately5% coarse overall.

## Part 094 — Exact-source supervisor lifecycle verification (2026-10-11)

Implementation **9ef47eda2043529304b0e0af9f8ffb164f3c177f** passes **1348/1348=778 expressions+491 compiled modules+79 diagnostics** against exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. [Raw proof](validation/part-094/differential.json), [provenance](validation/part-094/remote-checkpoint.json), [preservation](validation/part-094/corpus-audit.json). Same-head CI **1541/1541 and full pipeline**, test names match local part093 exactly. All prior778/476/79 sources/expectations/generated hashes unchanged;15 independently expected lifecycle modules match with zero expectation corrections. Original build/version/completeness/tracks and workflow/job/artifact head guards verified; no mismatch, abort or infrastructure failure. This evidence-only part changes no executable code and reuses the completed local1541/full pipeline.

Selected dead PID start/return/query/count/running contracts, callback-owned links, detached/unlinked children, child survival after supervisor kill, infinite/timeout/brutal shutdown, queued EXIT flush with shared PID and selective request preservation are verified.85 MFAs/delta0, supervisor9/23 and gen_server10/41 remain partial API presence. Supervisor89 related tests (+15), gen_server32; these are overlapping contract counts, not semantic percentages.220 namespace types/no violations. Consolidated notices package-checked, draft excluded; no banners/new dependency/production BEAM.

Full shutdown logging/reason classification, signal/priority/distribution/timing races, intensity clocks/sys/proc_lib/simple_one_for_one/significant/auto_shutdown/options/aliases/upgrades/hibernate/stacks/resources/upstream suites/performance remain unfinished. Approximately5% coarse overall. Next extend supervisor strategies/flags, keeping lifecycle/timing hardening separate from full compatibility claims.

## Part 095 — Significant children and automatic shutdown (2026-10-11)

Support significant transient/temporary children, never/any_significant/all_significant and supervisor:check_childspecs/2. Standalone check_childspecs/1 uses undefined policy; startup and dynamic calls validate against their configured policy. Preserve source validation/error precedence, significant=true in failed-start child records, last remaining significant PID/restarting bookkeeping, manual termination/ignore versus unrestarted natural exits, and ordinary group restart behavior. Public positional record constructors stay unchanged via init properties. Sources: pinned supervisor.erl and selected supervisor_SUITE significant routines; reuse existing C# monitor/mailbox/immutable terms, no new dependency.

Local **1572/1572 and full pipeline**,86 MFAs (+1), supervisor10/23 and gen_server10/41 partial API presence.222 namespace types/no violations. [30 independent modules](validation/part-095/contract-matrix.json), [source/reuse audit](validation/part-095/reference-audit.json), [preservation](validation/part-095/corpus-audit.json), [tests](validation/part-095/tests.json), [integration](validation/part-095/integration-results.json). All prior778 expressions/491 modules/79 diagnostics unchanged; plan **1378=778/521/79**, new original comparison pending. Initial local21/30 probe had9 fixture-only undef failures; helper/module/predicate inputs corrected before original dispatch with unchanged expectations, logs retained. Final30/30 passes. Consolidated notices verified in both packages; license draft excluded; no banners/production BEAM.

Full simple_one_for_one/sys/upgrades/logging/clocks/races/distribution/stacks/resources/upstream suites/performance unfinished. [Early platform milestone](TEN_PERCENT_MILESTONE.md) records the requested target10%; earlier approximately5% remains a coarse estimate until broader capabilities are delivered. No percentage promotion from test counts. Publish implementation, complete exact-source/same-head CI and preserve part096 proof, then continue the target milestone.

Part096: check_childspecs/2 currently validates proper-list tail too early; original is_list accepts improper cons cells and invalid policy takes precedence. Native1377/1378 retained; correction pending097. [Checkpoint](validation/part-096/checkpoint.json).


## Part 097 — Improper childspec list validation correction (2026-10-11)

Pinned check_childspecs/2 is_list accepts cons cells with improper tails, then validates policy. C# previously required a proper list first. Correct this guard/order and preserve check_startspec/4: invalid head returns its error, valid improper prefix reaches a missing clause and raises function_clause, which must escape the public checker. [Source audit](validation/part-097/reference-audit.json), [explicit provisional source assertion correction](validation/part-097/source-correction.json). One new invalid-policy fixture assertion changes from badarg improper-list to badarg wrong, following retained original096 and source inspection; external FixtureExpected done remains unchanged. All prior778/491/79 contracts and29 other new significant sources/expectations/generated hashes unchanged. Initial native1377/1378 stays immutable096; no claim it passed.

Local **1574/1574 and full pipeline**;32 significant modules, two additional improper-head/prefix regressions. [Tests](validation/part-097/tests.json), [integration](validation/part-097/integration-results.json), [matrix](validation/part-097/contract-matrix.json), [preservation](validation/part-097/corpus-audit.json). First build temporarily denied writing an MSBuild cache; retry with build-server reuse disabled passed, first log retained. Correction probe30/30 precedes the two added regressions, both pass in the full1574 harness.86 MFAs/delta0, supervisor10/23 and gen_server10/41 partial;222 namespace types/no violations. Consolidated package notices verified, draft excluded/no banners/new dependency.

Plan **1380=778/523/79** against original/head CI; pending until098. Full startup malformed-tail stacks/init error frames, simple_one_for_one/sys/upgrades/logging/clocks/races/distribution/resources/upstream suites/performance unfinished. Requested10% milestone remains active; coarse5% is not promoted. Publish this correction, complete098 proof, then apply prepared SimpleSupervisor/public query/stop implementation as099 and continue wider platform work.

## Part 098 — Significant supervision verified against original OTP (2026-10-11)

**Status:** selected increment and reference checks complete. Implementation **4992a985f692c45315205a76ad44ef1c65e2a01f** passes **1380/1380=778 expressions+523 compiled modules+79 diagnostics** against exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. [Raw proof](validation/part-098/differential.json), [provenance](validation/part-098/remote-checkpoint.json), [preservation](validation/part-098/corpus-audit.json). Same-head **1574/1574 and full CI**, local part097 test names identical. Exact-source build/version/completeness/track counts/workflow/job/artifact/head verified; zero final mismatch, abort or infrastructure failure; one provisional source assertion corrected from retained096 and pinned source. All prior778/491/79 sources/expectations/generated hashes unchanged;32 new independent significant contracts match. Evidence-only part; no local full-pipeline repeat or executable delta.

Verified never/any_significant/all_significant, standalone undefined versus startup policy, significant transient/temporary validation/error order, failed-start records, normal/shutdown/abnormal outcomes, last significant child, inactive ignore, manual/dynamic control and group restart boundaries.86 MFAs (+1 in part095), supervisor10/23 and gen_server10/41 partial API presence; supervisor122 related tests (+33), gen_server32.222 namespace types/no violations. [Source/reuse audit](validation/part-098/reference-audit.json), [contracts](validation/part-098/contract-matrix.json). Initial local9 fixture-only undef failures retained in part095; input helpers corrected before reference dispatch, expectations unchanged. Consolidated notices package-checked; draft uncommitted/unpublished/unpackaged, no banners/new dependency/production BEAM.

Full simple_one_for_one/sys/upgrades/logging/clocks/races/distribution/stacks/resources/upstream suites/performance unfinished. [Requested early platform milestone](TEN_PERCENT_MILESTONE.md) remains in progress toward10%; the coarse5% estimate is not promoted by test/API counts. Continue simple_one_for_one and wider collections/timers/source/application capabilities. Commit proof, then continue implementation; active0014 remains bounded and previous entries unchanged.

## Part 099 — Simple supervisor strategies and public queries (2026-10-11)

Implement `simple_one_for_one`: one validated template starts no initial process; `start_child` appends arguments, PID/restarting identities retain arguments across retry, and permanent/transient/temporary policies preserve source outcomes. Dynamic simple shutdown monitors/signals all children before waiting, with one shared timeout; ordinary ordered shutdown is preserved. Add `which_child/2`, `get_childspec/2`, `stop/1,3`; temporary ordinary dynamic specs erase stored start arguments as source `save_child` does. Shared callback normalization extracted from existing ErlangSupervisor. Source/reuse/limits are recorded in [audit](validation/part-099/reference-audit.json) and [dependency decisions](dependency-decisions.md). No new dependency or production BEAM; consolidated notices packaged and draft license excluded.

Local **1614/1614 and full pipeline**, **90 MFAs (+4)**, supervisor **14/23**, gen_server **10/41**, all partial API presence. **224 namespace types/no violations**. [35 independent modules](validation/part-099/contract-matrix.json), [preservation](validation/part-099/corpus-audit.json), [tests](validation/part-099/tests.json), [integration](validation/part-099/integration-results.json). All prior **778/523/79** sources/expectations/generated hashes unchanged; next exact-source corpus **1415=778/558/79**. New original comparison and same-head CI pending. Historical1380/1574 proof belongs to4992a98/part098. Isolated draft35/35 and124/124 probes are preparation evidence. Initial full run1613/1614 failed because enum enumeration reused the three-child ordinary host test for the new single-template strategy; preserve that failure and restrict the old test matrix to its three intended strategies, adding explicit host invalid-template rejection. Final run includes that new host contract,35 modules and4 direct-MFA contracts.

Full supervisor/sys/upgrades/logging/clocks/races/distribution/stacks/resources/upstream suites/performance remain unfinished. C# host simple configuration requires an Erlang MFA template; generic CLR argument factories are outside this increment. PID-map query order is unspecified and no BEAM ordering claim is made. Target10% remains active; continue collections/timers/source/application delivery after original verification, without percentage promotion from test counts.

## Part 100 — Simple supervision verified against original OTP (2026-10-11)

**Status:** selected increment and reference checks complete. Implementation **0d6a2334c17b918a2edb7bbcc39acfb93a44a418** passes **1415/1415=778 expressions+558 compiled modules+79 diagnostics** against exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. [Raw comparison](validation/part-100/differential.json), [provenance](validation/part-100/remote-checkpoint.json), [preservation](validation/part-100/corpus-audit.json). Same-head **1614/1614 and full CI**, all local/CI names identical. Exact-source build/version/completeness/track counts/workflow/job/artifact/head verified; zero mismatch, abort, infrastructure failure or expectation correction. All prior778/523/79 source/expected/generated hashes preserved,35 independent simple modules match. Evidence-only part; no repeat local full pipeline or executable delta.

Selected ordinary/simple/significant supervision contracts now cover four strategies, appended arguments and retry state, PID/dead/unknown control distinctions, normalized child specs, public query/stop and concurrent simple shutdown versus ordinary ordered cleanup. **90 MFAs**, supervisor **14/23**, gen_server **10/41**, all partial API presence; **162/32 related tests**, **224 namespace types/no violations**. [Source/reuse audit](validation/part-100/reference-audit.json), [contracts](validation/part-100/contract-matrix.json). Initial local enum test-matrix failure remains099 history; no source fixture expectations changed. Consolidated notices verified in packages; draft license uncommitted/unpublished/unpackaged, no banners/new dependencies/production BEAM.

Full sys/proc_lib/options/upgrades/logging/distribution/stack/resource/timing/race/upstream-suite/performance contracts remain unfinished. Target10% remains pending until collections/timers/source/application packages satisfy [milestone](TEN_PERCENT_MILESTONE.md). Continue prepared101 maps/common-list functional APIs and runtime timers, then source/application state and runnable sample. Commit proof and continue implementation.

## Part 101 — Functional collections, timers, records and platform example (2026-10-11)

**Local increment complete; independent original verification pending.** Add 24 maps exports, 11 lists exports and four Erlang timer exports. Implement tuple record definitions, construction/access/update/index, wildcard and partial patterns, guard checks and record_info. The generated `platform_demo.erl` application combines supervisor, gen_server state in records, timer delivery and functional collections. Both the hybrid example and the installed local PackageReference consumer verify its `{platform_ready,100,[2,3]}` result without production BEAM.

Full local pipeline passes **1774/1774**, including **129 direct-MFA contracts (+39)**, style, incremental/clean builds, disabled-preprocessing failure, local CLI, package readiness and package consumer. **248 namespace types**, no layout violations. [Tests](validation/part-101/tests.json), [integration](validation/part-101/integration-results.json), [116 new module contracts](validation/part-101/contract-matrix.json), [source/reuse audit](validation/part-101/reference-audit.json), [preservation](validation/part-101/corpus-audit.json), [package notices](validation/part-101/package-notices.json).

All prior **778 expressions / 558 modules / 79 diagnostics** preserve sources, independent expectations and generated hashes. The next pinned corpus is **1536 = 778 expressions + 674 modules + 84 diagnostics**. New original comparison and same-head CI are pending. Historical **1415/1415 and 1614/full CI** belong to implementation0d6a233/part100. Initial isolated map registration omission and record guard `orelse` failure are retained; implementation/input setup corrected before promotion, independent expectations unchanged. All 558 prior modules also passed the isolated combined compiler/runtime probe.

Map callbacks preserve error precedence, exact keys and ordered iterator behavior; default traversal remains unspecified. Lists preserve initial versus recursive errors, left/right evaluation order and short circuit. Timers survive creator death, cancel for dead PID recipients, resolve names at delivery and do not flush delivered messages. Record update values precede base evaluation; strict record guard checks respect boolean operand boundaries. These are selected partial contracts, not full-module completion or a performance comparison. Typed/native/imported records, macros/includes, full lint/spans/stacks/resources, timer options/absolute epochs/ranges/races, sys/upgrades/distribution and full upstream suites remain unfinished.

The requested approximately 10% early platform milestone has all planned capability packages locally delivered. Keep that target pending until the new original and same-head checks are complete; do not derive the percentage from test/API counts. Continue exact-source verification and preserve part102, fixing any mismatch against pinned source without weakening independent expectations.

## Part 103 — 2026-10-11 — Correct strict record guards and isolate pinned map optimizer limitation

The complete failed original comparison from part 102 is retained unchanged. Source audit: OTP-29.1.1 erl_expand_records bool_operand/strict_record_access/conj injects strict guard checks that fail the entire alternative on a wrong record, including a left operand before orelse. Internal C# RecordTest now raises badarg on wrong tag/arity; Guard catches it and rejects that alternative. A true short-circuit still avoids unevaluated accesses. Exactly one newly introduced provisional fixture expectation is corrected to error/badmatch/no; its source is unchanged. This is a transparent expectation correction, not a zero-correction claim.

Pinned beam_call_types describes maps:map arguments as map-only although maps.erl accepts iterators. In part 102, optimized native compilation returns the first map result and drops the remaining run body for two statically non-map iterator sources. Two new fixtures explicitly include -compile(no_ssa_opt); their independent done outcomes remain unchanged. Parser accepts only that compile atom as a backend-neutral declaration; other compile options remain unsupported. Original optimized failures are preserved, not rewritten. Full optimized compiler parity is not claimed for these cases; repeat original verification must confirm the explicit directive. No maps runtime behaviour is changed to reproduce the native optimizer artifact.

Validation: 1774/1774 and full local pipeline; 129 direct MFA contracts; prior 778 expressions/558 module sources, independent expectations and generated hashes/79 diagnostics preserved; current674 modules/84 diagnostics. 249 namespace types, zero filename/multiple-type violations. Both packages contain byte-exact consolidated notices; draft license excluded. [Local report](validation/part-103/tests.json), [audit](validation/part-103/reference-audit.json), [corpus](validation/part-103/corpus-audit.json). No new dependency or production BEAM. Closed0014 and uncommitted LICENSE.local.txt untouched. Target approximately 10% remains pending until exact-source and same-head CI checkpoint104.

## Part 104 — Early functional platform milestone verified (2026-10-11)

**Selected milestone complete.** Published implementation **d713491b52da2bcdd0827f02133157730e917059** passes **1536/1536 = 778 expressions + 674 compiled modules + 84 compiler diagnostics** against exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. [Raw comparison](validation/part-104/differential.json), [provenance](validation/part-104/remote-checkpoint.json), [preservation](validation/part-104/corpus-audit.json). Same implementation head passes **1774/1774 and full CI**, including hybrid/preprocessing/incremental/clean/package consumer/local CLI/readiness. Local and CI test names are identical. Source build, version, complete/planned/executed counts, all tracks, jobs, artifacts and SHA256 provenance checked. Zero mismatch, infrastructure failure or abort. One provisional record expectation was corrected from source; the initial three-difference report is retained in part 102. Two new map iterator cases explicitly disable native SSA optimization due to the pinned compiler map-only type assumption. Prior 778/558/79 sources/expectations/generated hashes unchanged; 116 new independently expected modules and five diagnostics match.

The five planned capability packages are delivered and independently checked: ordinary/simple/significant supervision and lifecycle; functional map/list APIs; relative Erlang timers and recipient lifecycle; tuple records and selected compiler diagnostics; runnable supervised source application through generated hybrid and installed local package. Overall completion is reassessed at **approximately 10%**, a coarse engineering milestone estimate, not measured OTP semantic compatibility, API coverage or performance. The earlier approximately 5% was similarly an estimate; there is no reliable weighted whole-assignment denominator. [Milestone criteria and evidence](TEN_PERCENT_MILESTONE.md) explain the reassessment.

**129 directly tested registered MFAs**: maps 29/34, lists 31/91, erlang 44/351, supervisor 14/23, gen_server 10/41; these are partial API-presence counts, not semantic completion percentages. **249 namespace types/no violations.** [Module readiness](MODULE_READINESS.md), [source/reuse audit](validation/part-104/reference-audit.json), [new contracts](validation/part-104/contract-matrix.json). Initial draft failures remain immutable part 101 history; the complete 1533/1536 original failure report is retained in part 102. One provisional record expectation was corrected and two newly introduced map sources explicitly disable SSA optimization; earlier corpus expectations remain unchanged. Evidence-only part 104 reruns no local full pipeline and changes no executable code. Exact consolidated notices were verified in both local packages; no new dependency, production BEAM or source license banners. LICENSE.local.txt remains excluded/uncommitted/unpublished/unpackaged.

The full work.md assignment and all OTP modules remain unfinished. Next scope includes typed/native/imported records and macros/includes, full diagnostic spans/lint/warnings, timer options/absolute epochs/ranges, remaining standard library/OTP services, distribution/ETS/ports, sys/proc_lib/upgrades, optimizer/debugger/resource/signal/clock races and full upstream suites. Relative speed versus BEAM is unmeasured; no faster-than-OTP claim. Continue subsequent increments from these verified sources and preserve the independent corpus.

## Part 105 — 2026-10-11 — Complete registered maps API and iterator list conversion

**Part status:** implementation complete; independent OTP/same-head CI verification pending 106. **Objective:** implement the five remaining pinned maps exports and audit public functional contracts beyond API presence.

### Changes and decisions

- Add intersect/2, intersect_with/3, merge_with/3, groups_from_list/2,3: maps 29/34 becomes 34/34, total 129 MFAs becomes 134. Use smaller-map traversal with original left/right callback orientation, callback-arity-before-badmap validation and immutable exact-key results. Reuse dictionary builders and create/sort the final MapTerm once rather than reconstructing a map per entry; no performance superiority claim.
- Grouping validates the complete proper list before invoking callbacks, then invokes key/value callbacks right-to-left, key before value, while retaining original order within result groups. Callback throw/exit/error propagates unchanged. Existing to_list/1 now accepts iterators and none, preserves ordered/reversed/custom traversal and wraps malformed continuation errors as badmap with the original argument.
- Source audit before implementation: pinned maps.erl and maps_SUITE combiners/grouping/iterator/ordering/errors; reuse existing MapTerm, FunctionTerm, Cons, MapIteration and BCL Dictionary. No new dependency or production BEAM. Applicable consolidated Ericsson Apache notices remain exact in both packages; no source license banners; LICENSE.local.txt remains excluded/uncommitted/unpublished/unpackaged. [Source/reuse audit](validation/part-105/reference-audit.json).
- Add 85 independent compiled modules: all five API contracts, both size orientations, exact numeric/composite keys, zero calls for disjoint/empty maps, callback class/reason and side effects, proper-list validation, numeric key rank, iterator conversion/invalid continuations, 256-element iteration consistency and grouped data, 128/192-key combiners and errors of existing API. All 85 use default native optimization; the two historical no_ssa_opt sources are untouched. All prior 778/674/84 sources, expectations and generated hashes remain unchanged; no new expectation correction. [Corpus](validation/part-105/corpus-audit.json), [contracts](validation/part-105/contract-matrix.json).

### Validation

| Check | Command/report | Result |
| --- | --- | --- |
| Build/layout | dotnet build -m:1 --disable-build-servers; Erlang.Style --write and full --check | Passed;250 namespace types/no filename or multiple-type violations |
| New compiled source cases | maps105-probe; [report](validation/part-105/maps-probe.log) | 85/85 |
| Historical preservation | refactor-audit; [report](validation/part-105/corpus-audit.json) | All 778/674/84 preserved |
| Full product validation | pwsh -File tools/validate.ps1; [tests](validation/part-105/tests.json), [integration](validation/part-105/integration-results.json), [log](validation/part-105/validation.log) | 1864/1864; 134 direct-MFA contracts; hybrid, negative preprocessing/oracle, incremental/clean/package consumer/local CLI/readiness passed |
| Package notices | package-notice-audit -Part105; [report](validation/part-105/package-notices.json) | Two packages exact; draft excluded |
| Original and remote CI | Pinned comparison 1621=778 expressions/759 compiled modules/84 diagnostics | Pending new implementation head; historical 1536/1774 proof remains part104 at d713491 |

### Scope and handoff

All 34 exports are registered and directly tested; this is completed public API presence, not proof of native HAMT layout, stack/error_info/resource/reduction limits or full Common Test/performance parity. Those remain explicit runtime/compiler boundaries. Overall approximately 10% engineering estimate stays unchanged. Publish the validated implementation, compare the complete corpus and same-head 1864/full CI, then preserve independent proof in106 before declaring original validation complete. Detailed journal stays in active 0015; closed volumes and reference checkout untouched.

## Part 106 — 2026-10-11 — Original maps comparison exposes iterator batching mismatch

**Part status: completed evidence; maps semantic completion remains blocked by one mismatch.** Original OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 was built successfully and ran the complete corpus at implementation 6aa1122a38b89e4bb7696f05c61c819e2394858f: **1620/1621 passed**, no infrastructure error or abort. Same-head CI passes **1864/1864 and full integration**. All prior 778 expressions/674 compiled module sources, independent expectations/generated hashes/84 diagnostics are preserved. Of 85 new maps modules, 84 match; maps-completion-iterator-valid-first-only fails with original {error,error,{badmatch,false}}, CLR {ok,done}, provisional independent {ok,done}.

[Raw comparison](validation/part-106/differential.json), [mismatch](validation/part-106/mismatches.json), [provenance](validation/part-106/remote-checkpoint.json). Pinned maps.erl is_iterator_valid delegates to next. Inspection of erts/emulator/beam/erl_map.c:erts_internal_map_next_3 shows ordered iteration builds a tuple chain and validates keys before returning, rather than reading just the first key. The provisional scenario incorrectly required true for [[a,missing]|#{a=>1}]. Preserve this failed source and outcome; correct the source-directed contract in the next part, fix CLR iterator batching, and rerun the entire original comparison. No expectation is replaced by the implementation's observed value.

No runtime modification in this evidence part. API presence remains 34/34; verified original semantic checkpoint remains immutable part104. English journal stays bounded; closed journals/sibling checkout untouched. LICENSE.local.txt remains uncommitted/unpublished/unpackaged. No BEAM performance or complete internal/resource/error_info/stack/Common Test claim.

## Part 107 — 2026-10-11 — Port maps iterator tuple-chain validation

**Local increment complete; original verification pending.** Pinned maps.erl:next/is_iterator_valid and erts/emulator/beam/erl_map.c:erts_internal_map_next_3 resolve ordered keys into a tuple chain before returning. CLR next now validates keys/proper tails, builds that immutable chain and propagates badarg before callbacks. Existing exact-key lookup, Cons and tuple terms are reused; no external dependency. Native reduction-dependent batch boundaries and HAMT integer paths remain internal/resource limitations.

Part106 permanently retains the original 1620/1621 failure and its provisional source. Correct one new source contract: rename iterator-valid-first-only to iterator-valid-missing-later-key, change the true assertion to false based on original source/native {badmatch,false}; independent final {ok,done} remains unchanged. Add five independent cases: ordered/reversed tuple chains, missing later key, improper later tail, and foreach rejecting the malformed batch before invoking a callback. Preserve all 84 other new maps fixtures and all historical 778 expressions/674 module sources/independent expectations/generated hashes/84 diagnostics.

Full local pipeline passes **1869/1869**, **134 direct MFAs**, hybrid/negative preprocessing/negative oracle/incremental/clean builds/local PackageReference consumer/CLI/readiness. **250 namespace types**, no filename/multiple-type violations. Consolidated notices exactly match both packages; LICENSE.local.txt excluded. [Tests](validation/part-107/tests.json), [integration](validation/part-107/integration-results.json), [90 maps cases](validation/part-107/contract-matrix.json), [source/correction audit](validation/part-107/reference-audit.json), [preservation](validation/part-107/corpus-audit.json), [package notices](validation/part-107/package-notices.json).

The next original corpus is **1626 = 778 expressions + 764 compiled modules + 84 diagnostics**. Original part108 and same-head CI are pending; no semantic promotion from local tests. All 90 new maps modules use default native optimizations; two historical explicit no_ssa_opt fixtures remain unchanged. API presence stays 34/34; full maps internal/resource/stack/error_info/Common Test/performance parity remains unclaimed. Overall approximately 10% estimate unchanged. Publish the correction and then verify the entire original corpus; closed journals/sibling checkout and uncommitted license draft preserved.

## Part 108 — 2026-10-11 — Complete maps API verified against original OTP

**Part status: completed.** Implementation **6363039e77a5eb1a15c2ea06b30f77e2fb267023** delivers all **34/34 pinned maps exports**, including the five missing exports and iterator-aware to_list. Exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 comparison passes **1626/1626 = 778 expressions + 764 independently compiled modules + 84 diagnostics**; **90 new maps modules** match independent expectations. The same implementation head passes **1869/1869 and full CI**, with identical local/CI test names and **134 directly tested MFAs**. Source build, version, completeness/counts/tracks, jobs/artifacts/heads and archive SHA256 are checked. Zero mismatch, infrastructure failure or abort. Part106 retains the initial 1620/1621 mismatch; part107 corrected one provisional source assertion (true to false) from pinned map_next batching, leaving its final independent {ok,done} result unchanged. All prior 778/674/84 sources, expectations and generated hashes are preserved. All 90 new cases use default original optimization; the two historical explicit no_ssa_opt fixtures remain unchanged.

[Raw comparison](validation/part-108/differential.json), [strict provenance](validation/part-108/remote-checkpoint.json), [preservation](validation/part-108/corpus-audit.json), [maps contracts](validation/part-108/contract-matrix.json), [module scope](MAPS_COMPATIBILITY.md). Local product validation belongs to immutable part 107, the same executable sources; this evidence-only part changes no executable code and does not repeat the full local pipeline. Hybrid, negative preprocessing/oracle checks, incremental/clean builds, local package consumer/CLI/readiness are confirmed by same-head CI. 250 namespace types/no violations; consolidated package notices exact and draft license excluded. No new dependency, production BEAM, source banners or public NuGet publication. During the wait, local main/origin-main ref files were found to contain 41 zero bytes. Remote ls-remote, retained reflog and the commit object independently confirmed 6363039e77a5eb1a15c2ea06b30f77e2fb267023; the two ref files alone were restored to that SHA, corrupted bytes retained under ignored artifacts, working tree/index unchanged. Cause is unconfirmed.

Completed functional API contracts include smaller-map traversal with original callback argument orientation; exact keys and input immutability; callback arity/error precedence, propagation and no calls for absent common keys; complete-list validation before grouping, right-to-left key/value callbacks and stable group order; map/iterator/none list conversion, malformed continuation wrapping, numeric key ordering and traversal consistency. This is **complete exported API presence and verified functional scope**, not a claim that all native HAMT/opaque iterator layout/stack/error_info/resource/reduction/internal Common Test/performance contracts are reproduced. Those boundaries stay explicit and module status remains Partially compatible. Overall approximately 10% engineering estimate is unchanged; no BEAM speed claim.

### Validation and handoff

- Commands: reference-checkpoint-108 -Mode Download; preserve108 strict source/independent-outcome/hash/head/CI guard; readiness CLI with immutable part 107 tests; Git diff/closed-volume/reference guards.
- Historical 1536/1774 proof at d713491 remains immutable part104. One provisional iterator source assertion correction retained in part106/107; no new optimization workaround; prior map compiler caveat retained in part102/103/104.
- Publish verified maps proof, bounded English journal and refreshed readiness. Next work: remaining lists/OTP/source features; map internal resources/error_info/stacks require runtime/compiler support. Closed journals and sibling checkout remain untouched; LICENSE.local.txt stays uncommitted/unpublished/unpackaged.

## Part 109 — 2026-10-11 — Complete lists exported API and source-directed contracts

Implement all **91/91 lists exports**, adding **60 registered MFAs** (194 total) and **252 independent compiled-module contracts**. Before implementation, audit pinned OTP-29.1.1 lists.erl and lists_SUITE.erl, existing terms/comparison/callback infrastructure and BCL reuse; record source/reuse choices in OTP-PORTS and dependency-decisions. No new dependency or source license banners.

Port basic/key operations, concat legacy float formatting, zip/zip3/zipwith/zipwith3 fail/trim/pad modes, unzip, filter/filtermap alias, search/splitwith/flatmap/uniq, stable sort/keysort and unique/reverse/key/callback merges. Preserve exact versus numeric equality, first representatives, unchanged tails, callback directions/order, malformed arguments and fault precedence. Callback unique sorting and merging follow original state transitions; pure sort uses cached stable BCL projection. Fix existing map/sum tail and callback contracts. Separate semantic constants/types and apply repository layout/style rules.

Add direct exported-function contracts, literal callback traces, badfun/badarity/throw/exit, improper tails, numeric/composite keys, padding modes and 64 deterministic seeded sort/merge vectors. Expected values are independent literal/frequency contracts, never copied from execution. Preserve all historical 778 expressions, 764 module sources/expectations/generated hashes and 84 diagnostics. New modules use default native optimization; two historical explicit no_ssa_opt map fixtures stay unchanged.

Retain initial failures: first isolated172 probe passed169; correct two provisional source contracts (prefix remainder guard and zip3 fixed padding), plus one invalid map-pattern fixture syntax; final independent {ok,done} remains unchanged. Source audit then finds keysort first-key versus tail priority and lazy mergel/umergel nested versus outer-tail priority; retain two failing regression pairs and fix implementation. Also retain intermediate tuple projection build failure. Filtered helper Total labels count corpus size, not executed subsets: keysort/mergel probes each execute two cases. Earlier full passes2177 and2179 precede final corrections; final evidence supersedes them without erasing logs.

Final local pipeline **2181/2181**, all252 new modules and194 direct MFAs; hybrid, negative preprocessing/oracle, incremental/clean builds, package consumer, CLI and readiness pass. **263 namespace types**, no file/type violations; exact consolidated notices in both packages and uncommitted license draft excluded. [Tests](validation/part-109/tests.json), [integration](validation/part-109/integration-results.json), [contracts](validation/part-109/contract-matrix.json), [source/failure audit](validation/part-109/reference-audit.json), [corpus preservation](validation/part-109/corpus-audit.json), [notices](validation/part-109/package-notices.json).

Next pinned original corpus **1878 = 778 expressions + 1016 modules + 84 diagnostics**, with same-head full CI, pending part110. API presence is complete; functional promotion waits for original proof. Native allocation/reductions/stack/error_info, exhaustive Common Test and performance parity remain unclaimed. Latest complete original remains part108 (1626/1626); other modules remain partial and overall approximate10% engineering estimate is unchanged. Publish this increment and continue through original verification.

## Part 110 — 2026-10-11 — Complete lists API verified against original OTP

**Part status: completed.** Implementation **9aeb8f1c6943dad8bbba22689e3de95e48a6d338** delivers **91/91 pinned lists exports**, including 60 newly registered MFAs and 252 independently expected compiled-module contracts. Exact-source OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 comparison passes **1878/1878 = 778 expressions + 1016 compiled modules + 84 diagnostics**. Same-head CI passes **2181/2181 and full integration**, with identical local/CI test names and 194 direct MFAs. Source build, version/completeness/counts/tracks, all independent expected outcomes, historical hashes, workflow/job/artifact/head and archive SHA256 are checked. Zero mismatch/infrastructure failure/abort.

All prior 778 expressions/764 module sources, expected outcomes and generated hashes/84 diagnostics remain unchanged. All 252 new modules use default native optimization; two historical explicit no_ssa_opt maps cases stay unchanged. Part109 preserves initial169/172 provisional failures, two source-contract corrections plus one fixture syntax correction made before original verification, and failing keysort/mergel regression pairs before source-directed implementation fixes. Independent final {ok,done} expectations remain unchanged; no expected values are copied from native results.

[Raw original](validation/part-110/differential.json), [strict provenance](validation/part-110/remote-checkpoint.json), [preservation](validation/part-110/corpus-audit.json), [252 contracts](validation/part-110/contract-matrix.json), [source/failure audit](validation/part-110/reference-audit.json), [lists scope](LISTS_COMPATIBILITY.md). Local full validation is immutable part 109 with the same executable sources. Hybrid/negative preprocessing/negative unavailable-oracle/incremental/clean builds/package consumer/local CLI/readiness pass same-head CI. 263 namespace types/no violations; consolidated package notices match exactly and LICENSE.local.txt remains uncommitted/unpublished/unpackaged. No new dependency, production BEAM, source banners or public NuGet publication.

Verified functional scope includes exact versus numeric equality; stable first representatives and numeric/composite keys; empty/short/improper lists; unchanged early tails; fail/trim/pad zip modes; callback order/directions and error/throw/exit propagation; legacy float concat; deterministic sorting/merge vectors and nested fault precedence. All exported API presence and this functional scope are complete. Native allocations/reductions/resource ceilings, full stack/error_info, exhaustive upstream Common Test and performance parity remain separate runtime/compiler work; module status stays Partially compatible. Other modules remain partial and approximately 10% overall engineering estimate is unchanged. This evidence-only increment changes no executable sources and needs no duplicate full local run.

Commands: reference-checkpoint-110 -Mode Download; preserve110 strict original/source/hash/head/CI guard; readiness CLI with immutable part 109 tests; Git diff, closed-volume and sibling guards. Local product pipeline was pwsh -File tools/validate.ps1 in part 109; evidence-only part 110 does not rerun it.

Publish original proof, bounded English journal and 91-function report. Preserve closed journals, sibling checkout and draft license. Continue broader OTP work separately; no measured BEAM speed claim.

## Part 111 — 2026-10-11 — Erlang basic BIF and registered operator implementation

**Local increment complete; whole erlang module remains unfinished.** Pinned union has351 explicit exports/BIF declarations; register61 missing MFAs, moving erlang44 to105/351 and total194 to255. Target every remaining export with actual runtime support and independent original contracts; do not count generic badarg/undef stubs. Source/reuse plan is recorded before implementation in OTP-PORTS and dependency-decisions. Audit erlang.erl, bif.tab, erl_bif_guard.c, erl_bif_op.c, bif.c, erl_arith.c and tuple arity definitions in erl_term.h.

Expose29 operator MFAs through shared runtime/compiler value semantics, preserving compiler exception stack wrappers. Add explicit numeric rounding/conversion, min/max first ties, function/boolean/reference/port/ranged-integer predicates, size floor-byte behavior, immutable tuple construction/update/insert/delete/conversions, append/subtract, send, apply/2-/3 and dictionary enumeration/erase. Add guard admission consistently. Native positive bignum arity gives false, invalid arity gives badarg; float overflow is badarg rather than arithmetic error; round uses AwayFromZero, including negative ties. make_tuple initialization validates proper lists and indexes, with later duplicate index winning. Exact dictionary values distinguish integer/float keys. Reuse existing terms/order/list helpers/callbacks/dispatch plus SDK BigInteger/Math/temporary arrays; no package dependency, source banners or production BEAM.

Add115 independently expected compiled modules, including61 direct exports, dynamic apply operator dispatch, numeric/composite ties, signed zero/large rounding/overflow, tuple input immutability/empty/index errors, guard execution, callback throw/badfun/badarity and list-validation precedence, dictionary exactness and named/self sending. All new native cases use default optimization. Initial isolated probe114/115: one fixture used reserved bsr/bsl atoms unquoted; retain failed log and correct source syntax. Separately correct one provisional ordered assertion before original: get_keys traversal is unspecified, so test exact membership/count instead of sorting equal integer/float keys and assuming initial order. Independent final {ok,done} is unchanged; historical fixtures are untouched.

Standalone and full local pipeline **2357/2357**,255 direct MFAs, hybrid/negative preprocessing/negative missing-oracle/incremental/clean builds/package consumer/CLI/readiness pass. **270 namespace types**, zero filename or multiple-type violations; exact consolidated notices in both packages, LICENSE.local.txt excluded. Source/expected/generated-hash audit preserves all778 expressions/1016 compiled modules/84 diagnostics. [Tests](validation/part-111/tests.json), [integration](validation/part-111/integration-results.json), [115 contracts](validation/part-111/contract-matrix.json), [source/correction audit](validation/part-111/reference-audit.json), [preservation](validation/part-111/corpus-audit.json), [notices](validation/part-111/package-notices.json).

Commands: dotnet build -m:1 --disable-build-servers; isolated development probe; SDK style --write; standalone harness; pwsh -File tools/validate.ps1; development corpus/layout/package-notice audits; readiness CLI. Original **1993 = 778 expressions + 1131 modules + 84 diagnostics** and same-head2357/full CI pending part112. Latest original remains1878/1878 at9aeb8f1 in immutable110; no promotion from local success. erlang remains105/351 API presence and Partially compatible; ports/distribution/native records/code/trace/host support and many ordinary BIFs still require implementation. Overall coarse10% estimate unchanged; no native performance claim.

Close0015 after110 and begin0016 to preserve32KiB/400line journal bounds. Publish this increment, finish original proof and continue erlang conversion/serialization/clock/process/runtime increments without stopping at this partial checkpoint. Closed journals and sibling checkout remain untouched; draft license uncommitted/unpublished/unpackaged.

## Part 112 — 2026-10-11 — Preserve failed/incomplete original Erlang basic comparison

**Checkpoint published; original verification unfinished and whole erlang goal remains active.** Implementation **5f0c5597d2477bde19c0bcb77af8040d337be625** passes same-head **2357/2357/full CI**, 255 direct MFAs, erlang105/351. Exact-source OTP build/version succeed, but comparison plans1993 and executes **1863:1862 pass,1 semantic mismatch**, then aborts on a native compile failure. Expressions **778/778**, compiled modules **1084/1085** of1131 planned, diagnostics **0/84 executed**. No complete-original claim; latest successful1878/1878 remains immutable110. Workflow/job/head/artifact SHA256 and full CI names are checked. All actual prior778 expressions/1016 completed module contracts and their source/generated hashes match110; diagnostic source preservation is only the local111 audit, not native execution in112.

Native apply/2 rejects the legacy {lists,reverse} callable with {badfun,{lists,reverse}}, while C# accepts it. Audit beam_common.c apply_fun/call_fun: argument-list validation happens first and any non-fun is EXC_BADFUN. The erlang.erl deprecated documentation still describes the historical tuple syntax; actual pinned runtime is authoritative. Fix the implementation to reject tuple callables, replace the invalid provisional success assertion with a source-derived rejection contract, and retain this raw mismatch. Keep apply/3 valid MFA dispatch. No silent adoption of results.

Native compilation then aborts because insert_element/3,delete_element/2,make_tuple/3 are called unqualified in the tuple-immutable fixture; erl_internal.erl excludes them from auto-import. Qualify all such tuple helper calls (including append_element and make_tuple/2 in subsequent cases) with erlang: after checking the full auto-import table. This is fixture-source repair, not relaxed compiler flags. All changes to111 fixture sources will be explicitly audited; historical110 fixtures remain unchanged. The independent final {ok,done} stays the same for the corrected full scenario, with rejection verified explicitly.

[Failed original](validation/part-112/differential.json), [provenance and diagnosis](validation/part-112/remote-checkpoint.json), [same-head CI](validation/part-112/ci-tests.json), [full integration](validation/part-112/ci-integration-results.json), [original source/correction audit](validation/part-112/reference-audit.json). Preserve111 initial source failures and immutable reports. Clarification:111 IntentionalCorrection prose carried a stale historical-module number; actual HistoricalCompiledModules1016 and audited rows are correct and unchanged. No new executable changes in112; no repeated full local run.270 namespace types, exact notices in two packages, draft excluded, no new dependency/banners/production BEAM/public NuGet.

Next113: fix apply semantics and tuple fixture qualifications; integrate the already ignored integer/binary/iolist/float-parser/checksum drafts and real parser string-versus-operator token-kind fix, with permanent independent cases and consolidated zlib notices before validation. Initial draft data probe121/126 had four real parser failures plus unsupported local fun reference; explicit fun wrapper fixes the latter. Expanded200/204 retains real string +/- failures, and token-boundary probes add broader regressions. Publish validated113 and repeat exact-source original plus same-head CI114. Whole module remains105/351 with246 exports and underlying conversion/time/process/native-record/code/ports/distribution/tracing/host work unfinished; native resource/stack/error_info/exhaustive CT/performance boundaries remain explicit. Coarse overall10% unchanged. User requested no stopping until erlang complete; active goal persists.

Commands: reference-checkpoint-112 Download; strict failed-report/earlier-corpus/same-head CI guard; pinned beam_common.c and erl_internal.erl source audit; readiness CLI with111 local test snapshot. Publish this evidence checkpoint and continue implementation immediately. Closed journals/sibling checkout/draft license unchanged;0016 active.

## Part 113 — 2026-10-11 — Erlang data BIFs and original-driven apply/parser/identity fixes

**Part status: locally complete; original verification pending114 and whole erlang goal active.** Add28 actual MFAs, moving erlang105 to **133/351**, registry255 to **283**. Reuse/source plan is recorded before production implementation. Port integer/list/binary conversions, binary/list/bitstring slicing, iodata flatten/size/iovec, strict float parsing, CRC-32/Adler-32 seeded/combine functions and MD5. Admit binary_part/2,3 in guards. Each export has a permanent direct contract; no generic error stubs.

Runtime files: ErlangDataBifNames/DataLimits/DataRegistrations, ErlangIntegerText/IoData/FloatText, ErlangChecksumNames/Parameters/Checksums. SDK BigInteger decimal output and12-digit balanced parse groups avoid per-digit growing bignum operations; all bases2–36 use uppercase output and accept signed mixed-case input. Iterative stack traversal handles nested iodata and binary improper tails, with bounded integer-byte batches rather than one object per byte. Distinguish plain binary from partial bits: binary_part requires alignment, split_binary preserves a partial-bit suffix, bitstring_to_list returns final bits as a list element; binary_to_list/3 indexes1..N inclusively. Iovec coalesces small segments and reuses large immutable binaries with source512-byte threshold; exact native storage-offset partitions are not claimed. Float parser requires decimal digits around dot/comma, optional exponent/sign, rejects overflow, preserves signed zero/underflow and native NUL termination after byte validation. Checksums consume segments, use uint32 seed/length bounds, zero-length Adler combine first input and CRC combine XOR semantics; SDK IncrementalHash owns MD5 state. No package or production BEAM dependency, no measured speed claim.

Fix native112 apply/2 mismatch: validate proper args before callable type and reject any non-fun with {badfun,Value}, including legacy {Module,Function}; apply/3 remains MFA dispatch. Audit beam_common.c apply_fun/call_fun rather than stale deprecation prose in erlang.erl. Correct the one provisional legacy-success assertion to explicit rejection, keeping final independent {ok,done}; add malformed tuple/list precedence regression. Repair9 other111 fixture sources with explicit erlang qualifications for non-auto-import tuple APIs per erl_internal.erl. [Exact before/after10-source correction ledger](validation/part-113/fixture-corrections.json) preserves original expectations/names and failed112 raw evidence. All110 historical fixtures stay untouched.

New string '+'/'-' conversion tests exposed real parser defects. Syntax matching, prefix/infix precedence and bit unary checks now require Keyword/Symbol tokens; literal strings and quoted atoms stay data, with explicit atom consumption for no_ssa_opt compile option preserving the two historical map fixtures. Add31 operator/keyword/delimiter string scenarios. Initial draft121/126 included four token defects and unsupported local fun reference; explicit predicate fun wrapper fixes that test scaffolding (function-reference language support remains future work). Expanded200/204 and token213/235 failures are retained. The unchanged5000-depth iolist scenario then crashed both isolated and initial full harness before iodata traversal: binding merges recursively compared the very same immutable object. Port native utils.c eq/erts_cmp_compound is_same shortcut as ReferenceEquals after current rank/type validation. Keep5000 depth and add20000-level direct term identity test; distinct-object ordering/equality paths remain unchanged, and arbitrary nonidentical deep recursion is still explicit.

**Validation:355/355 isolated corrected scenarios;2626/2626 full local tests and full integration.** Add240 independent compiled modules:239 data cases plus one extra tuple-callable rejection; total1371. Planned original **2233 = 778 expressions +1371 modules +84 diagnostics** uses default native optimization for new cases. Preserve historical778/1016/84 source/expected/generated hashes. **280 namespace types**, zero matching-file/multiple-type violations; exact updated notices in both packages. Source-guided checksum adaptations are explicitly altered C# implementations; full bundled zlib1.3.2 Gailly/Adler notice and per-source Mark Adler credits are consolidated in OTP-LICENSE/OTP-PORTS. No source banners; LICENSE.local.txt uncommitted/unpublished/unpackaged; no public NuGet publication.

[Full tests](validation/part-113/tests.json), [integration](validation/part-113/integration-results.json), [240 new contracts](validation/part-113/contract-matrix.json), [source/reuse/failure audit](validation/part-113/reference-audit.json), [preservation](validation/part-113/corpus-audit.json), [package notices](validation/part-113/package-notices.json), [deep crash excerpt](validation/part-113/deep-stack-failure.txt), [initial harness stdout](validation/part-113/initial-tests-stdout.log). Commands: explicit builds -m:1 --disable-build-servers; isolated draft/final probes; SDK style --write; pwsh -File tools/validate.ps1; corpus/layout/notice audits; readiness CLI. Initial full harness produced no final JSON due stack overflow; final full pipeline is the2626 proof. No duplicate final full run after success.

Publish113, dispatch exact-source original2233 and same-head2626/full CI114; keep executable sources frozen for proof and retain any mismatch before repair. Latest complete original remains1878/1878 at9aeb8f1 in110;112 is failed/incomplete and never promoted. **218 erlang exports still need real implementations/runtime support**: atom/existing table and float formatting, ETF/native identities/original hash, clocks/options, processes/aliases/spawn/inspection, native records/code lifecycle, ports/distribution/tracing/host state. Full error_info/stacks/resources/exhaustive CT/performance parity, compiler auto-import validation and complete language remain separate boundaries; no whole-module completion claim or weighted progress inference. Overall coarse10% unchanged; active user goal persists until erlang complete.0016 journal remains bounded; closed0015/sibling checkout/draft unchanged.

Part114: erlang133/351,218 missing; exact-source original2233/2233=778/1371/84 and same-head2626/full CI at2832c78. [Immutable proof](validation/part-114/remote-checkpoint.json). Failed112 and ten explicitly audited113 source corrections retained. No whole-module completion/performance claim; active erlang goal continues with atom/float output and remaining real runtime families.

Part115: atom/existing-name and float-output increment adds13 MFAs/175 independent modules;erlang146/351,205 missing,total296,local2816/full. [Local evidence](validation/part-115/tests.json), [source boundaries](validation/part-115/reference-audit.json). Original2408/same-head2816 CI116 pending; latest complete original114 remains2233/2233 at2832c78. Active goal continues through all remaining real runtime families.

Part116: exact-source original2408/2408=778/1546/84 and same-head2816/full CI at44ca437 verify atom/float output increment. [Proof](validation/part-116/remote-checkpoint.json). All earlier114 inputs/outcomes/generated hashes preserved;175 new modules match. erlang146/351,205 missing,total296;whole goal continues with clocks/calendar/timer options and real remaining runtime families.

Part120 establishes bounded original2657/2657 and same-head3105/full CI for119 at4aa285d. Native64 MD5 opaque ABI/forged-length safety,automatic physical-source frames/error_info/native VM population/distribution/incarnations,exact reductions/resources/races/upstream suites/performance remain explicit boundaries;181/351 presence is not whole-module completion. [Provenance](validation/part-120/remote-checkpoint.json).

Part121 adds7 external-function/query/error2/3/nif MFAs with real external MFA equality/order/hash,current-registry late binding and actual argument/error_info logical stack metadata. Local3207/full;original122 pending. function_exported reflects actual CLR availability; is_builtin is factual native classification,not stub registration. Full preloaded/load/unload populations,local code/env/init/hash/fun_info/ETF,source physical locations/host stacks/resources/upstream suites/performance remain unfinished. [Source boundaries](validation/part-121/reference-audit.json).

Part122 verifies121 against original2751/2751 and3207/full same-head CI at069b560. Actual registry/native classification,local code/env/init metadata/fun_info/ETF/hashing and full physical source/host stacks/resources/upstream/performance boundaries remain explicit;188/351 API presence is not whole-module completion. Ignored lexical-capture32/32 and existing1889/1889 draft checks are local only and unregistered. [Provenance](validation/part-122/remote-checkpoint.json).

Part123 local-function metadata: source-created closures have real immutable code descriptors and only lexical free captures; local exact/numeric equality, environment term order and hashes now use code/environment rather than a creation counter. Four functional fun introspection/text MFAs are implemented. CLR canonical module MD5, AST indexes/names and free-variable traversal are implementation-specific metadata; no BEAM binary hash/index/env-layout/SSA-optimization or local ETF interoperability claim. Descriptor-less host callbacks remain callable but introspection cannot invent their source metadata. Code unloading/purging remains unfinished.

fun_info(local,pid) now returns the invoking managed VM's real initialization process, registered init, which installs core modules, handles native-shaped get_status and serializes atomic host configuration. Failed registration rolls back; queued shutdown work cancels; initialization death ends other managed processes. First user PID remains1. Full OTP init boot/kernel protocols/arguments/exit statuses/background populations/process inspection remain unfinished. New57 independent compiled contracts await original verification; local queue/immutability/cross-runtime proofs do not imply native resource or scheduler parity.

Part124: complete original at704485b executes2808 with **2805 passed/3 mismatches**,same-head3270/full CI;all122778/1889/84 unchanged. [Preserved failures](validation/part-124/mismatches.json). fun_to_list returns UTF8 bytes,not Unicode codepoints;native -eval init phase starting differs from completed managed bootstrap. Registry342,erlang192/351,159 missing;current function increment not fully native-verified. Latest successful original1222751/2751. Publish failure evidence,correct new-only fixtures/real UTF8 output,repeat whole original and continue erlang goal.

Part125: UTF8 byte-list conversion repaired from preserved124 failures;two Unicode expectations and actual init-phase observation corrected explicitly;one local Unicode regression. **3271/full local**,plan2809=778/1947/84;1943 previous modules unchanged and all122 preserved. [Ledger](validation/part-125/correction-ledger.json). Registry342,erlang192/351,159 missing;whole goal active. Publish/freeze/verify original and same-head CI126 before promoting proof;ETF/runtime work follows.

Part126: native**2724/2724 executed pass**,all57 prior function contracts now match,but**complete=false**:new non-Latin1 module compilation abort prevents1 new module/84 diagnostics. Same-head**3271/full CI**. [Preserved abort](validation/part-126/remote-checkpoint.json). Registry342,erlang192/351,159 missing. Publish failure;correct new-only module name to Latin1,add independent compiler rejection127,repeat whole oracle128;latest complete success1222751/2751.

Part127: source-directed module-name Latin1/visible/control admission;new-only local UTF8 fixture repaired from126 abort;four positive names/nine independent diagnostics. **3284/full local**,plan2822=778/1951/93. [Ledger](validation/part-127/correction-ledger.json).1946 prior modules/84 diagnostics unchanged;registry342,erlang192/351,159 missing. Publish/freeze/full original and same-head CI128;whole goal active. Compiler annotations/aggregation/attribute syntax and ETF/runtime scope remain unfinished.

Part128: **complete original2822/2822=778/1951/93 and3284/full same-head CI** at7b5029c. [Provenance](validation/part-128/remote-checkpoint.json).All122 preserved;58 function/four valid-name/nine diagnostic contracts match;124/126 failures immutable. Registry342,erlang192/351,159 missing;whole goal active. Publish proof and continue real ETF/hash/function/reference/iovec/runtime prerequisites;physical code/init/annotations/resources boundaries remain explicit.

## Part481 — context entry and no-return boundaries

Close the observed nil/atom/literal context-forwarding and finite record-helper admission refusals. Retain ordinary-only semantics for binary-preserving duplicate wrappers and require typed initialized swap sources in newly proven no-return helpers. One original frame refusal remains in crashdump_viewer;receive/native capture/full application and API-contract boundaries remain explicit. No full MFA classification is promoted from these110 finite profiles.

## Part482 — observed frame refusal closed

The observed safe comparison before an uninitialized Y destination no longer produces a false exception/root edge. All eight canonical comparison BIFs share the already implemented operator semantics. Five invalid operand/destination/root cases remain refused and agree with original compiler diagnostics. No previously observed frame refusal remains in the1321-image inventory;receive/native capture failures may conceal further paths. Full APIs,physical/native/application/performance compatibility are not promoted.

## erpc alternative — 2026-11-05

All33 explicit OTP29.1.1 exports have a selectable C# implementation and retained original Erlang/BEAM. Calls, casts, request collections, multicast/multicall, source-stack trimming and timeout abandonment share the existing runtime/distribution machinery. Both implementations match330 focused observations, including3 unchanged upstream tests;8 connected RPC/spawn/monitor checks pass. This is finite Windows evidence, not universal100% acceptance. Full regression and canonical readiness are deliberately unrun. See [backend selection](erpc-backends.md) and [immutable receipts](validation/erpc-port-20261105/README.md).


## gen_server selectable alternative — 2026-11-05

All42 non-module_info exports now have a manual C# alternative beside unchanged original Erlang. Each passes107 full API observations and70 lifecycle/action/system/request scenarios, including four unchanged upstream tests. Preserve source-tail frames, Erlang improper-list guards, zero-timeout priority, successor-loop termination ownership and callback error/throw semantics. API completeness and this finite Windows corpus do not prove universal100% compatibility; full upstream/distribution/global/resource/platform/owned-heap/actual Logger delivery/metadata acceptance remains open. No full regression or canonical readiness refresh. See [backend contract](gen-server-backends.md) and [receipt](validation/gen-server-port-20261105/README.md).
