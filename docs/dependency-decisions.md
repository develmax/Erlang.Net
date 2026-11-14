# Dependency and compatibility decisions

Part 1656 reuses the tagged tuple check, record extraction and call emitters. No dependency was added. [Evidence](validation/part-1656/README.md).

Part 1655 reuses the tagged tuple check, record extraction and call emitters. No dependency was added. [Evidence](validation/part-1655/README.md).

Part 1654 reuses the tagged tuple check, record extraction and call emitters. No dependency was added. [Evidence](validation/part-1654/README.md).

Part 1653 reuses the tagged tuple check, record extraction and call emitters. No dependency was added. [Evidence](validation/part-1653/README.md).

Part 1652 reuses the tagged tuple check, record extraction and call emitters. No dependency was added. [Evidence](validation/part-1652/README.md).

Part 1651 reuses the tagged tuple check, record extraction and call emitters. No dependency was added. [Evidence](validation/part-1651/README.md).

Part 1650 reuses the closure and tail-call emitters. No dependency was added. [Evidence](validation/part-1650/README.md).

Part 1649 reuses dispatch, typed register and tail-call emitters. No dependency was added. [Evidence](validation/part-1649/README.md).

Part 1648 reuses dispatch, typed register and tail-call emitters. No dependency was added. [Evidence](validation/part-1648/README.md).

Part 1647 reuses the BEAM return-sharing pass. No dependency was added. [Evidence](validation/part-1647/README.md).

Part 1646 reuses BEAM list tests and tail-call emission. No dependency was added. [Evidence](validation/part-1646/README.md).

Part 1645 reuses the BEAM return-sharing pass. No dependency was added. [Evidence](validation/part-1645/README.md).

Part 1644 reuses list extraction and tail-call emission. No dependency was added. [Evidence](validation/part-1644/README.md).

Part 1643 reuses list extraction and tail-call emission. No dependency was added. [Evidence](validation/part-1643/README.md).

Part 1642 reuses the entry call, tail move and constructor planners. No dependency was added. [Evidence](validation/part-1642/README.md).

Part 1641 reuses the entry call, tail move and constructor planners. No dependency was added. [Evidence](validation/part-1641/README.md).

Part 1640 reuses the tagged entry test and existing entry call and tail move emitters against pinned OTP release disassembly. No dependency was added. [Evidence](validation/part-1640/README.md).

Part 1639 reuses closed constant normalization and the existing case and call planners. Pinned OTP sys_core_fold and the release disassembly provide the reference. No dependency was added. [Evidence](validation/part-1639/README.md).

Part 1638 reuses the existing register move plan for mixed tagged tuple and atom dispatch. The pinned OTP beam_ssa_codegen and release disassembly provide the instruction reference. No dependency was added. [Evidence](validation/part-1638/README.md).

Part 1637 reuses entry call and tuple emitters, using the pinned OTP release disassembly to place record tests, field reads and frame operations. No dependency was added. [Evidence](validation/part-1637/README.md).

Part 1636 uses existing call and tuple emitters with pinned OTP frame placement as the reference. No dependency was added. [Evidence](validation/part-1636/README.md).

Part 1635 uses the existing nested-call line propagation and pinned OTP beam_z as the reference. No dependency was added. [Evidence](validation/part-1635/README.md).

Part 1634 reuses the existing X-register list constructor and pinned OTP instruction ordering as the reference. No dependency was added. [Evidence](validation/part-1634/README.md).

Part 1633 follows pinned OTP sys_core_fold using the existing list constructor emitter. No dependency was added. [Evidence](validation/part-1633/README.md).

Part 1632 uses the existing C# entry call and tail emitters with pinned OTP frame placement as the reference. [Evidence](validation/part-1632/README.md).

Part 1631 reuses the C# monitor planner and entry guard analysis. No dependency was added. [Evidence](validation/part-1631/README.md).

Part 1630 uses the pinned OTP receive optimization and the existing runtime support for BEAM receive marker instructions. [Evidence](validation/part-1630/README.md).

Part 1629 reuses the existing call emitter and uses pinned OTP line folding and exception scope handling as the reference. [Evidence](validation/part-1629/README.md).

Part 1628 follows OTP sys_core_fold effect lowering using the existing C# discard emitter. No dependency was added. [Evidence](validation/part-1628/README.md).

Part 1627 reuses C# tail calls and framed argument construction for erlang:hibernate/3. Pinned OTP receive code generation supplies the reference. [Evidence](validation/part-1627/README.md).

Part 1626 applies the existing C# argument-construction literal folding rule to register-based tail arguments and uses OTP Core lowering as its reference. No dependency was added. [Evidence](validation/part-1626/README.md).

Part 1625 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1625/README.md).

Part 1624 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1624/README.md).

Part 1623 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1623/README.md).

Part 1622 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1622/README.md).

Part 1621 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1621/README.md).

Part 1620 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1620/README.md).

Part 1619 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1619/README.md).

Part 1618 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1618/README.md).

Part 1617 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1617/README.md).

Part 1616 reuses the C# call sequence planner and pinned OTP pre-codegen allocation as the reference. No dependency was added. [Evidence](validation/part-1616/README.md).

Part 1615 reuses the existing C# X-register planner and BIF encoders. The OTP 29.1.1 compiler and release disassembly are the oracle. No dependency was added. [Evidence](validation/part-1615/README.md).

Part 1572 reuses the tagged tuple test and tuple constructor emitters. No dependency was added. [Evidence](validation/part-1572/README.md).

Part 1571 reuses the tuple planner and pinned OTP tuple and rethrow lowering. No dependency was added. [Evidence](validation/part-1571/README.md).

Part 1570 reuses the existing case tuple planner for exception register operands. No dependency was added. [Evidence](validation/part-1570/README.md).

Part 1569 reuses lexical reference analysis and exception register emission from the pinned OTP model. No dependency was added. [Evidence](validation/part-1569/README.md).

Part 1568 uses OTP 29.1.1 v3_core.erl as the stacktrace construction reference. No dependency was added. [Evidence](validation/part-1568/README.md).

Part 1567 reuses literal normalization and branch emission, with OTP 29.1.1 beam_ssa_type.erl as reference. No dependency was added. [Evidence](validation/part-1567/README.md).

Part 1566 reuses the saved-call planner and consults OTP 29.1.1 beam_ssa_codegen.erl for call result registers. No dependency was added. [Evidence](validation/part-1566/README.md).

Part 1565 reuses record expansion and tuple planning and consults OTP 29.1.1 erl_expand_records.erl. No dependency was added. [Evidence](validation/part-1565/README.md).

Part 1564 reuses the saved-call planner and consults OTP 29.1.1 beam_ssa_codegen.erl for call and return emission. No dependency was added. [Evidence](validation/part-1564/README.md).

Part 1563 reuses the existing move scheduler and consults OTP 29.1.1 beam_block.erl. No dependency was added. [Evidence](validation/part-1563/README.md).

Part 1562 reuses the existing saved-call argument emitter and scratch path. No dependency was added. [Evidence](validation/part-1562/README.md).

Part 1561 reuses the saved call argument emitter for slot reuse and consults OTP 29.1.1 beam_ssa_pre_codegen.erl. No dependency was added. [Evidence](validation/part-1561/README.md).

Part 1560 reuses the existing BIF operand and framed tail encoders, with OTP 29.1.1 beam_ssa_pre_codegen.erl as reference. No dependency was added. [Evidence](validation/part-1560/README.md).

Part 1559 reuses record expansion and constant normalization for wildcard record fields and follows OTP 29.1.1 erl_expand_records.erl. No dependency was added. [Evidence](validation/part-1559/README.md).

Part 1558 follows the tagged tuple annotation rule in OTP 29.1.1 beam_ssa_type.erl and reuses the existing BEAM type encoder. No dependency was added. [Evidence](validation/part-1558/README.md).

Part 1557 follows the return contracts in OTP 29.1.1 beam_call_types.erl and reuses the existing BEAM type encoder. No dependency was added. [Evidence](validation/part-1557/README.md).

Part 1545 reuses frame and line encoders for measured list entries and if_end. OTP 29.1.1 remains the reference and the change adds no dependency. [Evidence](validation/part-1545/README.md).

Part 1530 reuses entry selector saves, saved call arguments and the call argument construction planner. No dependency was added. [Evidence](validation/part-1530/README.md).

Part 1529 reuses the local argument fixed point and OTP saved-register type encoding. No dependency was added. [Evidence](validation/part-1529/README.md).

Part 1528 reuses the compiler register call-sequence planner and OTP release instructions for argument forwarding and frame compaction. No dependency was added. [Evidence](validation/part-1528/README.md).

Parts 1526 and 1527 reuse OTP call result contracts, the tuple projection emitter and the entry selector save plan. No dependency was added. [Evidence](validation/part-1526/README.md).

Part 1525 reuses the tuple projection, entry selector, tuple construction, and local atom proof from the compiler. The OTP release instructions and pre-codegen source provide the reference. No dependency was added. [Evidence](validation/part-1525/README.md).

Part 1524 reuses the register case call-sequence planner, tuple projection, and OTP 29.1.1 pre-codegen liveness as the reference for choosing stack slots. No dependency was added. [Evidence](validation/part-1524/README.md).

Part 1523 reuses tuple projection, input type proofs, ordered call moves, and tuple result emission. OTP 29.1.1 release instructions provide the reference for branch-local trimming. The change adds no dependency. [Evidence](validation/part-1523/README.md).

Part 1495 reuses frame reservation and tuple and list argument construction in the nested selector planner. The pinned `beam_ssa_pre_codegen` source and release disassembly provide the reference. No dependency was added. [Evidence](validation/part-1495/README.md).

Part 1494 reuses call argument preparation for a field that stays live across the inner selector. The pinned `sys_core_fold` and `beam_ssa_pre_codegen` sources and release disassembly provide the reference. No dependency was added. [Evidence](validation/part-1494/README.md).

Part 1493 reuses capture analysis and register case emission to keep only the saved values used by the inner branches. Pinned `beam_ssa_pre_codegen` and `beam_trim` provide the reference for register liveness and branch restrictions. No dependency was added. [Evidence](validation/part-1493/README.md).

Part 1492 reuses the call argument flattener and register case sequence planner. Pinned `beam_ssa_pre_codegen` and release instructions provide the reference for saved arguments and register liveness. No dependency was added. [Evidence](validation/part-1492/README.md).

Part 1491 reuses the saved-selector tuple arm planner for cases with a continuation. Pinned `beam_ssa_pre_codegen` and release disassembly provide the frame and liveness reference. No dependency was added. [Evidence](validation/part-1491/README.md).

Part 1490 reuses the register tuple arm planner and BEAM field operand encoder for saved Y selectors. Pinned `beam_ssa_codegen` and release disassembly provide the reference for tuple projection. No dependency was added. [Evidence](validation/part-1490/README.md).

Part 1489 reuses the case arity and tag planner for saved Y selectors and extends the existing arity table emitter to that register class. The reference is pinned `beam_core_to_ssa` and the release disassembly. No dependency was added. [Evidence](validation/part-1489/README.md).

Part 1488 reuses the existing tuple arity and value table emitters in the entry planner. The reference is pinned `beam_ssa_codegen` and release disassembly. No dependency was added. [Evidence](validation/part-1488/README.md).

Part 1487 reuses entry tuple shape checks, field projection and literal selection tables for tuple tags. The reference is pinned `beam_core_to_ssa` and the release disassembly. No dependency was added. [Evidence](validation/part-1487/README.md).

Part 1486 reuses entry tuple shape checks, projections, register guard emission and literal selection tables. The reference is pinned `beam_core_to_ssa` and the release disassembly. No dependency was added. [Evidence](validation/part-1486/README.md).

Part 1485 reuses the entry guard and literal table emitters and the compiler's constant reader for bitstrings. The reference is pinned `beam_core_to_ssa` selection and the release instructions. No dependency was added. [Evidence](validation/part-1485/README.md).

Part 1484 reuses tuple projection and exact comparisons in the entry frame planner. The reference is the pinned OTP compiler's tuple and value selection in `beam_core_to_ssa` and the release instructions. No dependency was added. [Evidence](validation/part-1484/README.md).

Part 1483 reuses the separate entry frame and register construction planners for tuple dispatch. The reference is the pinned OTP compiler's frame placement and release instructions. No dependency was added. [Evidence](validation/part-1483/README.md).

Part 1482 reuses the register entry guard planner, separate frame planner and existing list and tuple construction emitter. The reference is the pinned OTP compiler's frame and register placement. No dependency was added. [Evidence](validation/part-1482/README.md).

Part 1481 uses the existing external call resolver, continuation analysis, and constructor emitter to project binary split results and construct measured tuples. No dependency was added. [Evidence](validation/part-1481/README.md).

Part 1480 reuses the unary BIF resolver, call result binding analysis and arithmetic guard emitter. It adds no dependency. [Evidence](validation/part-1480/README.md).

Part 1479 reuses entry guard operands, branch frame planning and ordinary register calls for comparison-guarded clauses. It adds no dependency. [Evidence](validation/part-1479/README.md).

Part 1478 reuses the AST rewrite walker, Erlang call binding, Unicode term conversion, and existing case emitters. It follows the pinned beam_doc signature table and visible-type filter. The erl_tracer regression uses unchanged Windows OTP release assets with original license notices. No dependency was added. [Evidence](validation/part-1478/README.md).

Part 1477 reuses the register tuple arm planner and tagged dispatch. It projects the common field before selecting an arm and emits independent terminal results. No dependency was added. [Evidence](validation/part-1477/README.md).

Part 1476 combines the existing register tuple arm emitter, ordinary call classification and call argument construction planner. It emits a heap check for the constructed arguments and adds no dependency. [Evidence](validation/part-1476/README.md).

Part 1475 reuses the existing entry selector planner and branch reference analysis for nested calls in general case emission. No dependency was added. [Evidence](validation/part-1475/README.md).

Part 1474 reuses the static and object command emitters for unary overloads. The wxDisplay test assets are the unchanged Windows OTP 29.1.1 release source and binary with upstream license provenance. The compiler adds no dependency. [Evidence](validation/part-1474/README.md).

Part 1473 reuses the existing C# case entry emitter and binding reference analysis. The pinned OTP reference places frames and allocates registers from SSA live intervals. The patch keeps eligible selectors in X through pattern tests and guards. It adds no dependency. [Evidence](validation/part-1473/README.md).

Part 1472 reuses entry case lowering, closure reference analysis and the fixed-point local result analysis. The compiler adds no dependency. [Evidence](validation/part-1472/README.md).

Part 1471 extends the existing entry BIF planner, live register encoding and typed operands. The managed test uses the existing lists BEAM fixture through an explicit dependency. No dependency was added to the compiler. [Evidence](validation/part-1471/README.md).

Part 1470 reuses the existing register-reusing list emitters and the structural matcher. The deferred callback shares the existing list-test encoding and no dependency was added. [Evidence](validation/part-1470/README.md).

Part 1469 reuses register pattern operands, guard lowering, ordered tail calls and branch frame planning. The compiler adds no dependency. [Evidence](validation/part-1469/README.md).

Part 1468 reuses call fact inference and the register arithmetic planner. The comparison uses the pinned OTP 29.1.1 Windows release. The implementation adds no dependency. [Evidence](validation/part-1468/README.md).

Part 1467 reuses ordinary call resolution, ordered argument moves, typed register encoding and proper-list proofs. The pinned OTP compiler supplies the instruction and line reference. No dependencies were added. [Evidence](validation/part-1467/README.md).

Part 1466 reuses tuple entry tests, sorted select_val emission and general closure lifting. The pinned OTP compiler supplies the block order and failure target reference. No dependencies were added. [Evidence](validation/part-1466/README.md).

Part 1465 reuses decoded BEAM labels, operand tags and the existing compact writer. The pinned OTP validator supplies the frame initialization requirement. No dependencies were added. [Evidence](validation/part-1465/README.md).

Part 1464 reuses the existing BEAM decoder and projection pass. The pinned OTP validator supplies the frame initialization requirement. No dependencies were added. [Evidence](validation/part-1464/README.md).

Part 1463 reuses the managed BEAM instruction decoder and existing compact operand writer. The pinned OTP beam_validator checks Y-slot initialization at tail calls. No dependencies were added. [Evidence](validation/part-1463/README.md).

Part 1462 reuses the tuple arity dispatch, atom selection and tuple projection emitters. The pinned OTP compiler supplies the block sharing and ordering reference. No dependencies were added. [Evidence](validation/part-1462/README.md).

Part 1461 reuses source option binding and the pinned OTP 29.1.1 compiler as the oracle for inline option precedence. OTP cerl_inline supplies the reference size and effort model. No dependencies were added. [Evidence](validation/part-1461/README.md).

Part 1460 reuses general closure lowering, lists:foldl/3 call emission and the existing list subtraction implementation for constant operands. Pinned OTP images determine evaluation and frame order. No dependencies were added. [Evidence](validation/part-1460/README.md).

Part 1459 reuses the atom table, record test, field projection and tail-call emitters. Parser token coordinates supply badrecord locations, checked against pinned OTP release disassembly. No dependencies were added. [Evidence](validation/part-1459/README.md).

Part 1458 reuses the literal table, existing guard AST and Erlang atom comparison rules. The Windows OTP release determines branch order and default-key elimination. No dependencies were added. [Evidence](validation/part-1458/README.md).

Part 1457 reuses the literal dispatch emitter, BIF guard resolution and constant error tail calls. Pinned OTP Core lowering and release instructions determine failure location and register use. No dependencies were added. [Evidence](validation/part-1457/README.md).

Part 1456 reuses the register tail-call emitter, membership table operands and Erlang term comparator. The OTP release disassembly determines branch and fallback layout. No dependencies were added. [Evidence](validation/part-1456/README.md).

Part 1455 reuses the BEAM select emitter, atom comparator, guard resolution, exact-equality BIF and local tail-call emission. OTP 29.1.1 beam_jump and release disassembly establish branch ordering and shared exits. [Evidence](validation/part-1455/README.md).

Part 1454 reuses the literal jump table and existing frameless fallback emitters. The typed binary result uses the binary alignment unit already defined by the compiler. [Evidence](validation/part-1454/README.md).

Part 1453 reuses the serial mapper call emitter, comprehension guard classification and recursive result emission. Pinned OTP disassembly determines predicate branch and frame layout. There are no new dependencies. [Evidence](validation/part-1453/README.md).

Part 1452 reuses existing call resolution, recursive list result emission and specialized mapping plans. The pinned OTP disassembly establishes the mapper order and slot layout. There are no new dependencies. [Evidence](validation/part-1452/README.md).

Part 1451 reuses recursive mapping error emission and tuple test and projection helpers. OTP 29.1.1 release disassembly establishes the label and register schedule. There are no new dependencies. [Evidence](validation/part-1451/README.md).

Part 1450 reuses closure reference collection and recursive mapping generation. OTP v3_core at the pinned tag and release disassembly supply the helper shape. The pinned OTP source carries the Apache 2.0 license. No source is copied and there are no new dependencies. [Evidence](validation/part-1450/README.md).

Part 1449 reuses saved-variable bindings, the shared record shape test and direct tuple projection. The pinned OTP disassembly establishes register destinations and error order. There are no new dependencies. [Evidence](validation/part-1449/README.md).

Part 1448 reuses the shared record checks and tuple field emitter. OTP 29.1.1 disassembly supplies the register schedule reference. The chunk comparison uses the Python standard library and adds no runtime dependency. [Evidence](validation/part-1448/README.md).

Part 1447 reuses the closure reference collector and existing wildcard matching. Pinned OTP disassembly supplies the field projection reference. [Evidence](validation/part-1447/README.md).

Part 1433 reuses the bound pattern, argument and construction emitters. There are no new dependencies. [Evidence](validation/part-1433/README.md).

Part 1432 reuses the existing pattern and frame emitters for call-result cases. The pinned BEAM supplies the register schedule reference. [Evidence](validation/part-1432/README.md).

Part 1431 reuses the source liveness proof and tuple operand emitter. The pinned release defines direct Y construction from a live x0. There are no new dependencies. [Evidence](validation/part-1431/README.md).

Part 1430 reuses the existing shared list planner and integer range proof. The pinned release fixes the test and projection schedule. There are no new dependencies. [Evidence](validation/part-1430/README.md).

Part 1429 reuses the shared list entry and argument construction planners. The pinned BEAM supplies the heap and register reference. There are no new dependencies. [Evidence](validation/part-1429/README.md).

Part 1428 compares pinned optimized SSA landing pads with the existing source handler walk. It reuses the source pattern and call graph analysis without adding dependencies. [Evidence](validation/part-1428/README.md).

Part 1427 uses the pinned beam_ssa_throw pass and reuses the existing C# call walker, argument flattening and tuple scheduler. It adds no dependency or third-party code. [Evidence](validation/part-1427/README.md).

Part 1426 uses the existing source call walker, exception patterns and external tail-call emitter. The pinned OTP beam_ssa_throw pass supplies the comparison for handler suitability and empty-stack raise/3. The BEAM runtime already admits raise/3 through its native catalog. No dependency or third-party code was added. [Evidence](validation/part-1426/README.md).

Part 1425 reuses mixed cons entry, select_val, the argument flattener and the register constructor plan. The pinned Windows OTP release and source supply the reference. No dependency or third-party code was added. [Evidence](validation/part-1425/README.md).

Part 1424 extends mixed list entry and saved-call planning for a computed cons argument. It reuses the existing type tests, record-free list construction, exact comparison and local call instructions. The pinned OTP source and Windows image supply the reference. No dependency or third-party code was added. [Evidence](validation/part-1424/README.md).

Part 1423 reuses BEAM record shape tests, exact value dispatch, record updates, list construction, and the register tail-call planner. The pinned Windows OTP release and source supply the reference. No dependency or third-party code was added. [Evidence](validation/part-1423/README.md).

Part 1422 reuses the list extraction and saved register planning for a call followed by a case selector. The reference is the pinned OTP source and Windows image. No dependency was added. [Evidence](validation/part-1422/README.md).

Part 1421 reuses record guards, register constructors, tail-call emission and saved-call planning for a shared guarded list entry. The pinned OTP source and Windows image supply the reference. No dependency was added. [Evidence](validation/part-1421/README.md).

Part 1420 reuses local call visibility, register construction and record update emission. The record shape proof follows local argument forwarding and tuple results. The pinned OTP source and Windows image supply the reference. No dependency was added. [Evidence](validation/part-1420/README.md).

Part 1419 reuses register construction, equality tests, local tail calls and the exact local return-shape proof. The pinned OTP source and Windows image supply the reference. No dependency was added. [Evidence](validation/part-1419/README.md).

Part 1418 reuses tuple arity dispatch, dynamic call operand planning, argument construction, parallel moves and deferred label operands. The pinned OTP source and Windows image supply the reference. No dependency was added. [Evidence](validation/part-1418/README.md).

Part 1417 reuses the saved-call planner's slot lifetimes, argument moves and shape proofs to defer fields from a shared tuple. The pinned OTP compiler and release instructions supply the reference. No dependency was added. [Evidence](validation/part-1417/README.md).

Part 1416 reuses the saved-call planner's slot lifetimes, parallel moves and tuple checks for fields live across calls. Pinned OTP 29.1.1 supplies the reference for early and deferred projections. No dependency was added. [Evidence](validation/part-1416/README.md).

Part 1415 reuses the saved-call planner, parallel register moves and the existing exact local return-shape proof. The pinned OTP projection sinking pass and release instructions supply the reference. No dependency was added. [Evidence](validation/part-1415/tuple-liveness.md).

Part 1414 reuses the saved-call planner, tuple shape checks, register case emission and deferred labels. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1414/README.md).

Part 1413 reuses entry selector planning, tuple shape proofs, catch emission and register case branches. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1413/README.md).

Part 1412 reuses tagged tuple tests, badmatch emission, and saved-call register planning. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1412/README.md).

Part 1411 reuses tuple return proofs, nested-call flattening and the existing argument constructor and register scheduler. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1411/README.md).

Part 1410 reuses the closure emitter, capture analysis and call lifetime planner. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1410/README.md).

Part 1409 reuses nested-call flattening, saved-call planning, and register tuple and list constructors. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1409/README.md).

Part 1408 reuses the saved-call planner, heap allocation and direct return emitter. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1408/README.md).

Part 1407 reuses comprehension helper naming, saved call scheduling and register case emission. The pinned OTP 29.1.1 source and installed images supply the reference. No dependency was added. [Evidence](validation/part-1407/README.md).

Part 1406 reuses BIF call resolution, tuple construction, typed operands and the tail register move scheduler. Pinned OTP 29.1.1 sources and installed images supply the reference. No dependency was added. [Evidence](validation/part-1406/README.md).

Part 1405 reuses tuple shape and field tests, static and object command emission, and protected class union emission. Pinned OTP 29.1.1 sources and installed images supply the reference. No dependency was added. [Evidence](validation/part-1405/README.md).

Part 1404 reuses colour and shape guards, option mapper closures, the frame trim and swap emitters, and the wx call planner. Pinned OTP 29.1.1 sources and installed images supply the reference. No dependency was added. [Evidence](validation/part-1404/README.md).

Part 1403 reuses register tuple shape tests, heap and tuple emission, wx class and environment checks, and prepared tail calls. Pinned OTP 29.1.1 sources and installed images supply the reference. No dependency was added. [Evidence](validation/part-1403/README.md).

Part 1402 reuses register colour guards, the conversion scheduler, frame trimming and the wx environment emitter. Pinned OTP 29.1.1 sources and installed images supply the reference. No dependency was added. [Evidence](validation/part-1402/README.md).

Part 1401 reuses the guarded local forwarding plan, register guards, wx object command and protected class union emitters. Pinned OTP 29.1.1 sources and installed images supply the reference. No dependency was added. [Evidence](validation/part-1401/README.md).

Part 1400 reuses call argument lowering, the saved-call stack planner, swap and frame trim. The pinned OTP 29.1.1 compiler and release disassembly supply the reference. No dependency was added. [Evidence](validation/part-1400/README.md).

Part 1399 reuses source call resolution, the entry call planner, ordered argument moves and existing apply instructions. The pinned OTP 29.1.1 source and installed release supply the reference. No dependency was added. [Evidence](validation/part-1399/README.md).

Part 1398 reuses ordinary-call resolution, ordered argument moves, GC BIF instructions and typed registers. OTP 29.1.1 source and release metadata supply the reference. No dependency was added. [Evidence](validation/part-1398/README.md).

Part 1397 reuses nested argument scheduling, closure lifting and local caller detection. The pinned OTP 29.1.1 source and release supply the reference. No dependency was added. [Evidence](validation/part-1397/README.md).

Part 1396 reuses ordinary argument planning, saved-call frames, closure lifting and the closed local caller proof. The pinned OTP 29.1.1 source and release supply the reference. No dependency was added. [Evidence](validation/part-1396/README.md).

Part 1395 reuses the saved-call stack planner, ordinary-call resolver and register case emitter. The pinned OTP 29.1.1 source and release supply the reference. No dependency was added. [Evidence](validation/part-1395/README.md).

Part 1394 reuses tuple arity dispatch, entry register binding and existing frame planning. The OTP 29.1.1 release and pinned source supply the reference. No dependency was added. [Evidence](validation/part-1394/README.md).

Part 1393 reuses entry frame dispatch, tuple construction, list extraction and the function-arity test opcode. OTP 29.1.1 source and release disassembly supply the reference. No dependency was added. [Evidence](validation/part-1393/README.md).

Part 1392 reuses the ordinary call resolver, argument construction scheduler and stack slot emitters. The OTP 29.1.1 code generation and frame trimming passes provide reference behavior. No new implementation or dependency was imported. [Evidence](validation/part-1392/README.md).

Part 1391 reuses the saved-call argument emitter and case tuple builder. Case branches track the last uses of initial bindings and call results, then reuse slots in the selector frame. The OTP 29.1.1 release provides the reference. No dependency was added. [Evidence](validation/part-1391/README.md).

Part 1390 reuses ordinary call planning, the register argument constructor scheduler and BEAM list and frame emission for call sequences inside case arms. OTP 29.1.1 supplies the instruction and complete image oracle. No dependency was added. [Evidence](validation/part-1390/README.md).

Part 1389 reuses the register case planner, private literal-result proof, type guard opcode table and deferred label operands. The OTP 29.1.1 release supplies the instruction reference for the shared list test and branch-local frame. No dependency was added. [Evidence](validation/part-1389/README.md).

Part 1388 reuses BEAM list extraction, the ordinary call and constructor planners, and private call-site analysis. The OTP 29.1.1 release provides the instruction and complete image reference. No dependency was added. [Evidence](validation/part-1388/README.md).

Part 1387 reuses the straight-call and saved-result planners after list dispatch. The existing BEAM list extraction, stack move and trimming emitters handle the head and tail destinations. The OTP 29.1.1 release supplies the byte comparison. No dependency was added. [Evidence](validation/part-1387/README.md).

Part 1386 reuses list entry dispatch, register tail calls, tuple emission and BEAM type encoding. The checked element and tuple projection sequence follows the pinned OTP compiler's element optimization and the release instructions. No dependency was added. [Evidence](validation/part-1386/README.md).

Part 1385 reuses the straight call reader, ordinary argument plans, register construction and the BEAM frame emitters. The pinned OTP compiler supplies the stack slot, swap and trim instruction order for validation. No dependencies were added. [Evidence](validation/part-1385/README.md).

Part 1384 reuses the entry call-chain emitter, its stack-slot tracking, ordinary call resolution and the shared register constructor scheduler. The OTP 29.1.1 BEAM provides the oracle for the X0 continuation, stack frame and heap check. No dependency was added. [Evidence](validation/part-1384/README.md).

Part 1383 reuses the argument construction scheduler, exact Erlang term ordering, map instruction header emitter and ordinary tail-call resolution. OTP 29.1.1 supplies the instruction and execution oracle. The existing compiler and runtime representations cover the feature without an additional dependency. [Evidence](validation/part-1383/README.md).

Part 1382 reuses the register construction and constant call sequence planners. Permanent tests link the existing pinned OTP resources and source-relative profiles. No dependencies were added. [Evidence](validation/part-1382/README.md).

Part 1381 reuses the mixed cons dispatch planner, argument construction scheduler and closure capture analysis for register liveness. OTP 29.1.1 provides the execution and instruction oracle. No dependencies were added. [Evidence](validation/part-1381/README.md).

Part 1380 reuses the arithmetic call planner, argument construction scheduler and private caller checks. The type proof walks direct forwarding edges and literal seeds. OTP 29.1.1 provides the execution and instruction oracle. No dependencies were added. [Evidence](validation/part-1380/README.md).

Part 1379 reuses the ordinary call plan, parallel register moves, gc_bif arithmetic and private caller bounds. OTP 29.1.1 provides the instruction and execution oracle. No dependencies were added. [Evidence](validation/part-1379/README.md).

Part 1378 reuses branch-local frame planning, the existing exact comparison and checked record operations, register call scheduling and tuple result emission. OTP 29.1.1 supplies the execution and instruction oracle. No dependencies were added. [Evidence](validation/part-1378/README.md).

Part 1377 reuses the entry clause planner, tuple field binding and proper-list caller analysis. OTP 29.1.1 supplies the execution and instruction oracle. No dependencies were added. [Evidence](validation/part-1377/README.md).

Part 1376 extends the existing first-call continuation planner and reuses argument scheduling and tuple emission. OTP 29.1.1 supplies the execution and instruction oracle. No dependencies were added. [Evidence](validation/part-1376/README.md).

Part 1375 reuses the local tuple result proof, pair fact inference and entry register call scheduler. OTP 29.1.1 supplies the instruction and execution oracle. No dependencies were added. [Evidence](validation/part-1375/README.md).

Part 1374 reuses the C# entry frame planner, resolved call eligibility and the register scheduler for constructed call arguments. OTP 29.1.1 provides the instruction and execution oracle. No dependencies were added. [Evidence](validation/part-1374/README.md).

Part 1373 reuses the entry frame planner, slot reuse and ordinary call chain scheduler. Pinned OTP 29.1.1 disassembly guides result placement and trim timing. No dependencies were added. [Evidence](validation/part-1373/README.md).

Part 1372 reuses the C# branch frame planner, proper-list analysis and BEAM call argument scheduler. Pinned OTP 29.1.1 release disassembly guides clause ordering and extraction destinations. No dependencies were added. [Evidence](validation/part-1372/README.md).

Part 1371 reuses the entry clause planner, BEAM term ordering and the register constructor planner. Pinned OTP 29.1.1 release disassembly guides selection order, frame placement and argument construction. No dependencies were added. [Evidence](validation/part-1371/README.md).

Part 1370 reuses the C# sibling call scheduler and constructor emitter. OTP 29.1.1 argument setup and release disassembly guide slot reuse and register placement. No dependencies were added. [Evidence](validation/part-1370/README.md).

Part 1369 reuses record expansion, tuple and list emission, and call operand scheduling. The pinned Apache-2.0 OTP beam_ssa_codegen need_heap pass and release disassembly guide heap placement. No dependencies were added. [Evidence](validation/part-1369/README.md).

Part 1368 reuses lexical capture analysis, entry call planning and the C# argument emitter. The pinned Apache-2.0 OTP beam_ssa_pre_codegen find_yregs pass provides the reference for keeping values in X registers until a call would clobber them. No dependencies were added. [Evidence](validation/part-1368/README.md).

Part 1367 reuses C# expression lowering, slot allocation and call argument loading. The pinned Apache-2.0 OTP beam_ssa_pre_codegen and beam_ssa_codegen sources and Windows release disassembly guide the frame comparison. No dependencies were added. [Evidence](validation/part-1367/README.md).

Part 1366 reuses the C# entry case planner, tagged tuple matcher and call argument scheduler. Pinned Apache-2.0 OTP release disassembly and the focused Erlang fixture guide instruction order. No dependencies were added. [Evidence](validation/part-1366/README.md).

Part 1365 reuses the C# call chain scheduler, argument move ordering and frame trimming. Pinned Apache-2.0 OTP beam_ssa_codegen.erl and beam_trim.erl, synthetic release-oracle code and ttb disassembly guide the projection and reload order. No dependencies were added. [Evidence](validation/part-1365/README.md).

Part 1364 reuses the C# entry call chain scheduler, tuple projection and register match writers. The Apache-2.0 OTP 29.1.1 beam_ssa_codegen.erl match and call emitters and release disassembly guide the comparison. No dependencies were added. [Evidence](validation/part-1364/README.md).

Part 1363 reuses C# call selection, argument moves, tagged tuple testing and wx option emission. Pinned Apache-2.0 OTP beam_ssa_codegen.erl and beam_trim.erl guide register and stack scheduling. Release disassembly supplies the debugger and wx instruction oracles. No dependencies were added. [Evidence](validation/part-1363/README.md).

Part 1362 reuses the C# forward guard, option mapper, class check, closure and frame emitters. Pinned Apache-2.0 OTP beam_ssa_codegen.erl, beam_trim.erl and Windows release disassembly guide slot lifetimes and label order. The comparison uses OTP with an Erlang queue stub. No dependencies were added. [Evidence](validation/part-1362/README.md).

Part 1361 reuses the C# local caller analysis, constant substitution, case planner and ordered call moves. Pinned Apache-2.0 OTP beam_ssa_type.erl, beam_ssa_opt.erl, beam_ssa_codegen.erl and beam_trim.erl guide argument inference and X0 assignment. OTP beam_lib.erl supplies the reference chunk reader. No dependencies were added. [Evidence](validation/part-1361/README.md).

Part 1360 reuses the C# tuple entry and register case planners, stack slot liveness and private call-site binding analysis. Pinned Apache-2.0 OTP erl_bifs.erl, beam_trim.erl and release disassembly guide conversion call liveness and slot relocation. No dependencies were added. [Evidence](validation/part-1360/README.md).

Part 1359 reuses the C# case and tuple planners, entry frame allocation, tuple shape tests and stack relocation. Pinned Apache-2.0 OTP beam_ssa_codegen.erl, beam_trim.erl and release disassembly guide case failure, saved tuple liveness and projection order. No dependencies were added. [Evidence](validation/part-1359/README.md).

Part 1358 reuses the C# register case and tuple planners, call argument scheduler, and frame trim operations. Pinned Apache-2.0 OTP beam_ssa_codegen.erl, beam_trim.erl and release disassembly guide call-result liveness and slot relocation. No dependencies were added. [Evidence](validation/part-1358/README.md).

Part 1357 reuses the C# register case planner, exact term ordering, frame-tail emitter and local function bindings. The pinned Apache-2.0 OTP beam_ssa_type.erl, beam_jump.erl and release disassembly guide return inference, branch sharing and failure locations. No dependencies were added. [Evidence](validation/part-1357/README.md).

Part 1356 reuses the C# entry call planner, capture analysis, register case and tuple emitter, operand typing and unary GC BIF writer. Pinned Apache-2.0 OTP beam_call_types.erl and Windows release disassembly supply operand types, slot reuse and instruction order. No dependencies were added. [Evidence](validation/part-1356/README.md).

Part 1355 reuses the C# ordinary call planner, frame trim, register move scheduler, named fun and tuple emitters. Pinned Apache-2.0 OTP beam_ssa_codegen.erl, beam_ssa_pre_codegen.erl and beam_block.erl supply the reference for heap liveness, function references and swap folding. No dependencies were added. [Evidence](validation/part-1355/README.md).

Part 1354 reuses the C# ordinary call planner, register tuple emitter and separate entry-frame allocator. The pinned Apache-2.0 OTP beam_ssa_codegen.erl and release disassembly establish tuple instruction and frame order. No dependencies were added. [Evidence](validation/part-1354/README.md).

Part 1353 reuses C# nested call planning, slot relocation, source binding analysis and the existing expression emitter. Pinned Apache-2.0 OTP release disassembly supplies the frame and argument reference. No dependencies were added. [Evidence](validation/part-1353/README.md).

Part 1352 reuses the C# register case, tuple operand, selector and tail-call emitters. OTP 29.1.1 call types and release disassembly supply the Apache-2.0 reference. No dependencies were added. [Evidence](validation/part-1352/README.md).

Part 1351 reuses C# closure binding analysis and the register case planner. OTP 29.1.1 core-to-SSA free variable handling and release instructions supply the Apache-2.0 reference. No dependencies were added. [Evidence](validation/part-1351/README.md).

Part 1350 reuses the C# expression emitter, per-branch frame planning, nested call plans and tuple entry checks. The Apache-2.0 OTP 29.1.1 instructions supply the release comparison. No dependencies were added. [Evidence](validation/part-1350/README.md).

Part 1349 reuses the existing tuple field, computed cons, stack and call writers. The pinned Apache-2.0 OTP release disassembly gives the order of field reads and saved registers. No dependencies were added. [Evidence](validation/part-1349/README.md).

Part 1348 reuses the C# cons entry planner, tuple arity dispatch and call chain frame writer. Pinned OTP 29.1.1 code generation and release disassembly provide the Apache-2.0 reference. No dependencies were added. [Evidence](validation/part-1348/README.md).

Part 1347 reuses the C# call resolver, parallel argument moves, cons extraction and select_val writers. Pinned OTP frame placement and release disassembly provide the Apache-2.0 reference. No dependencies were added. [Evidence](validation/part-1347/README.md).

Part 1346 reuses the C# record expansion, call resolver, constructor planner and BEAM operand writers. OTP 29.1.1 pre-codegen source and the release disassembly provide the allocation reference under Apache-2.0. No dependencies were added. [Evidence](validation/part-1346/README.md).

Part 947 reuses source call substitution for automatic inlining. No new dependency. [Evidence](validation/part-947/README.md).

Part 946 reuses the existing token and metadata paths. No new dependency. [Evidence](validation/part-946/README.md).

Part 945 reuses existing character escape and triple-quoted indentation code. No new dependency. [Evidence](validation/part-945/README.md).

Part 944 reuses the existing BEAM loader and source metadata writer. No new dependency. [Evidence](validation/part-944/README.md).

Part 943 reuses Erlang record AST nodes and existing OTP lists fixtures. No new dependency. [Evidence](validation/part-943/README.md).

Part 942 reuses the AST traversal and existing term emitters. OTP ms_transform supplies the format reference. No new dependency. [Evidence](validation/part-942/README.md).

Part 941 reuses the include resolver and documentation writer with pinned OTP Markdown input. No new dependency. [Evidence](validation/part-941/README.md).

Part 940 reuses object macro expansion and the existing literal escaping. No new dependency. [Evidence](validation/part-940/README.md).

Part 939 reuses the existing BIF interpreter and typed-register encoding. No new dependency. [Evidence](validation/part-939/README.md).

Part 938 reuses template substitution and the existing pattern and exception emitters. No new dependency. [Evidence](validation/part-938/README.md).

Part 937 reuses declared NIF signatures, the source attribute decoder and the existing BEAM loader semantics. No new dependency. [Evidence](validation/part-937/README.md).

Part 936 reuses the current AST and literal escaping. OTP erl_pp supplies the reference layout. No new dependency. [Evidence](validation/part-936/README.md).

Part 935 extends the existing pure receive matcher and reuses the BEAM guard emitters. No new dependency. [Evidence](validation/part-935/README.md).

Part 934 reuses the OTP-derived skip pattern rewrite and existing list, pattern and error emitters. No new dependency. [Evidence](validation/part-934/README.md).

Part 933 reuses the runtime predicate BIFs and wire decoding, with no new dependency. [Evidence](validation/part-933/README.md).

Part 932 reuses the existing BEAM BIF encodings, operand emission and runtime operator implementations. No new dependency. [Evidence](validation/part-932/README.md).

Part 931 reuses the release profile and metadata writer for the public_key ASN.1 cohort. No new dependency. [Evidence](validation/part-931/README.md).

Part 930 reuses the existing include resolver and reads six Apache-2.0 headers from the pinned OTP source tree for corpus validation. [Evidence](validation/part-930/README.md).

Part 929 uses the pinned OTP ASN.1 tool with noobj to restore public_key header inputs for validation. The C# compiler still emits the compared BEAM and the C# ASN.1 generator still supplies ELDAPv3. [Evidence](validation/part-929/README.md).

Part 928 reuses general closure emission, the failure stub and the existing OTP lists fixture. No new dependency. [Evidence](validation/part-928/README.md).

Part 927 reuses targeted substitution, case emission and BEAM function info. Pinned OTP provides the failure helper naming and instruction reference. No new dependency. [Evidence](validation/part-927/README.md).

Part 926 reuses the managed receive planner and its pure BIF implementations. Pinned OTP provides the catch-all and guard behavior reference. No new dependency. [Evidence](validation/part-926/README.md).

Part 925 reuses the existing receive matcher map and type operations and guard emitter. Pinned OTP provides the mailbox behavior reference. No new dependency. [Evidence](validation/part-925/README.md).

Part 924 reuses preprocessor source locations, release include path mappings and the Docs writer. Pinned OTP output provides the file and location reference. No new dependency. [Evidence](validation/part-924/README.md).

Part 923 reuses the inline rewrite and existing record emitter. Pinned OTP disassembly supplies the parser-action oracle. No new dependency. [Evidence](validation/part-923/README.md).

Part 922 reuses the inline template rewrite and local function reference walk. The pinned sys_core_inline implementation and Windows OTP disassembly establish argument and residual call behavior. No new dependency. [Evidence](validation/part-922/README.md).

Part 921 reuses fixed bit-match instruction encoding and the existing qualifier loop. The pinned OTP runtime and disassembly establish strict mismatch behavior. No new dependency. [Evidence](validation/part-921/README.md).

Part 920 reuses qualifier traversal, the existing bit construction encoder and the managed BEAM writable binary implementation. Pinned OTP disassembly provides the accumulator opcode layout. No new dependency. [Evidence](validation/part-920/README.md).

Part 919 reuses general closure lifting, capture analysis and local BEAM calls. The pinned disassembly establishes the self-call and self-value forms. No new dependency. [Evidence](validation/part-919/README.md).

Part 918 reuses unary GC and binary map guard BIF emission and the registered runtime operations. Pinned Windows OTP disassembly establishes the opcode forms and failure edges. No new dependency. [Evidence](validation/part-918/README.md).

Part 917 reuses pattern and case clause emission, the existing GC guard BIF path and managed runtime operations. Pinned v3_core and release disassembly establish the failure behavior. No new dependency. [Evidence](validation/part-917/README.md).

Part 916 reuses the list qualifier emitter, registered OTP compiler iterator helpers and maps:from_list/1. Pinned v3_core and Windows release instructions provide the iterator contract. No new dependency. [Evidence](validation/part-916/README.md).

Part 915 reuses the existing BEAM operand encoding, managed type tests and BIF interpreter. Pinned OTP disassembly provides the instruction and guard failure reference. No new dependency. [Evidence](validation/part-915/README.md).

Part 914 reuses the managed option mapper, existing Unicode conversion, reply emission and generated wx release profile. Register and stack layouts come from pinned OTP release disassembly. No new dependency. [Evidence](validation/part-914/README.md).

Part 913 reuses the existing wx class check, record guard, option closure and release profile. The pinned OTP beam_trim source and release images provide the reference. No new dependency. [Evidence](validation/part-913/README.md).

Part894 reuses the release profile and EEP-48 writer to preserve recorded header paths and include callback type dependencies. No new dependency. [Evidence](validation/part-894/README.md).

Part893 reuses managed bit matching commands and the existing exact and lower-bound checks. The pinned OTP assembler establishes operand and flag layouts. No new dependency. [Evidence](validation/part-893/README.md).

Part892 reuses preprocessor file locations and the existing EEP-48 writer. The pinned OTP documentation term establishes the annotation layout. No new dependency. [Evidence](validation/part-892/README.md).

Part891 reuses managed guard classification, BEAM map extraction and list construction. The pinned OTP assembler and runtime establish the reference. No new dependency. [Evidence](validation/part-891/README.md).

Part890 reuses parsed type forms, source call bindings and managed EEP-48 writing. The pinned OTP compiler establishes behaviour_info and Docs layouts. No new dependency. [Evidence](validation/part-890/README.md).

Part889 reuses the existing specialized and general emitters with isolated scratch planning. The pinned OTP assembler establishes the direct unary BIF instruction reference. No new dependency. [Evidence](validation/part-889/README.md).

Part888 reuses managed type tests, BIF encoding and typed registers. The pinned OTP assembler establishes the reference. No new dependency. [Evidence](validation/part-888/README.md).

Part887 reuses managed exception handlers and saved trace rethrow instructions. The pinned OTP assembler establishes the direct cleanup and exception layout. No new dependency. [Evidence](validation/part-887/README.md).

Part886 reuses managed arithmetic and list operations, the direct argument mover and float register encoding. The pinned OTP assembler establishes operation and guard layouts. No new dependency. [Evidence](validation/part-886/README.md).

Part885 reuses unary BIF emission, type predicates and managed head, number and map operations. The pinned OTP assembler establishes the reference. No new dependency. [Evidence](validation/part-885/README.md).

Part884 reuses managed bit syntax normalization, BEAM string table interning and the existing binary construction opcode. The pinned OTP assembler output establishes segment encoding. No new dependency. [Evidence](validation/part-884/README.md).

Part883 reuses managed guard BIF emission and typed registers. The pinned OTP assembler output establishes opcode selection and the inferred list length range. No new dependency. [Evidence](validation/part-883/README.md).

Part882 reuses managed term ordering and existing BEAM map operations. The pinned OTP Core lowering and assembler output establish evaluation and operand order. No new dependency. [Evidence](validation/part-882/README.md).

Part881 reuses BEAM BIF emission, the direct arithmetic emitter and managed Erlang operators. The pinned OTP assembler output establishes instruction and operand order. No new dependency. [Evidence](validation/part-881/README.md).

Part880 reuses managed PID and tuple tests, the unary BIF opcode and the existing tuple size runtime implementation. The pinned OTP assembler output and ERTS tuple arity limit establish the reference. No new dependency. [Evidence](validation/part-880/README.md).

Part879 reuses tagged tuple validation and tuple field extraction. The OTP 29.1.1 record expander and deterministic assembly establish the reference. No new dependency. [Evidence](validation/part-879/README.md).

Part878 reuses the managed argument move scheduler and the existing call_fun instruction. The OTP 29.1.1 Core lowering and assembler output establish the reference. No new dependency. [Evidence](validation/part-878/README.md).

Part877 reuses the managed pattern and guard emitters and the existing transactional receive matcher. The pinned OTP compiler and receive code generator establish the reference. No new dependency. [Evidence](validation/part-877/README.md).

Part876 reuses managed closure allocation, the fun table and deferred function labels. The pinned OTP compiler and disassembly establish the reference. No new dependency. [Evidence](validation/part-876/README.md).

Part875 reuses managed arithmetic BIF emission, frame slots and typed operands. The pinned OTP compiler and disassembly establish the instruction and liveness reference. No new dependency. [Evidence](validation/part-875/README.md).

Part874 reuses managed BIF and test emission, call frames, and the existing xmerl release profile. The pinned OTP compiler and disassembly establish the reference. No new dependency. [Evidence](validation/part-874/README.md).

Part873 reuses the tagged tuple opcode, guard failure labels, and abstract call writer. The pinned OTP disassembly and debug forms establish the reference. No new dependency. [Evidence](validation/part-873/README.md).

Part872 reuses managed documentation text and ETF emission. The pinned OTP `beam_doc.erl` establishes metadata normalization and inheritance. No new dependency. [Evidence](validation/part-872/README.md).

Part871 reuses the source call binding table, parser and managed call emitter. The pinned OTP compiler supplies the imported call and debug form oracles. No new dependency. [Evidence](validation/part-871/README.md).

Part870 reuses the managed BEAM catch runtime and frame planner. The pinned OTP compiler and disassembly establish opcode layout and frame sizes. No new dependency. [Evidence](validation/part-870/README.md).

Part869 reuses managed call setup, cons and tuple emission, and the existing Erlang append BIF. The pinned OTP disassembly establishes tail calls, heap allocation and source lines. No new dependency. [Evidence](validation/part-869/README.md).

Part868 reuses managed free-variable analysis, parsed Core ordinals, frame planning, and the BEAM fun table. The pinned OTP Core-to-SSA source and disassembly establish the reference. No new dependency. [Evidence](validation/part-868/README.md).

Part867 reuses managed record expansion and tuple emission. The pinned OTP `erl_expand_records.erl` defines traditional record field order, defaults, and wildcard semantics. The pinned parser and release debug forms establish dynamic remote annotations. No new dependency. [Evidence](validation/part-867/README.md).

Part866 reuses the existing managed call operands, frame slots and BEAM apply instructions. The pinned OTP `v3_core.erl` and `beam_ssa_codegen.erl` establish evaluation order and register layout. No new dependency. [Evidence](validation/part-866/README.md).

Part865 reuses managed tail-call and literal dispatch emission, the release metadata profile, and the pinned OTP disassembly and compiler as reference. No new dependency. [Evidence](validation/part-865/README.md).

Part864 uses .NET Rune, UTF-8 decoding and grapheme boundaries for documentation text. The pinned OTP `beam_doc.erl` and `unicode_util:pattern_whitespace/0` define the source behavior. No new dependency. [Evidence](validation/part-864/README.md).

Part863 uses the compiler's parsed type terms, managed reachability analysis, and the existing ETF writer. The pinned OTP `beam_doc.erl` defines the type entries and the pinned compiler supplies the byte comparison oracle. No new dependency. [Evidence](validation/part-863/README.md).

Part862 shares the existing managed character escape decoder with quoted literals and uses .NET stack frames for corpus failure locations. The pinned OTP scanner defines escape behavior. No new dependency. [Evidence](validation/part-862/README.md).

Part861 reuses the managed macro argument reader and the corpus tool's include directories. OTP 29.1.1 `epp.erl` provides the function-context reference and the pinned `erlc` builds the regression oracle. No new dependency. [Evidence](validation/part-861/README.md).

Part860 reuses the managed record checks, typed operands, label writer and local call graph. The pinned OTP compiler supplies the deterministic oracle. [Evidence](validation/part-860/README.md).

Part799 reuses C# call, tuple and frame writers. The pinned OTP disassembly guides dynamic apply, closure invocation and stack liveness. The fixture preserves the upstream notice. No new dependencies were required. [Evidence](validation/part-799/README.md).

Part798 reuses C# tuple, list, call and frame writers. The pinned OTP disassembly guides register scheduling and heap allocation. No new dependencies or notices were required. [Evidence](validation/part-798/README.md).

Part797 reuses the C# closure table and exception instruction writers. The pinned OTP release disassembly guides nested exception scopes and register scheduling. The fixture preserves the upstream Apache-2.0 notice. No new dependencies were required. [Evidence](validation/part-797/README.md).

Part796 reuses C# guard BIF resolution and call, list, tuple and match instruction writers. The pinned OTP disassembly guides the recursive dispatch. The fixture preserves the upstream Apache-2.0 notice. No new dependencies were required. [Evidence](validation/part-796/README.md).

Part795 reuses the C# mapping scheduler, BIF resolution and list failure handling. Pinned OTP disassembly guides the allocator dispatch and helper order. No new dependencies or notices were required. [Evidence](validation/part-795/README.md).

Part794 reuses the C# mapping scheduler, list failure handling and BIF resolution. Pinned OTP disassembly guides the helper ordering and register layout. No new dependencies or notices were required. [Evidence](validation/part-794/README.md).

Part793 reuses the C# list mapping helper and call, list and tuple instruction writers. Pinned OTP disassembly guides call order and register placement. No new dependencies or notices were required. [Evidence](validation/part-793/README.md).

Part792 reuses C# call, frame and tuple instruction writers and the interleaved progress call emitter. Pinned OTP disassembly guides the captured argument and frame layout. No new dependencies or notices were required. [Evidence](validation/part-792/README.md).

Part791 reuses the C# list comprehension helpers and call, tuple and list instruction writers. Pinned OTP release disassembly guides register scheduling and helper ordering. No new dependencies or notices were required. [Evidence](validation/part-791/README.md).

Part790 reuses the C# tuple, list, call and exception instruction writers. Pinned OTP code generation and release disassembly guide heap reservations and register scheduling. No new dependencies or notices were required. [Evidence](validation/part-790/README.md).

Part789 reuses the C# closure table, BIF resolution and exception instruction writers. Pinned OTP disassembly guides captured register order and exception frames. No new dependencies or notices were required. [Evidence](validation/part-789/README.md).

Part788 reuses the C# binary match instruction writers and BIF resolution. Pinned OTP disassembly guides the match-context and frame handling. No new dependencies or notices were required. [Evidence](validation/part-788/README.md).

Part787 reuses the C# tagged reply matcher, closure table and instruction writers. The pinned OTP compiler source and release disassembly guide frame and register placement. No new dependencies or notices were required. [Evidence](validation/part-787/README.md).

Part786 reuses C# BEAM instruction writers, literal folding and source BIF resolution. The pinned OTP compiler source and release disassembly guide heap reservations and operand placement. No new dependencies or notices were required. [Evidence](validation/part-786/README.md).

Part785 reuses the C# closure table, source call resolution and BEAM instruction writers. The pinned OTP compiler source and release disassembly guide function table entries and registers. No new dependencies or notices were required. [Evidence](validation/part-785/README.md).

Part784 reuses C# BEAM instruction writers, constant evaluation and guard binding resolution. Pinned OTP compiler source and release disassembly determine the frame layout. No new dependencies or notices were required. [Evidence](validation/part-784/README.md).

Part783 reuses C# BEAM frame and closure emission, tuple arity dispatch and source call resolution. It follows the pinned OTP release disassembly for register placement. No new dependencies or notices were required. [Evidence](validation/part-783/README.md).

Part782 reuses the C# BEAM closure table, list comprehension helper, numeric evaluator and source call resolution. Pinned OTP release artifacts and disassembly provide the oracle. No new dependencies or notices were required. [Evidence](validation/part-782/README.md).

Part781 reuses the Erlang numeric evaluator and C# BEAM case, operand and tuple writers. The fixture uses the pinned OTP compiler. [Evidence](validation/part-781/README.md).

Part780 follows the pinned OTP pattern lint and reuses the Erlang numeric evaluator and C# BEAM metadata and selection writers. [Evidence](validation/part-780/README.md).

Part779 reuses C# BEAM operand, guard, literal and debug writers with the original Observer assets and wx header. [Evidence](validation/part-779/README.md).

Part778 reuses the existing C# type parser and BEAM metadata and operand writers with the pinned Observer and wx assets. [Evidence](validation/part-778/README.md).

Part777 reuses the C# BEAM call, literal, type and release profile writers with the original SSL supervisor assets. [Evidence](validation/part-777/README.md).

Part776 uses the pinned OTP opcode and type definitions and existing C# BEAM call, tuple, literal and metadata writers. The regression embeds the original Inets assets. [Evidence](validation/part-776/README.md).

Part775 reuses the existing C# BEAM exception, operand and tuple writers with the original Mnesia source and image. [Evidence](validation/part-775/README.md).

Part774 reuses existing C# BIF, typed register, call and tuple writers. The release regression reuses the original SASL assets. [Evidence](validation/part-774/README.md).

Part773 uses the pinned OTP opcode table and existing C# typed register, operand and debug writers. The release regression reuses the original Kernel assets. [Evidence](validation/part-773/README.md).

Part772 reuses C# call, literal and tuple writers with the pinned Kernel signal-handler source and BEAM. [Evidence](validation/part-772/README.md).

Part771 reuses C# BEAM call scheduling and ETF encoding for external function references. The regression embeds the pinned SSL source and image. [Evidence](validation/part-771/README.md).

Part770 uses the existing C# BEAM operand, call, line and release metadata writers. The regression reuses the pinned Mnesia source and image. [Evidence](validation/part-770/README.md).

Part769 reuses the C# constant binding reader, call scheduler and Diameter release profile. The regression embeds existing upstream source and image pairs. [Evidence](validation/part-769/README.md).

Part768 follows the pinned OTP disassembly and reuses the C# BEAM call, line and list-generator writers. The original source and image are reused as the regression reference. [Evidence](validation/part-768/README.md).

Part767 reuses the C# literal dispatch, term order and BEAM operand writers. The regression reuses the pinned original source and image already in the repository. [Evidence](validation/part-767/README.md).

Part766 reuses the C# BEAM selection and BIF operands for the pinned grouped dispatch order. The imported fixture retains its upstream notice. [Evidence](validation/part-766/README.md).

Part765 follows the OTP erl_reply disassembly and reuses C# operand, frame and release metadata writers. The upstream fixture retains its source notice. [Evidence](validation/part-765/README.md).

Part764 follows OTP epp macro argument annotations and uses the existing C# release profile and debug writers. The imported test fixture retains upstream notices. [Evidence](validation/part-764/README.md).

Part763 reuses the C# type parser and BEAM metadata writers with pinned OTP attribute and annotation behavior. Dependencies and licenses are unchanged. [Evidence](validation/part-763/README.md).

Part762 reuses the C# inline pipeline and standard BEAM list and error instructions. Dependencies and licenses are unchanged. [Evidence](validation/part-762/README.md).

Part761 follows OTP targeted inline option forms and reuses the C# constant and function writers. Dependencies and licenses are unchanged. [Evidence](validation/part-761/README.md).

Part760 follows pinned OTP epp file remapping and reuses the C# source annotations and file transition infrastructure. Dependencies and licenses are unchanged. [Evidence](validation/part-760/README.md).

Part759 follows pinned OTP closure ordinal and register reuse behavior. It reuses C# closure and tuple writers with no dependency or license change. [Evidence](validation/part-759/README.md).

Part758 reuses the C# closure and tuple instruction writers with pinned OTP heap and register behavior. Dependencies and licenses are unchanged. [Evidence](validation/part-758/README.md).

Part757 follows OTP arithmetic liveness and reuses C# typed operands, arithmetic and closure writers. Dependencies and licenses are unchanged. [Evidence](validation/part-757/README.md).

Part756 reuses the C# GC arithmetic emitter and local tail-call instructions for the pinned OTP countdown shape. Dependencies and licenses are unchanged. [Evidence](validation/part-756/README.md).

Part755 reuses the C# tuple constructor and BEAM closure writers with capture order verified by pinned OTP. Dependencies and licenses are unchanged. [Evidence](validation/part-755/README.md).

Part754 follows the OTP lambda table and reuses the C# compact operand writer, module digest and closure loader. Dependencies and licenses are unchanged. [Evidence](validation/part-754/README.md).

Part753 follows OTP named fun syntax and reuses C# source closure identities, capture plans and tail dispatch. Dependencies and licenses are unchanged. [Evidence](validation/part-753/README.md).

Part752 follows OTP external fun syntax and reuses the C# make_fun BIF and call emitter. Dependencies and licenses are unchanged. [Evidence](validation/part-752/README.md).

Part751 follows pinned OTP token separation and string grammar. It reuses the C# token stream and Erlang list representation. Dependencies and licenses are unchanged. [Evidence](validation/part-751/README.md).

Part750 follows the pinned OTP disassembly and frame trimming rules. It reuses C# instruction writers for calls, tagged tuples, frames and literals. The upstream test source retains its Apache-2.0 notice. No new dependency is required. [Evidence](validation/part-750/README.md).

Part749 follows the pinned OTP external fun lowering and constant folding. It reuses the existing C# function identity, ETF writer and literal table interning. Dependencies and licenses are unchanged. [Evidence](validation/part-749/README.md).

Part748 reuses C# tagged tuple, frame, call and list instructions. Dependencies and licenses are unchanged. [Evidence](validation/part-748/README.md).

Part647 follows OTP binary match instruction forms and reuses the C# typed operand and frame emitters. [Evidence](validation/part-647/README.md).

Part646 reuses the C# emitter and Erlang exact term ordering for select tables. [Evidence](validation/part-646/README.md).

Part645 reuses existing C# binary matching opcodes for fixed unsigned bytes. [Evidence](validation/part-645/README.md).

Part644 reuses existing C# BEAM type tests and branch slots. [Evidence](validation/part-644/README.md).

Part643 reuses the C# GC BIF and operand emitters for LDAP arithmetic. [Evidence](validation/part-643/README.md).

Part642 follows the pinned BER generator forms and reuses the C# source and BEAM pipelines. [Evidence](validation/part-642/README.md).

Part641 follows the pinned ASN.1 public API template and reuses the BEAM exception and debug writers. [Evidence](validation/part-641/README.md).

Part640 reuses C# preprocessing and source tokens to derive header records and file transitions. [Evidence](validation/part-640/README.md).

Part639 reuses the C# source AST, token annotations and ETF writer for LDAP abstract debug forms. [Evidence](validation/part-639/README.md).

Part638 reuses existing BEAM operand emission and tracks the frame high-water mark for temporary call slots. [Evidence](validation/part-638/README.md).

Part637 reuses C# operand and stack slot emission for matches, tuple construction and linear case branches. [Evidence](validation/part-637/README.md).

Part636 reuses the existing C# emitter and select_val for closed literal and tagged-tuple alternatives. [Evidence](validation/part-636/README.md).

Part635 reuses C# BEAM instruction and metadata writers. It follows the pinned OTP selector, tail-call and register operand forms. [Evidence](validation/part-635/README.md).

Part634 reuses the C# emitter and ETF writer for direct guard tests and restores tagged codec delegation from the pinned OTP BER output. [Evidence](validation/part-634/README.md).

Part633 reproduces OTP BER helpers and emits named list mapping functions in C#. The partial release profile reconstructs compile options without copying chunks or requiring OTP during compilation. [Evidence](validation/part-633/README.md).

Part632 replaces the generated ASN.1 descriptor interpreter with type-specific codecs and follows the pinned OTP BER type-tree helper order. Original OTP remains a development oracle. [Evidence](validation/part-632/README.md).

Part631 derives field codecs and tags from the ASN.1 type graph and reuses the BER primitive and collection helpers. [Evidence](validation/part-631/README.md).

Part630 derives element tags from the ASN.1 type graph and reuses existing BER tag and length helpers. [Evidence](validation/part-630/README.md).

Part629 reuses the generated BER length and tag helpers and the C# signed integer lowering. [Evidence](validation/part-629/README.md).

Part628 derives direct codecs from ASN.1 alias resolution and verifies BER lengths and tag handling in the pinned OTP VM. [Evidence](validation/part-628/README.md).

Part627 uses the pinned OTP ASN.1 generated source and original VM as the BER error oracle. [Evidence](validation/part-627/README.md).

Part626 derives bounds from the ASN.1 schema and uses the existing BEAM protected-call emitter to normalize ASN.1 exceptions. [Evidence](validation/part-626/README.md).

Part625 compares the generated header against the pinned upstream asset and uses the existing Erlang lexer for bare atoms. [Evidence](validation/part-625/README.md).

Part623 extends the existing C# BEAM emitter with frame and label planning for nested expressions and patterns. The OTP compiler remains the oracle. [Evidence](validation/part-623/README.md).

Part622 emits local list helpers using the existing call, frame and function-table writer. [Evidence](validation/part-622/README.md).

Part621 reuses the BEAM tuple, list and call operations for tagged decoding. [Evidence](validation/part-621/README.md).

Part620 reuses frame and tuple operations for nested field matching and preserves unread input across local calls. [Evidence](validation/part-620/README.md).

Part619 reuses BEAM tuple dispatch and frames and checks the case failure when the predicate result is unproven. [Evidence](validation/part-619/README.md).

Part618 reuses the existing type, match context and compact operand writer for BER tag lowering. [Evidence](validation/part-618/README.md).

Part617 extends the existing BEAM type table and binary operand emitter for signed variable-width segments. [Evidence](validation/part-617/README.md).

Part616 reuses the OTP type table and handler, stack and frame instruction formats in the C# emitter. [Evidence](validation/part-616/README.md).

Part615 uses the existing OTP-compatible frame and tuple operand writer and checks unknown pair results. [Evidence](validation/part-615/README.md).

Part614 uses the pinned OTP tuple type and bs_match wire conventions in the C# emitter. [Evidence](validation/part-614/README.md).

Part613 reuses the existing literal dispatch and compact operand writer with a failure target at function info. [Evidence](validation/part-613/README.md).

Part612 uses the existing type parser and BEAM operand emitter. The OTP compiler is used only as the validation reference. [Evidence](validation/part-612/README.md).

Part611 reuses the C# type parser for release debug forms and records the admitted stdlib build settings. [Evidence](validation/part-611/README.md).

Part610 removes the external OTP ASN.1 generator from the corpus runner. The emitted Erlang codec reuses the ASN.1 TLV runtime interface. [Evidence](validation/part-610/README.md).

Part609 uses the admitted OTP ASN.1 tool with noobj for ELDAPv3 source preparation in the validation runner. The C# BEAM compiler has no ASN.1 tool dependency. [Evidence](validation/part-609/README.md).

Part608 reuses the existing C# frame and operand emitter and the admitted Mnesia release settings. The installed BEAM is only used by validation. [Evidence](validation/part-608/README.md).

Part607 follows pinned OTP assembler, type-table and opcode conventions for the tested call-case flow. Reuse the existing BEAM writer, source token annotations, and explicit release profile mechanism. Use the admitted OTP disassembly and release input metadata as reference evidence. [Evidence](validation/part-607/README.md).

Part606 follows OTP beam_ssa_codegen move-chain ordering and pinned opcode definitions. Reuse Erlang AST constant normalization, source token annotations, and the existing release metadata writer. The new release profiles contain compiler parameters rather than reference chunk payloads. [Evidence](validation/part-606/README.md).

Part605 follows pinned OTP beam_asm and beam_dict for literal order, ETF payloads and module fingerprints. Reuse constant normalization, deterministic ETF, and the pinned compressor for debug and documentation chunks. Record release options and build paths separately from the generated image. The generator reads source and profile inputs, while the corpus runner reads the original image for comparison. [Evidence](validation/part-605/README.md).

Part604 uses the zlib 1.3.2 source pinned by OTP 29.1.1 for Windows x64 BEAM ETF compression. The original .NET ETF path produced identical raw content and different compressed bytes. A bounded native adapter reproduces the ERTS deflate call and the C# compiler retains ETF and BEAM assembly. Retain the zlib notice and build recipe. Reuse the existing AST and compact encoder for the tested BIF and constructor opcodes. [Evidence](validation/part-604/README.md).

Part603 uses pinned OTP erl_parse.yrl, beam_doc.erl, compile.erl and genop.tab for supported documentation and dynamic call shapes. Reuse the existing apply dispatch representation, register emitter and ETF serializer. The existing ETF compressor produces valid Docs payloads, with an observed compressed-byte difference against OTP. [Evidence](validation/part-603/README.md).

Part602 uses pinned OTP genop.tab, assembler disassembly, erl_parse.yrl, and epp.erl for dispatch, external call, typed form and predefined macro rules. Reuse term ordering, the existing type parser and macro preprocessor. Corpus include lookup uses wx and SNMP source directories indicated by their build layout. [Evidence](validation/part-602/README.md).

Part601 follows OTP 29.1.1 beam_asm and genop.tab for file-indexed line entries and instruction operands. Reuse the existing Erlang AST, compact encoder and module writer. Use admitted OTP disassembly to pin the tested pattern, call and move sequences. [Evidence](validation/part-601/README.md).

Part600 follows Apache-2.0 OTP epp and io_lib for include and supported token stringification behavior, and beam_asm/genop.tab for local-call instruction and table layout. Reuse lexer tokens, the compiler's source offsets, and the existing BEAM writer. Use BCL path and file APIs for explicit file-backed compilation. [Evidence](validation/part-600/README.md).

Part599 follows pinned Apache-2.0 OTP epp for macro arity selection, argument boundaries, and substitution locations. Reuse the expression parser and runtime term ordering for a restricted conditional evaluator. The evaluator admits a fixed syntax subset and does not execute arbitrary calls. [Evidence](validation/part-599/README.md).

Part598 follows the pinned Apache-2.0 OTP `epp.erl` conditional and undefinition rules for the supported subset. Reuse the compiler token stream, macro table, and source offsets. Use a BCL stack to track nested conditionals. [Evidence](validation/part-598/README.md).

Part597 follows the pinned Apache-2.0 OTP `epp.erl` object macro path and `erl_parse.yrl` typed forms. Reuse compiler tokens, source offsets, the existing type parser and compile option binder. The BEAM gate admits the tested warning option without generalizing to functional options. No external preprocessor is used by the C# backend. [Evidence](validation/part-597/README.md).

Part596 uses the existing lexer whitespace, token, and source diagnostic conventions with BCL spans and UTF-16 rune validation. Delimiter width, literal content, and indentation follow OTP 29.1.1 `erl_scan.erl`. Documentation forms follow the applicable `erl_parse.yrl` clauses and are stored separately from runtime attributes. Retain Apache-2.0 attribution. [Evidence](validation/part-596/README.md).

Part595 follows OTP 29.1.1 `beam_asm.erl`, `beam_dict.erl`, and `genop.tab` for the supported BEAM output profile. Reuse the existing parser and ETF serializer for source forms and metadata. Use BCL big-endian writes and MD5 for the OTP container and version attribute. Keep the original OTP compiler as the fixture oracle. [Evidence](validation/part-595/README.md).

Part594 uses the existing runtime's BEAM wire constants and BCL `BigInteger.ToByteArray` for signed big-endian integer bytes. The latter supplies the payload, while the pinned Apache-2.0 OTP `beam_asm:encode/2` defines the compact prefixes and minimum signed width. The retained OTP compiler package provides the reference implementation. The C# writer adds no package dependency. [Evidence](validation/part-594/README.md).

Part580 — The user explicitly requests a manual C# gen_event alternative while keeping original Erlang selectable. Reuse the existing term/process/module/BEAM runtime and original gen/proc_lib/sys/io/Unicode dependencies. BCL events and actor frameworks do not preserve the OTP handler, mailbox, callback and link contract, so they are not substituted. No new package or external C# library is introduced. Pin OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5; preserve Apache-2.0 notices in the retained source/fixtures and derived implementation. Semantic catalogs separate equal strings/numbers by contract. Finite Windows comparisons and the retained timing failure do not certify universal parity. [Evidence](validation/part-580/README.md).

## gen_statem C# alternative — 2026-11-04

Inspect pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 `lib/stdlib/src/gen_statem.erl`, its complete engine and callback exports, existing GenServer and runtime mailbox/timer/function boundaries. Existing .NET state-machine/actor APIs and System.Threading.Timer do not supply OTP postponed-event ordering, state-enter restrictions, callback-module stacks, timer reference tombstones or the sys record protocol. Translate the library engine directly from Apache-2.0 OTP; preserve notices and unchanged original Erlang/BEAM. Reuse the explicitly selected gen/proc_lib/sys and formatting/logging modules exactly as the source does, and existing runtime timer/receive primitives. No third-party package or host ERTS dependency in production. This user's instruction authorizes this module's C# alternative; it does not broaden the policy for other pure modules. Implementation precedes focused verification; finite timing/value observations do not certify universal OTP compatibility.

Part533 — Reuse the existing typed backing/root, guest-frame, native environment and provider collectors. Distinguish recipient completion from shared origin lifetime; external Host custody is explicit. Runtime-owned cleanup must not inherit a retiring actor scheduler or its private authority. Use existing .NET10 ForceYielding/context suppression and explicit own-VM atom scope for exit construction; await only the specific ETS queue observation fence before mailbox capture. No new dependency/package, alternate production BEAM VM, license header or package publication. Finite foundation evidence does not promote whole API contracts.

Part532 — Reuse the existing NativeMemory-backed atomics allocation, shared off-heap chain, typed resource registry, owned heap/transfer protocols and genuine original-image/provider collector. Inspected pinned erl_bif_atomics.c, erl_bif_unique magic-reference layout and current atomics/store shutdown paths. Atomics remain a distinct magic destructor family, not a fabricated NIF resource type. No new dependency/package or alternate production BEAM VM. Host finalizers release only explicit external-host custody; guest GC owns actual process headers. Preserve preloaded and file-loaded original metadata as separate execution profiles, with raw full evidence and no license-header or package-publication change. Whole P2/24-contract completion is not claimed.

Part485 — Keep original re.erl and the existing admitted PCRE2 provider. BCL Regex has different matching, error, capture and stack semantics and is not substituted. Reuse BitString backing pin/slice machinery and the existing38 native bootstrap providers through explicit typed calls. No new package or production BEAM VM. The ordinary driver services are exact existing C sources, now rebuilt by the catalog verifier with admitted headers. Preserve notices and keep LICENSE.local.txt uncommitted. Finite176 outcomes and1321 preparations are separate from full module/application/native/platform/performance parity.

Part399 — Keep original prim_eval.S/os.erl and the selected 180-module closure as original Erlang/BEAM library inputs. Extend the existing C# mailbox and Windows native port launcher; introduce no package or alternate BEAM engine. BCL linked-list nodes support the source save-link protocol, but optimistic selector retries and stable-node leases cannot preserve effectful nested/priority receive behavior, so callback mode owns a shared save cursor while pre-activation pure selection keeps its existing route. Use actual Windows QueryPerformanceCounter through Stopwatch, per-call UTC wall time and process PID for OS primitives. Dictionary/OrdinalIgnoreCase and ProcessStartInfo.Environment cannot preserve the pinned Windows environment comparator, red-black traversal order and leading drive-variable keys; use a VM-owned tree, immutable owned CRT C locale and the existing raw CreateProcessW launcher. Do not mutate the host environment or global CRT locale. Existing source notices and the uncommitted local license file remain; these implementation choices do not imply full native/import closure, Unix signal support or performance superiority. Final-source tests and exact original outcomes are acceptance gates, not source review alone.

Part369 — Port all31 binary exports using the pinned OTP algorithms and existing cooperative runtime, source-frame scopes and typed reference-resource registry. Reuse BCL spans, byte arrays, BigInteger and an immutable byte trie; add no package or production BEAM engine. Real backing descriptors retain immutable logical views, actual allocated capacity and writer ownership. Pin/sharing effects follow named source events; compiler allocation optimization, physical heap addresses, absolute resource ceilings and performance identity remain separate platform/compiler obligations. No captured sizes, original reference IDs or projected stacks are injected into production. GitHub original-only fixtures and passive evidence tools remain development infrastructure. Preserve upstream notices; add no source banners and keep LICENSE.local.txt uncommitted.

Part367 — Reuse the prepared source-guided queue core and existing immutable Erlang tuple/list terms. BCL Queue<T> is mutable and ImmutableQueue<T> hides a different representation; neither preserves OTP's observable {Rear,Front}, exact half-split shapes or clause-specific acceptance of manually constructed improper spines. Use BCL buffers only during traversal, with cooperative ProcessContext reductions and the existing source/exception continuation infrastructure. No new package or production BEAM dependency. Baseline remains OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. Preserve the existing upstream queue.erl notice, add no own-source license banners and leave the local license file uncommitted. Historical isolated checks are not original/full-contract acceptance.

Part356 — Reuse the existing BCL/runtime loader and explicit image-review policy; add no package dependency or new third-party installation. Match native entry module/function declarations to actual loaded code rather than fixture names. The Windows fixture builder uses the existing MSVC installation, pinned OTP headers with an explicit LLP64 size configuration, and owned kill-on-close process jobs. These fixture binaries remain outside the repository. The final runtime closure has 127 reviewed source guards; preserve the two native plan seals and all older Verified evidence. .NET 10.0.11 already selects altered-search-path loading, so do not characterize the explicit Win32 loader as repairing an absent runtime flag. Compatibility counts remain unchanged.


Part 352 — Preserve all 74 published runtime source bindings and add only ErlangBasicRegistrations.cs, ErlangBasicBifNames.cs and AutoImportNames.cs, for 77 total. Raw Erlang fixtures and C# catalogs are evidence rather than runtime guards. This increment delivers no package, runtime source, test expectation, loader or scheduler-affinity change.

Part351 — Reuse runtime/BCL scheduling and existing tests without packages. Preserve all older Verified evidence, bind74 current runtime dependencies, and refine unsupported spawn options truthfully. Archive exact source and declared raw proof subsets; no SDK/runtime/package binaries.

Part350 — Reuse existing C#/Erlang fixture infrastructure and packages. Keep pending1250 raw unchanged; change only the existing .gitattributes row in reallocation1275 and seal its strict LF bytes. Publish lean text/ETF receipts, not runtime binaries, ZIP or nupkg outputs.

Part349 — Reporting/evidence only: no new SDK build or runtime dependency. Reuse the built348 readiness tool; extend current-source guard union35→59. Publish explicit lean raw original/CI subsets plus full hash manifests. Keep XML/solution EOL exceptions separate from C# and returned-value equality; preserve archived348 incident chronology.

Part348 — Use C# Stack/IEnumerator/HashCode and existing immutable term layouts; add no package or production BEAM dependency. Preserve CS1061 harness history, lifecycle-capture source finding and package raw-byte preflight refusal separately. Publish exact25source snapshots and lean text receipts, not runtime/ZIP/nupkg binaries. Keep inherited registry custom Limits and5/19/563 statuses.

Part347 — Reporting/source-audit correction only: use existing345Tool for readiness, no SDK/full rerun and no runtime package. Physical accounting needs an explicit ownership model rather than invented numbers or node-wide GC substitutions. Preserve raw proof bytes through one attribute row and corresponding reallocation workflow seal.

Part346 — Reuse the existing original-only collector, owned-process controls and passive typed reader; add no runtime package or BEAM dependency. Preserve pending1246 unchanged, update only attributes in reallocation1271 plus its workflow seal, and retain byte-exact controller/payload/workflow sources.

Part345 — Reuse the existing parser, source module, scheduler and registry; add no framework, package or production BEAM dependency. Reuse existing module-name validation for imports. Preserve full/focused/preparation histories and custom Limits; archive exact34source snapshots plus lean text receipts, excluding runtime/ZIP/nupkg binaries. Keep346collector fixes outside345source.

Part344 changes original-observation tooling only. Four procfs discovery handlers now tolerate a disappearing process; ownership, generation, permission and normal cleanup requirements remain enforced. Exact six-file source and deterministic controls pass independent review. The previous eight-case original run remains failed/incomplete, with seven partial acquisitions; no fresh original result or full SDK run is claimed. Runtime baseline343 remains7224 tests/11 integrations,587 APIs and5/17/565 compatibility counts. A new original eight-case capture requires the reviewed manifest61E52EC3EEE23FD4084772C0E383470C7FD09DAC6A6F16D7BD50AD3A6CB85229. Historical syscall and peer-crash/cleanup ordering are unproven. [Evidence](validation/part-344/).

Part343 — Reuse existing C# scheduler, mailbox, module and pattern infrastructure without a production BEAM dependency or new package. Publish exact43 tested snapshots and lean textual receipts. Preserve actual342 CI success and real-peer collector failure separately; exclude ZIPs/runtime binaries/package binaries and leave LICENSE.local.txt uncommitted.

Part342 — Reuse C# module/image ownership, read-only per-module dictionaries and ConditionalWeakTable capture plans; add no framework, package or production BEAM dependency. Standalone original tooling builds pinned OTP with owned private EPMD/TCP peers. Preserve Main unowned EOL bytes, recorded failure history, notices and uncommitted LICENSE.local.txt; no NuGet publication.

Part341 — Reuse the existing C# readiness generator, System.Text.Json and BCL SHA256 with no new third-party or production BEAM dependency. The test project references the existing Tool project for direct admission/generator controls. Preserve upstream notices and leave LICENSE.local.txt uncommitted.

Part340 — Reuse owned distribution controllers, existing process/mailbox/monitor/alias/trace channels, real registry apply dispatch and process-info requests. Add only rpc:call/4,5 and erpc:call/4,5,execute_call/4 needed for remote proc_lib metadata. Node enumeration reads actual connection entries; no fake registry or local substitute for remote labels. No third-party package or actor framework is added.

Part338 — Reuse process monitors, dictionaries, mailbox queries, runtime standard streams and immutable term/map iterators. Port only the pinned logger/environment/limiter dependency paths required by real crash reports; introduce no new package or actor framework. Physical per-process BEAM allocation is unavailable in CLR and is never simulated with guessed counts. Keep existing third-party notices and the owner-local license proposal uncommitted.

Part337 — Inspect pinned proc_lib, io_lib_format, io_lib_pretty, erl_error, string and Unicode generator/data. Reuse the existing terms, runtime identity/float/function-information facilities, System.Text.Rune/strict UTF8, and current BCL grapheme measurement with its Unicode-version gap explicit. A general .NET text/JSON serializer cannot preserve Erlang raw report lists or source depth/budget rules; no external formatter, actor framework or new package is introduced. Preserve existing third-party notices and keep the owner-local public-license proposal uncommitted.

Part336 — Inspected the existing differential workflow and source-capture observer tools. Reuse their pinned OTP build/configuration, Python standard-library process and hashing facilities, with fresh erlc/VM percase. Python is development observation infrastructure only; production/runtime formatter remains C#. No package or public license decision changes; existing third-party notices and owner-local uncommitted license are preserved.

Part335 — Pinned OTP29.1.1 proc_lib.erl stop, sys.erl system messages and gen.erl call/reply/name resolution guide the adaptation. Existing project/runtime/BCL primitives supply actual monitors, aliases, sends, ownership and cancellation. No third-party framework, production BEAM dependency or new license header is added; existing notices and license decisions remain. Global registry implementation and complete system protocol are separate dependencies.

Part334 — Pinned OTP29.1.1 proc_lib.erl and c.erl guide raw_initial_call, initial_call, translate_initial_call and process labels. Existing C# runtime/BCL facilities are reused; separate actor or logging libraries would not supply these source contracts. Existing OTP notices and the repository license decision remain unchanged; no license headers or new third-party dependency are introduced.

Part333 — Source/license/reuse: pinned OTP29.1.1 proc_lib.erl supplies sync_start_monitor and VERIFY_NO_MONITOR_OPT contracts. Existing project C# process, monitor, mailbox, option parser, sequence descriptions and BCL mechanisms remain the implementation; existing licensing decisions apply and no dependency is added. A separate actor framework would duplicate ownership and alter mailbox/failure semantics. Native tuning options unsupported by the current parser remain unsupported, rather than reporting fabricated heap/scheduler effects.

Part332 — Local proc_lib startup and receive ordering. Reviewed pinned OTP29.1.1 proc_lib.erl and VM receive instructions. Reuse the project's existing C# ProcessRuntime, monitor/link machinery, immutable terms, Mailbox and BCL timing/cancellation; their source and licensing decisions remain unchanged. A new actor framework would require adapting its mailbox and failure semantics and is unnecessary for this slice. No new third-party component or license header is introduced. Source review and bounded tests do not establish native scheduling, full module compatibility or performance.

Part331 reuses existing ProcessRuntime.SpawnTraced, ModuleRegistry, immutable terms and mailbox behavior rather than introducing an actor framework or scheduler. Narrow existing friend-assembly declarations grant the OTP assembly access to the internal spawn path, preserving atomic link and trace descriptions without exporting hidden init_p APIs. Pinned proc_lib.erl guides the adaptation; no new external package or license decision. Crash reporting is explicitly deferred.

Part330 reuses existing public GeneratedModuleCompiler, CompiledModuleExecution, CompiledModuleProtocol and OracleResultProtocol helpers through binary references to the separately identified329 subject. No new NuGet package, runtime dependency or production BEAM dependency is introduced. Pinned OTP remains a development oracle only; third-party notices and uncommitted owner-license choice remain unchanged.

Part 329: the CLR source evaluator checks physical stack availability at named and anonymous function entries and yields on a low-stack result without charging guest reductions; halt/cancellation and the existing source-image lease/owner flow remain in force. Actual bounded compiled4096, retained original input, module-null interpreted and lifecycle controls, fresh Main6747/11, post-full SDK native roles and readiness509 total/351 erlang are mandatory evidence for this slice. Registered MFA rows and all thirty prior custom Limits remain unchanged. This does not establish arbitrary AST/term/pattern or all host-context/completion/unwind stack safety, nor native physical layout, scheduler or performance parity. The existing BCL mechanism is reused without a new third-party component; selected prior328 all-five typed proofs remain separate from future original329 observations.

Part 328: deferred compiler-source and unresolved restored local fun invocation selects the executing runtime and its actual undefined_lambda/3 with the original restored wrapper and evaluated arguments only when no CURRENT defining code can resolve the fun. Stable per-key immutable publications, Volatile validation and typed retry retain existing lock order and normal source leases; named/anonymous and module-null factory paths validate before invocation. The public six-argument source admission preserves old undef. Actual focused eight generated fixtures/twelve HOST selectors (seven feature controls plus five unchanged legacy regression groups), historical327 public and generated undef, Main6738/6738 and eleven integrations, three fresh post-full Windows SDK roles and readiness CLI exit0 against immutable327 are required and bound by this entry. Initial first-fixture stack-overflow is retained; a tail-recursive fixture helper is not a generic production stack-safety fix. Fresh CURRENT without maker factory, direct foreign captured-image funs, default autoload, raw BEAM, full native continuation/stack parity and performance remain incomplete; original328 comparison remains pending. Registry509/previous509/delta0 and351erlang is presence only. Published327 all five typed scopes pass, including5925/fd8 with a separate strict map-stdout refusal and no raw normalization.
 First actual Main1 failed6733/6738 on five old legacy factory timing assertions, reproduced using its exact captured Runtime; a narrow public Register HOST-factory eager Restore split preserves exact legacy return objects outside Functions gate while compiler source factories remain deferred. Compiler source factories never construct during decode. Legacy public HOST factories retain existing selected decode or owner-preparation timing outside gates; no new ingress path or test weakening is introduced. First failed full and same-DLL RED remain historical; only newly bound corrected Main gates are publication authority.

Audit date: 2026-10-09. Reuse is accepted for specific primitives, never as an assertion that a CLR library reproduces the entire Erlang contract.

| Candidate | Source/license evidence | Decision and validation |
|---|---|---|
| BigInteger | [runtime source](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Runtime.Numerics/src/System/Numerics/BigInteger.cs), .NET MIT license | Reuse arbitrary precision arithmetic, explicit sign/endianness for ETF. Local boundary and 200 deterministic integer roundtrip cases. Numeric comparison has an Erlang-specific rational implementation. |
| CLR async/Tasks | SDK/runtime APIs, installed .NET license/third-party notices | Reuse continuation scheduling and wakeups; explicit ProcessContext models lifecycle. 1,000 suspended processes tested. No assertion of BEAM fairness or preemptive execution. |
| Channels | [official API](https://learn.microsoft.com/dotnet/api/system.threading.channels) | FIFO consumption does not implement selective receive. Not used as mailbox storage. Custom scanning and preservation are tested. |
| Roslyn | Pinned SDK's Roslyn/bincore assemblies, product 5.9.0-1.26379.115; [upstream MIT license](https://github.com/dotnet/roslyn/blob/main/License.txt), installed SDK library license/notices | Reuse C# lexical boundaries. Erlang grammar and semantics use dedicated C# parser/analyzer. Tested raw strings, comments, ordinary fun calls and C# switch coexistence. SDK direct references are provisional until independently versioned compiler packaging. |
| Akka.NET | [ActorCell source](https://github.com/akkadotnet/akka.net/blob/dev/src/core/Akka/Actor/ActorCell.cs), [mailbox docs](https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/actors/mailboxes.md), [Apache-2.0 notice](https://github.com/akkadotnet/akka.net/blob/dev/LICENSE) | Not adopted as Erlang runtime. Default mailbox/unhandled-message and parental-supervision contracts do not establish Erlang selective receive, links or monitors. Inspected upstream material; no pinned adaptation or differential compatibility evidence. |
| saleyn/otp.net | [upstream repository](https://github.com/saleyn/otp.net) | Investigated as transport/term candidate. README says it is being phased out toward NFX.Erlang. Exact source/license retrieval was incomplete; modern OTP 29 protocol and signed-zero/map contracts not verified. No code copied; retain as a candidate for distribution audit. |
| takayuki/Erlang.NET | [upstream repository](https://github.com/takayuki/Erlang.NET) | Historical jinterface-derived implementation; repository has EPLICENSE and no published release visible in inspected snapshot. Source/maintenance/OTP29 contract audit incomplete. Not imported; do not infer current compatibility from historical README. |
| Orleans, Pipelines, Immutable collections | No component-level audit yet | Not adopted. Investigate only when an applicable component is next, then record source/license and behavioural tests. |

No third-party actor or ETF code was incorporated. New implementations were written against the observable contracts of the pinned reference. Existing candidates still need a deeper source and maintenance audit before future reuse. There is no live differential verification of any candidate at this checkpoint.

Part 004 map-source audit: reused the existing MapTerm exact-key equality/order and BCL Dictionary storage, whose primitive reuse is already recorded above. Inspected MapTerm/Pattern source and the pinned OTP expressions manual plus lib/compiler/test/map_SUITE.erl. General .NET dictionaries do not supply Erlang syntax, key-binding scope, duplicate pattern clauses or guard-failure semantics; these were implemented in the existing C# parser/analyzer/evaluator. No new package or third-party implementation was imported, and no reference code was copied. Local regressions establish the tested subset; exact-version oracle comparison remains unrun.

Part 005 map-BIF audit: inspected pinned erts/emulator/beam/erl_map.c (map_size_1, map_get_2/maps_get_2, is_map_key_2/maps_is_key_2) and lib/stdlib/src/erl_internal.erl guard/type-test declarations. Reused MapTerm.Get/TryGet/Entries and CoreModules.Boolean. No external package is necessary for these existing term operations; no code was copied. The compiler guard whitelist exposes only the audited erlang functions, not arbitrary maps module calls.

Part 006 bit-construction audit: reused immutable BitString and BCL BigInteger bit operations. Inspected the pinned Bit Syntax Expressions manual, lib/stdlib/src/erl_bits.erl default/merge rules and HOWTO/INSTALL.md headless dependency guidance. No external serializer implements the source size/unit/evaluation contract directly, so construction was implemented in the existing compiler/evaluator. No new package or copied source; no performance equivalence claim. BCL shifts implement low-bit truncation explicitly, with Erlang sizes/byte order validated separately.

Part 007 bit-BIF audit: inspected pinned erts/emulator/beam/erl_bif_guard.c bit_size_1/byte_size_1, erl_bif_op.c is_bitstring_1 and erl_internal guard declarations. Reused BitString.BitLength and type identity; byte-size arithmetic uses a long intermediate to avoid overflow when rounding. No new dependency or copied implementation; current int-sized representation remains narrower than all BEAM resource limits.

Part 008 oracle-transport audit: inspected pinned erts/preloaded/src/init.erl native argument decoding, lib/stdlib/src/unicode.erl characters_to_list (UTF-8 binary decoding), erl_scan:string, erl_parse:parse_exprs and erl_eval:exprs/new_bindings. Reused BCL Encoding.UTF8/Base64 and the oracle's public scanner/evaluator; no dependency or copied source. Unicode source is encoded separately from the ASCII native command, so locale/native-name decoding cannot corrupt it. This code is development test infrastructure only; production remains independent of Erlang/BEAM.

Part 009 bit-pattern audit: inspected pinned expressions.md bit syntax value/guard-size/unit rules and lib/compiler/test/bs_match_SUITE.erl size_shadow_1/2/3. Reused immutable BitString, BigInteger, existing transactional Pattern.Match and synchronous guard evaluation. External actor/serialization candidates do not implement source-pattern binding rules, so matching extends the current C# compiler. No package or copied implementation. Binary slices currently copy and integer extraction shifts per bit; no performance equivalence claim. General pattern scope and all reference-suite cases remain unverified.

Part 010 fun-scope audit: inspected pinned expressions.md Fun Expressions shadow/local-binding contract, existing clause validation, transactional selection and closure capture. Reused those C# components with a fresh captured scope per attempted clause. The old union of every clause's head names removed captures needed by a different clause. No package, copied implementation or new MFA; full named/external fun and scope/oracle coverage remain pending.

Part 011 bit-size grammar audit: pinned lib/stdlib/src/erl_parse.yrl permits prefix_op expr_max for segment values, but only expr_max for segment sizes. Reused existing primary-expression parser and diagnostics; unparenthesized prefix sizes now fail compilation, while parenthesized unary sizes retain runtime validation. No dependency or copied grammar implementation. Real remote report establishes 52 tested comparisons only; later size BIFs, patterns and capture regressions were not reached.

Part 013 float-segment audit: inspected pinned system/doc/reference_manual/expressions.md, lib/stdlib/src/erl_bits.erl, erts/emulator/beam/erl_bits.c (erts_bs_put_float/erts_bs_get_float_2 and binary16 rounding), erts/emulator/beam/big.c (big_to_double), lib/compiler/src/v3_core.erl (integer literal coercion), erts/emulator/test/bs_construct_SUITE.erl fp16/coerce_to_float and bs_match_misc_SUITE.erl. Sizes are 16/32/64 for construction; the matcher additionally accepts zero-width positive zero. Nonfinite decoded values fail matching. Narrowing overflow may produce infinity bits in a binary, as demonstrated by the pinned fp16 test, while term floats remain finite. Integer coercion rounds to nearest, ties to even; a raw .NET BigInteger cast truncates and is unsuitable here.

Reused BCL BitConverter/BinaryPrimitives and System.Half. Inspected the actual [v10.0.0 Half source](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Half.cs): the double operator rounds directly with a sticky bit, avoiding double-to-single-to-half double rounding. .NET MIT notices already apply; no new package or copied implementation. Added explicit integer rounding using existing BigInteger primitives and Math.ScaleB. Permanent checks include all 63,488 finite half encodings, direct-rounding traps, integer ties/sticky bits and the largest finite double boundary. Current bit extraction copies/shifts; no performance equivalence claim. Selected remote float comparisons are pending at this implementation checkpoint.

Part 014 UTF-segment audit: inspected pinned expressions.md Unicode segments/string sugar, erl_bits.erl check_size_unit_1, erl_bits.c UTF8/UTF16 construction and matching helpers, and erts/emulator/test/bs_utf_SUITE.erl roundtrip/illegal-sequence/unaligned contracts. Valid values are integers 0..D7FF/E000..10FFFF, including noncharacters; surrogates, negative/oversized values and nonintegers are invalid. UTF8 ignores endian, UTF16/32 obey it, and explicit sizes/units are prohibited. Reuse [System.Text.Rune](https://learn.microsoft.com/en-us/dotnet/api/system.text.rune?view=net-10.0) scalar validation, EncodeToUtf8 and status-based DecodeFromUtf8/DecodeFromUtf16, plus audited BinaryPrimitives for byte order. BCL MIT licensing/notices already apply; no package or copied decoder. Accept only OperationStatus.Done, never the replacement value returned on invalid/incomplete decoding. Compiler binding/rollback and bit offsets remain custom Erlang logic. This audit precedes the implementation; actual regression/oracle scope is recorded in the part checkpoint.

Part 015 shared numeric-coercion audit: before implementation inspected pinned erl_arith.c mixed plus/minus/times/division and big.c big_to_double. Mixed arithmetic and integer / convert each operand to a finite double first, using nearest-even rounding; a nonrepresentable operand raises badarith even when the final mathematical result would be zero. CoreModules still used raw BigInteger casts, independently of the part 013 bit-segment fix. Reused that already audited rounding algorithm in Integer.TryToDouble and applied it to both runtime arithmetic and compiler float segments/literals. Failure returns false; call sites keep their own badarith/badarg/compile-diagnostic contract. No new package/license/copied implementation; exact numeric comparison remains rational and is not changed to rounded comparison. Six source/error/guard/direct boundary regressions and six oracle additions test this specific gap; complete arithmetic still needs full input/error/stack coverage.

Part 016 UTF-size audit correction: exact run 37953748714 stopped at explicit :undefined UTF size. Pinned erl_lint.erl bit_size/pat_bit_size and erl_eval.erl check_command reject it before evaluation: noninteger explicit expressions become unknown, which erl_bits rejects for UTF. The undefined forms accepted by the lower-level erl_bits helper are internal default/compiler representations, not authorization to accept this source form. Removed the parser normalization, replaced the invalid positive test with construction/pattern diagnostics for all three UTF types and corrected the corpus. No dependency or copied source. Earlier part 014/015 entries remain dated history; current docs use the corrected source contract.

Part 018 readability/reuse audit: reused the installed .NET SDK Roslyn whitespace formatter and existing Roslyn assemblies for a transient source-token/value comparison. No new package, copied implementation or license change; existing .NET/Roslyn MIT notices apply. Solution-mode formatting encountered a sandbox MSBuild named-pipe permission error; folder-mode whitespace formatting avoided workspace loading and passed, followed by verify-no-changes. Reviewed constant replacements by domain meaning, including equal-value contracts kept distinct. The transient audit evaluates the actual compiled const fields and compares normalized token streams against parent 2e1997abfed427c03e73d672364371b94fd3afe1; comments/whitespace are ignored. It is value-preservation evidence, not full semantic/reference compatibility proof. The native-Erlang hybrid Program.cs was excluded from the C# formatter and validated through the existing build pipeline.

Part 019 compiler-constant audit: inspected existing lexer/parser/semantics/bit-pattern/hybrid diagnostics and their literal fixture expectations. Reused existing C# const fields, unchanged interpolation and the installed SDK Roslyn formatter/token parser. No new semantic algorithm, dependency, copied source or license change. The transient token audit expands actual compiled values on both baseline/current sides and inlines ten diagnostic builders with original argument syntax; four existing implementation token streams match. BitSyntaxConstants additions and new diagnostic/token catalog definitions are separately reviewed, not claimed as matched baseline files. Grammar, source specifier values, diagnostic codes/text/offsets and source-unit limits remain unchanged; no pinned OTP semantic re-audit or oracle run is required for this organization-only change.

Part 020 file-layout audit: reused installed SDK Roslyn to identify namespace-level declarations and preserve their original syntax rather than split with textual brace searches. No new dependency/copied implementation/license change; existing MIT Roslyn audit applies. The transient splitter checks target paths/collisions, copies original imports, leaves nested types intact and preserves top-level statements. An initial Windows separator mismatch in its same-file collision check stopped at the unchanged CSharpFeatures original; the three task-generated helper outputs were removed, the path comparison corrected to absolute normalized paths and the splitter rerun. Final verification reparses the Git baseline and all current ordinary source files after formatting, matching namespace/type keys and declaration token SHA256 values. Native-Erlang Program.cs has no namespace-level type and remains handled by the hybrid pipeline. No semantic algorithm or OTP contract changes; reference semantic re-audit was not required.

Part 021 tooling/reuse audit: reviewed installed pinned SDK Roslyn compiler/workspace/formatter assemblies and reused AdhocWorkspace, MefHostServices and Formatter.Format without MSBuild workspace loading. The development-only Erlang.Style project references SDK DotnetTools/dotnet-format assemblies and their System.Composition dependencies; no external package, copied implementation or production runtime dependency. Existing Roslyn/.NET MIT notices apply. Added syntax-aware trivia edits for repository rules and indentation seeds for wrapped lists; standard folder CLI formatting did not indent those newly wrapped lists correctly. The style tool reaches a fixed point and checks it; an independent malformed fixture fails before formatting and passes afterwards. .gitattributes fixes C# LF checkout endings. Only the pinned SDK was validated.

Process/OTP/ETF/compiler/tool/oracle constant replacements reuse existing domain contracts and preserve compiled string values; no semantic algorithm change or new pinned-source claim. A transient token audit reads Git stdout/stderr concurrently to avoid pipe backpressure from numerous CRLF warnings; the blocked prior read was interrupted and corrected. The final audit covers 71 existing C# files, expands 74 additional constant references and substitutes diagnostic builder arguments, with zero mismatches. Added error-reason catalog fields and new tool/catalog definitions are reviewed separately. Historical changelog migration is verified by reverse-link rebasing and exact part-body/order comparison. Full local build/integration and style probes provide current evidence; historical oracle scope is unchanged.

Part 022 ETF numeric audit: inspected pinned OTP-29.1.1 erts/emulator/beam/external.h before extracting tags; verified all 27 actually used tag constants plus VERSION_MAGIC against the macro values. Reused existing BinaryPrimitives, BigInteger, Encoding and ZLibStream implementation unchanged; no package, copied implementation or license change. Semantic constants separate wire tags/header/field layout from local resource bounds and compression buffer tuning. Actual compiled values expand to the identical original token stream across ExternalTermFormat.cs (97 extra references); existing literal ETF vectors/random roundtrips/invalid-input tests remain independent. No new semantic algorithm or reference-suite claim. Initial package consumer build stopped when the packaged tool child exited 1; a separate concurrent PowerShell startup reported CoreCLR GC initialization failure. Exact cause was not established; the full script retry passed without source changes. Saved initial-failure metadata separately from the successful reports.


Part 023 layout audit: reused the existing SDK Roslyn SwitchExpressionSyntax, trivia insertion and Formatter.Format pipeline; no new library, copied implementation, production dependency or license change. Existing .NET/Roslyn MIT notices apply. Reviewed all three compact switch expressions and kept their token streams unchanged. An independent guarded-pattern fixture demonstrates check failure, formatting repair and fixed-point success without changing string literals. A transient SDK token audit separately confirms three files against 90cfc50; the intentional style-tool rule is excluded from token equality and covered by the probe. Its first concurrent dotnet run build exited 1 without compiler diagnostics; sequential dotnet build -m:1 and no-build audit succeeded without edits. Cause was not established. Full local validation passed on its first run; no oracle rerun or pinned-source semantic claim is needed for whitespace changes.


Part 024 string segment audit (2026-10-10): inspected pinned OTP-29.1.1 lib/stdlib/src/eval_bits.erl eval_field/make_bit_type/eval_exp_field and erl_lint.erl good_string_size_type. Size is evaluated once, reused for each codepoint, and an empty string validates scalar zero but emits no bits. Non-default non-UTF strings are disallowed in patterns. Reuse existing C# Cons/BigInteger, BitConstruction integer packing and BitFloat IEEE encoders; BCL byte/float primitives cannot replace Erlang string evaluation and pattern contracts. No new dependency/copied source/license change. SDK Roslyn stays the existing development/compiler tooling. Add string-origin metadata to preserve the AST distinction and generated code; keep ordinary list-valued expressions scalar and invalid as numeric segments. Pattern errors remain explicit. Local and pinned-oracle validation are recorded at the checkpoint.

Part 025 correction audit: exact-version run 37998319734 built OTP successfully and matched 174 cases, then aborted before evaluation on static <<"":all>>. Re-read pinned erl_lint bit_size/bit_size_check: literal all is legal only for binary type; variable sizes resolving to all remain runtime checks. Reuse existing parser literal/type classification, with a domain-owned size atom and diagnostic; no copied source/new library/license change. Preserve initial artifact/head/counts and replace the illegal runtime fixture with a dynamic-size fixture. This is a concrete lint mismatch correction, not an oracle infrastructure success or completed 183-case result.

Part 026 evidence audit: reused the existing pinned development-only workflow and SDK Roslyn syntax parser for corpus/layout provenance, with no production/runtime changes, package or copied implementation/license change. Downloaded raw oracle/CI artifacts; verified source build, exact heads, reported baseline/version, completeness, counts and ZIP hashes. The first transient corpus helper build lacked the Microsoft.CodeAnalysis extension-method import; fixed before final audit. A transient layout dotnet run build exited 1 without diagnostics; explicit sequential dotnet build -m:1 and no-build run passed without source edits, cause not established. Final audit confirms previous 160 sources plus 23 current additions and 117 individually named namespace types. Broad compatibility stays partial despite selected oracle success.


Part 027 lists/report audit (2026-10-10): inspected pinned lib/stdlib/src/lists.erl nth/nthtail/seq clauses and current CoreModules/Cons/registry plus explicit-export/BIF inventory. Enumerable.ElementAt/Skip/Range are unsuitable for arbitrary BigInteger indices, improper-list prefix/tail rules and OTP-specific function_clause versus badarg errors. Reuse existing immutable Cons and BCL BigInteger primitives; implement guarded traversal/sequence construction in original C#, no OTP implementation text copied or new dependency/license change. Sequence length is limited to int.MaxValue in this CLR implementation; larger requests give system_limit and full resource equivalence remains pending. Readiness uses actual registered MFAs/direct passed tests joined to pinned explicit exports plus BIF declarations, with source-inventory limitations exposed. API presence ratios are distinct from semantic compatibility and component milestones; do not invent a weighted module completion percentage.

Part 028 reuses the existing pinned workflow, BCL artifact/hash tools and SDK Roslyn corpus parser to verify the complete 219-case/same-head CI evidence. No executable implementation, library, production dependency, copied reference implementation, public package or license change.

The long-lived gen_statem loop uses opaque source-wrapper custody: C# fields retained across awaits must not be treated as compiler-owned lexical roots without a separate borrowing/GC proof. Source execution frames are still explicit and baseline-bound. The focused validator uses authentic original feature configuration and an actual original init/loader/code_server bootstrap; it does not replace dependency failures with expected answers. Both implementations pass74 whole API observations and311 engine scenarios, including complete error stacks and sys exactly-one termination. Preserve full upstream, owned/resource/platform/distribution/log-handler/metadata acceptance as separate gates. No full regression or canonical readiness refresh is inferred from these reports; see validation/part-579/README.md.
## Part 029 — Lists reverse/key-search source and reuse audit

Read pinned lib/stdlib/src/lists.erl reverse/1 clauses and reverse/2/keyfind/keymember/keysearch BIF declarations, erts/emulator/beam/erl_bif_lists.c keyfind and erl_term.h SMALL_BITS/MAX_SMALL. reverse/2 already exists; reverse/1 has a short-invalid-input reason mismatch. Key search skips non-tuples/short tuples, stops before an improper suffix after a match, validates positive small-integer position first and otherwise reports badarg. The 64-bit reference uses 60 signed small bits (maximum 2^59-1). Its small-integer Key/float-element fast path compares the rounded double, whereas other numeric terms use general comparison; preserve that deliberate path separately from exact membership/map keys.

Considered existing LINQ Reverse/Any and BCL collection searching: these do not preserve reverse/1 clauses, tuple-position checks, early improper-tail termination or the native key comparison path. Reuse immutable Cons/TupleTerm, existing numeric comparator and audited Integer.TryToDouble with original iterative C#. Microsoft BCL/SDK are existing dependencies; no new library, copied OTP implementation, production BEAM dependency or license change. The position bound is explicitly the pinned 64-bit reference contract, not a general .NET collection limit. Permanent tests and live selected reference cases will verify both numeric directions and boundaries.

Part 030 reuses existing pinned workflows, SDK Roslyn corpus parsing and BCL JSON/hash tools for complete 249-case/same-head CI provenance. No new library, copied implementation, production dependency or license change. Reviewed existing OracleProtocol/Parser module entry points for the next separate compiled-reference-module track; no implementation or result is claimed for that track.

## Part 031 — Compiled-module oracle and Roslyn reuse audit

Inspected pinned compiler compile:forms/2 (binary output, return_errors/return_warnings) and kernel code:load_binary/3, existing OracleProtocol Unicode/Base64 transport, CodeGeneration.CompileModule and ModuleDefinition.Register. Current .NET SDK/Roslyn is already used by the compiler/style tools (Roslyn open-source MIT license; installed Microsoft .NET library terms are separate, existing notice scope unchanged). Reuse those SDK assemblies and BCL collectible AssemblyLoadContext rather than introduce a scripting/compiler package, shell temp-source quoting or a production BEAM dependency. Compile source forms to a reference BEAM binary with normal optimizations, load it in a fresh development oracle process and invoke run/0; generate C#, emit an actual .NET assembly and retrieve its ModuleDefinition in an unloadable context. Original fixture modules and independent expected values remain test data, not copied OTP implementation. Report expression and compiled-module tracks separately, with source/generated hashes and compile failure infrastructure evidence; preserve all 249 expression cases. Default compiler optimizations are tested only on selected fixtures, not claimed fully equivalent.

Part 031 local SDK/style and restore initially failed with OutOfMemoryException before new code compilation. Successful retry used only process-local processor/node-reuse settings; cause was not established and unrelated host processes were not stopped. Permanent generated-assembly tests and full validation passed; remote checks remain pending.

Part 031 license/version evidence: installed CSharp Roslyn file version 5.900.26.38015, product 5.9.0-1.26379.115+14fbf8d5271c98133561eb55185fdb05b286f578. Read C:/Program Files/dotnet/LICENSE.txt (Microsoft .NET library terms) and the [official Roslyn project license](https://github.com/dotnet/roslyn/blob/main/License.txt) (MIT). An exact product-source SHA license fetch was unavailable; repository license and installed-binary version evidence are kept distinct. Internal SDK reuse adds no new production distribution or library license choice.

Part 032 reuses the existing pinned workflows, SDK Roslyn syntax parsing/code generation and BCL JSON/SHA tools to preserve 249-expression/14-module/same-head 361-test evidence. No new dependency, copied reference implementation, production BEAM requirement or license change.

## Part 033 — Bounded immutable atom reuse and protocol constants

Inspected Atom.Name (read-only string), TermOrder name equality/hash, parser and ETF factory calls, and pinned OTP-29.1.1 erts/emulator/beam/atom.c: erts_atom_put_index uses atom-table lookup/insertion and erts_atom_put returns its identity. No OTP implementation is copied. The CLR representation continues to compare atom names, not object identity; it does not implement the full BEAM atom table or its resource limits.

Reuse the installed .NET BCL FrozenDictionary with ordinal string keys for ten explicitly selected common names. Fixed initialization and read-only access provide concurrent sharing without locks or retaining arbitrary application input. String.Intern or an unbounded ConcurrentDictionary would retain every encountered name and is rejected for this scope. Weak/global LRU interning adds unnecessary policy/overhead for the requested common values. No new dependency, license choice or public package; existing BCL/SDK license decisions above apply. CommonAtomNames describes value spellings only; independent domain constants (I/O success, evaluator empty sequence, oracle outcome) retain their distinct semantic names. Selected object identity/allocation checks are correctness evidence, not a measured throughput claim.

The SDK-backed style formatter also needed a documentation-trivia fix: insertion uses the full comment boundary including ///. Reuse existing Roslyn APIs; the development test harness references the existing style executable for permanent comment-preservation/idempotence regressions. No production dependency on the style tool or BEAM is introduced.

## Part 034 — Compiled bit evaluation and closure fixture expansion

Inspected pinned OTP-29.1.1 lib/stdlib/src/eval_bits.erl expr_grp/expr_grp1/eval_field: field value and size expressions are evaluated in source order before binary creation/type validation; modified string sizes share this staged path. Also inspected lib/compiler/test/bs_construct_SUITE.erl side_effect and bad-size sections, and local Execution/BitConstruction/Semantics closure handling. Fixtures are original small modules, not copies of the OTP suite. Compare actual generated C# Release assemblies with default-optimized compiled BEAM, since erl_eval evidence alone cannot establish compiled outcomes.

Reuse existing parser/evaluator, process dictionaries for observable order, immutable terms, SDK Roslyn compilation and fixed-baseline oracle protocol. No candidate external runtime library improves this focused semantic comparison; .NET expression/JIT evaluation order is not substituted for the Erlang contract. No new dependency, copied implementation or license choice; existing SDK/BCL license evidence applies. Independent expected payloads remain literal fixture data; success/failure wrappers reuse their own domain constants. Resource/stack/optimizer equivalence beyond selected inputs remains unverified.

## Part 035 — Construction binding scope correction from aborted oracle

The part034 reference run38014541956 atc9ca96a confirms249 expressions and19 modules, then aborts compiling the invalid sibling binding fixture with erl_lint unbound_var S. Complete=false,268 executed; this is not275/275 proof. Read pinned erl_lint.erl expr_bin/bin_element: every field value and size is checked against the same incoming variable table, then bindings are merged/exported after the binary. Local Semantics had incorrectly propagated bindings from one field into sibling expressions.

Reuse HashSet scope snapshots and existing ERL006 diagnostic for independent value/size checking, merge exports afterward. Keep runtime evaluation order separate from compile-time visibility; pattern segment binding order remains unchanged. Move the invalid fixture into a dedicated compiler-diagnostic track and use a valid exported-after-binary module plus independent-binding/prebound positive cases. Reference compile errors return normalized reason terms; scanner/parser/process failures still abort as infrastructure. Compare specific OTP unbound_var reasons and independent C# code/message expectations, not merely any rejection. Existing SDK/BCL/compiler infrastructure suffices; no new dependency, copied code, production BEAM requirement or license choice.

Part036 reuses the existing pinned three-track workflow, BCL JSON/SHA/artifact tools and independent fixtures for complete282/282 corrected-head proof and same-head389-test CI. No executable source, external dependency, copied implementation, production BEAM requirement, public package or license choice changes in this evidence checkpoint.

## Part 037 — Audited last and split list traversal

Read OTP-29.1.1 lib/stdlib/src/lists.erl last/1, last/2 and split/2, split/3. Reuse existing immutable Cons/Nil, arbitrary-precision Integer and ListOperations.Reverse; implement iterative traversal. BCL LINQ Last/Take/Skip are candidates but cannot preserve Erlang badarg versus function_clause, arbitrary improper tails or original suffix identity without a semantic adapter. Existing traversal primitives suffice; no external package, copied reference code, production BEAM dependency or license change. The reference is the contract, not a source transplant. Big indices are not narrowed; list length bounds traversal. Resource exhaustion and reference stack/annotation equivalence remain outside this part.

Part038 reuses pinned workflow, SDK Roslyn source/hash/layout audits and BCL JSON/SHA artifact tools for complete304/304 reference and same-head414-test CI. No new library, copied reference implementation, production BEAM dependency, public package publication or license choice. A direct dotnet run build of the optional layout helper failed without useful captured diagnostics; explicit serialized build -m:1 -nr:false then --no-build execution passed, with136 types and no violations. Root cause remains unconfirmed; mandatory production pipeline and remote CI passed.

Additional existing C# reuse audit: src/Runtime/Erlang.Terms/Collections/Lists/Cons.cs exposes immutable Head/Tail, but Cons.Items enforces proper termination with badarg. Using it for last would change function_clause, and enumerating a suffix for split would destroy the intended arbitrary-tail boundary. Dedicated traversal therefore reuses the term representation and Reverse, not the proper-list-only enumerator. Integer.Value is BigInteger and retains unrestricted count arithmetic.

## Part 039 — append/1 and existing member/2 contract audit

Inspected pinned lists.erl append/1 singleton/general/empty clauses and erts/emulator/beam/erl_bif_lists.c lists_member_2. member/2 already exists in CoreModules as Cons.Items(...).Any(x => x.Equals(key)); Term.Equals delegates to exact TermOrder comparison. Existing lazy enumeration preserves early match before an improper tail and badarg on exhausted malformed input. Retain that implementation and add nested exact-equality/early-match regressions instead of counting an existing MFA as new.

For append/1, reuse immutable Cons/Nil and existing Cons.From/Items as the append/2 operation; collect outer prefixes first, then fold right to left with the last element untouched. This preserves recursive reference error ordering without CLR recursion. Cons.Items alone is unsuitable for the outer spine because its malformed-tail badarg differs from required function_clause; flattening via LINQ SelectMany also rejects or flattens arbitrary final tails. No new dependency, copied reference code, production BEAM or license change. Resource/reduction fairness and error stacks remain unfinished. Names for append/member move into their list-domain catalog without conflating atom meanings.

Part040 reuses the pinned oracle/Windows CI workflows, SDK source/generated/layout audits and BCL JSON/SHA artifacts for complete330/330 and same-head442 tests. Existing member/2 remains unchanged apart from named export spelling; append/1 is the sole new MFA. No external dependency, copied reference implementation, production BEAM, public NuGet or license change. Source representation/resource/reduction/stack limitations remain explicit. Optional audit helpers built explicitly with -m:1 -nr:false before --no-build execution; no transient helper failure in this checkpoint.

## Part 041 — Duplicate and nested flatten contracts

Read pinned OTP-29.1.1 lists.erl duplicate/2, duplicate/3, flatten/1, flatten/2 and do_flatten/2. Reuse immutable Cons/Nil, Integer/BigInteger and BCL Stack/List representation. Inspected existing Cons.From/Items: proper-list-only enumeration raises badarg and cannot preserve flatten function_clause; LINQ Repeat requires int count and recursive SelectMany neither models nested term lists nor preserves arbitrary opaque leaves and unflattened final tail. Independent iterative constructors and explicit list-spine frames are appropriate; no external package, copied reference implementation, production BEAM or license change.

Flatten uses tail-first explicit traversal to match reference recursion while avoiding CLR recursion; only Cons/Nil heads are flattened, opaque terms remain shared, and flatten/2 accepts a Cons or Nil tail (including improper Cons) without traversing it. Duplicate retains the original element objects. ListDuplicateLimits.MaximumLength=int.MaxValue is a separately owned local allocation bound, not shared with ListSequenceLimits just because values coincide. Counts above it raise local system_limit before narrowing/allocation; this is not a claim about pinned OTP resource equivalence. Do not execute huge positive reference allocations. Resource exhaustion below that bound, reductions/fairness and error stacks remain unfinished.

Part042 reuses pinned workflows, SDK source/generated/layout audits and BCL JSON/SHA artifact tools for complete355/355 and same-head472 tests. No dependency, copied reference implementation, production BEAM, public package or license change. Duplicate local length/resource guard is not promoted to reference compatibility. Explicit serialized audit builds and --no-build execution passed;137 namespace-level types have no filename/multiple-type violations.

## Part 043 — Constant catalog extraction

Reuse existing SDK Roslyn syntax APIs (previously audited MIT) and BCL JSON/file/process helpers for a development-only token/value audit. No candidate implementation/package is needed for semantic names; no new dependency, copied OTP implementation, production BEAM, public NuGet or license change. Catalog ownership is reviewed per call/pattern/grammar context rather than merging by equal text. Full472-test integration and unchanged322/28/5 corpus/generated hashes support this representation-only change; historical pinned proof remains scoped to3c138b2. An ignored audit helper initially lacked the Microsoft.CodeAnalysis extension-method import; repaired helper build/audit passed, production build/tests did not fail.

## Part 044 — If expression implementation audit

Read pinned OTP-29.1.1 erl_eval.erl if_clauses/guard/guard0/guard1 and erl_lint.erl if/icrt_clauses/icrt_clause/icrt_export, plus erl_parse.yrl if_clauses grammar. Reuse existing C# Clause (empty pattern list), guard alternatives, ordered Execution.Select and branch scope merge/tail-call machinery. No actor/parser package reproduces these contracts; existing SDK Roslyn is only a C# lexical/code-generation primitive (previously audited MIT), not Erlang semantics. Keep production independent of BEAM; use exact pinned development oracle for values, errors and compiled behavior. No external dependency/copied implementation/license change. New Expr.If owns its AST identity; error/diagnostic text belongs in existing domain catalogs. C# if statements must retain their syntax; hybrid detection inspects candidate guard arrows at top-level before lowering.
Part045 reuses pinned oracle/CI workflows, SDK audits and BCL JSON/SHA provenance tools for392/392 and same-head516 tests/full integration. No external dependency, copied reference implementation, production BEAM, public package or license change. Preserve original corpus and independently specified new outcomes rather than treating oracle/implementation agreement alone as fixture correctness. Local part044 full validation remains separately scoped.

## Part 046 — Begin and expression operators audit

Read pinned OTP-29.1.1 erl_parse.yrl ordinary prefix/mult/add/comparison/list/catch operator grammar and inop_prec/preop_prec; erl_eval.erl block/catch/operator evaluation, short-circuit values and independent strict operand bindings; erl_lint.erl block/catch/short-circuit unsafe/export rules; erts/emulator/beam/erl_arith.c integer bitwise and signed/large shifts. Reuse existing AST/evaluator, BigInteger infinite signed bitwise primitives and arithmetic right shift (BCL/.NET MIT previously audited). No parser/actor package models these language contracts; SDK Roslyn remains only lexical/codegen infrastructure. No external dependency/copied OTP implementation/production BEAM/license change. Named precedence and CLR bit-index resource bounds have separate owners. Implement ordinary expression operators; record access/construction, generators and maybe ?= are construct-specific grammar, not standalone ordinary operators. Catch logical stack frames retain actual operation/call arguments but are not full OTP stack/source annotations; exact error reason/class and stack shape are separately tested. Avoid huge positive allocation oracle cases; enormous bignum shifts with guaranteed reference early bounds are safe.
Part047 reuses existing pinned oracle/CI, SDK/BCL source/hash/JSON provenance and readiness tools.484 selected comparisons and same-head614 tests pass; no new dependency/copied implementation/production BEAM/public package/license change. Preserve old contracts and independent expected new outcomes. Catch logical stacks/local shift resource bounds are explicitly outside full OTP trace/allocation proof; local full pipeline remains part046.

Part 048: accept a source-directed Apache-2.0 adaptation of pinned erl_eval expr_list/cons/ordered-dictionary merge and erl_lint independent expression-list walk. CLR Dictionary/HashSet/ValueTask are storage/execution primitives, not replacements for Erlang scope semantics. No compatible drop-in parser/actor library was established by prior candidate audits; use the authoritative baseline rather than adding a package. Preserve Ericsson copyright/SPDX and modification notices; OTP-PORTS.md accompanies both local packages. No production BEAM or public package publication. Name-only lint state and full traces remain explicit deviations.

Part050: inspect pinned v3_core safe_list/iexprs/known-variable group and uexprs match lowering after the observed interpreted-versus-compiled tuple conflict. Reuse BCL/ported exact binding helper with a separate compiled subset adapter; do not change correct erl_eval behavior. Independent closure environments are retained. No complete Core IR/optimizer port is claimed. Keep Apache notices for erl_eval/v3_core and modification/subset labels, include updated register in packages. Fixture correction is dictated by retained reference output, not fitted to current C# behavior.

Part051 reuses existing exact source-built OTP/CI, SDK/BCL hashes and readiness tooling.522/522 and same-head652/full CI verify the selected split binding policies, preserving failed part049 data and an explicit compiled fixture correction. No executable/dependency/license change here. Full Core/lint/grammar/stacks/resources remain unported; existing source notices/port register retained.

Part052: audited pinned erl_eval map update/eval_map_fields, erl_lint map_fields/vtupd_export_expr_list and v3_core expr_map/map_build_pairs_1. Reuse BCL Dictionary/ValueTask and existing source-directed binding adapters; no drop-in package established by previous candidate audits, no new dependency. Retain the privately owned exact-key dictionary already built by MapTerm instead of throwing it away and scanning sorted entries on lookup. The index is not exposed; owned sorted read-only entries still define ordering/printing/hash. Tradeoff: retained dictionary memory per map. This original C# storage optimization is separate from Apache-2.0 source-derived map scope adaptation. Preserve source notices and package register. Cross-runtime performance remains unmeasured; local before/after lookup measurements cannot establish superiority to BEAM.

Part 053 reuses exact pinned source-built oracle/CI, SDK/BCL provenance and readiness tools.543/543 selected comparisons and same-head 676/full CI verify selected map scopes/errors/keys. No new executable dependency/license change or full Core/runtime/OTP/performance claim. Local lookup microbenchmark remains separately scoped part052 evidence.

Part054: inspect pinned eval_bits expr_grp/expr_grp1/eval_field/create_binary, erl_lint expr_bin/bin_element/bit_size, v3_core expr_bin/expr_bin_1/bitstr and emulator i_bs_create_bin/erts_bs_put_float. Reuse existing SDK/BCL Dictionary, ValueTask, BigInteger and bit encoder; no new dependency/drop-in parser or runtime package. Adapt the eval_bits evaluation-before-materialization control flow with Ericsson AB1999-2025 Apache-2.0 notice; compiled path remains the v3_core-guided existing Core subset adapter. Interpreter materializes fields in order, while BEAM bs_create_bin can check sizes before float encoding. Preserve that distinction and avoid enormous allocation probes. Independent function captures require compiled child scopes; full Core known-variable states and JIT/error-info/resource parity remain unported. No performance superiority claim for this semantic increment.

Part056 correction: retained exact part055 output proves binary compiled pre-expressions are sequential (v3_core expr_bin_1 concatenates Eps; bitstr appends value and size pre-expressions). The safe_list independent-child adapter is inappropriate for binaries. Reuse the eval_bits sequential C# loop for both modes; preserve distinct float materialization order. Correct exactly the four newly added compiled expectations to official badmatch99/4/2/99; keep all69 source/generated hashes and all prior63 fixture expectations. No new dependency/copy/license change. Static expr_bin independent incoming Vt must not be generalized to runtime function capture scopes. Preserve failed561/565 evidence and verify a new head.

Part056 extends the correction to compiled maps after pinned expr_map/map_build_pairs_1 audit: sequential base/key/value pre-expressions, unlike safe_list sibling groups. Reuse existing map builder/sequential callback loop; four new independent compiled capture fixtures, existing static/interpreter scopes unchanged. No new dependency/license change.

Part058: inspect pinned v3_core bitstr empty-string branch and eval_bits evaluation/materialization; reuse existing ValueTask, List, BigInteger, AST and bit construction helpers, no new package. Port the dynamic integer/nonnegative precheck and elimination for empty integer/float string segments with full source notices. Preserve independent expectations and historical corpora; validate huge unused size without allocating. Full Core/constant-folding/diagnostic metadata remains partial.

Part060: audit pinned v3_core safe_list, force_safe, ulinearize_exprs and uexprs imatch/known-variable transitions alongside erl_eval expr_list/merge_bindings. Reuse BCL Dictionary, existing transactional Pattern.Match, Semantics.Variables and ValueTask callbacks; no new package/backend. Keep lexical child bindings independent from sibling match constraints to preserve closure capture. Check structural patterns immediately and retain whole RHS failure values; full known-state/FUV/lint/optimizer transformation stays incomplete. Exact prior-source fixture repro and fresh pinned comparison required before promoting proof.

Part062: inspect exact OTP-29.1.1 erl_parse try_expr/try_clause, erl_eval try_clauses/check_stacktrace_vars, erl_lint try exports/catch_clause/taint_stack_var and v3_core try/try_after/try_exception. Reuse SDK/BCL ValueTask, Dictionary, existing AST/pattern/guard evaluator and ErlangException; no new dependency or parser/runtime package. BCL exception handling is an execution mechanism, not Erlang semantics: separate protected body from of/handler execution and put after in an outer finally. Adapt source-directed control flow with Apache-2.0 Ericsson notices. Preserve existing logical stack terms; complete OTP frames, typed lint states, Core lowering and resource parity remain incomplete. Independent fixtures and fresh pinned source-built comparison required.

Part064: reuse the existing Pratt parser/constant-pattern converter after rereading pinned erl_parse pat_expr unary +/- and try optional stack productions; no dependency/algorithm import. The qualifier stop boundary must propagate into prefix operands before :Stack, preserving ordinary module calls and existing bit syntax. Original part063 valid negative-reason probe fails ERL002; retain that source/log, add independent integer/float/parenthesized reason cases interpreted and compiled, preserve all653 reference contracts/generated hashes and verify a fresh head. Existing Apache Parser modification notice applies; no license/backend change.

Part066: exact pinned erl_lint catch_clause/taint_stack_var creates a tainted GuardVt for the catch guard, then checks handler body with untainted Vt2. Existing C# marker leaked into the body, rejecting valid nested guards and fun parameter shadowing. Reuse SDK/BCL HashSet snapshots and existing clause checker; no .NET parser/runtime package or new dependency. Isolate the marker to a temporary guard scope and retain freshness checks. Add independent accepted expression/module regressions plus normalized stacktrace_guard/stacktrace_bound rejection contracts; preserve all659 historical outcomes/hashes. Existing Ericsson AB1996-2026 Apache notice applies. Full typed lint/source frames remain incomplete.

## Part 068 — maybe implementation reuse decision (2026-10-10)

Before implementation, inspected pinned erl_parse maybe_expr/maybe_match_exprs productions, erl_eval maybe_match_exprs/4 and maybe expression clauses, erl_lint maybe/maybe_match checks, and v3_core maybe_match_exprs/3 plus failure continuation lowering. Exact blobs/ranges are retained in part067/next-maybe-source-audit.json and will be copied into the implementation checkpoint. Reuse the existing Pratt parser, immutable Pattern matchers, compiled constraint scopes, async CLR evaluator and generated C# AST representation. BCL Dictionary copies provide explicit private body/else scopes and transactional conditional matching; Roslyn remains the existing hybrid lexical boundary/formatting dependency. No actor/parser package supplies these Erlang mismatch, unsafe binding or compiled pre-expression contracts; no new dependency is needed. Port the source routine's sequence/success/failure control flow, not an exception-catching approximation. Preserve upstream Apache notices for adaptations; original public code license is still unselected. Full Core lowering and annotations/warnings are outside this increment.

## Part 070 — Alias and literal list-prefix pattern reuse decision (2026-10-10)

Before implementation inspected pinned OTP-29.1.1 erl_parse pat_expr '=' and clause pattern productions, erl_eval match1 aliased patterns and '++' nil/cons(integer/char)/string forms, erl_lint independent alias pattern states and permitted prefix forms, v3_core alias consolidation and prefix-to-cons transformation, and the already-audited maybe_SUITE alias/string examples. Reuse Pattern's transactional match boundary and original key/size scope, existing immutable Cons/Pattern.List and parser AST; new AliasPattern is a separate runtime type. Do not export bindings from the left alias into the right's map keys/bit sizes; erl_lint checks both against the incoming scope. CLR collections and the existing Roslyn tools suffice; no new package or parser/runtime framework. Preserve full Apache notice for the source-directed alias matcher and existing parser/lint notices. Full compiler Core alias consolidation/warnings/records and pattern constant-expression folding remain separate work.

Part070 initial fixture correction: three new local failures came from writing Whole=<< or S=<< without whitespace. Erlang's longest-token rule reads =< then <; these are not valid alias/binary sources. Corrected only the two new source fixtures to use '= <<', preserving their intended value/scope contracts. No historical source or expectation and no lexer token rule was changed. Initial failed report/log retained in part070; final validation pending.

## Part 072 — List comprehension reuse decision (before execution implementation)

Pinned OTP-29.1.1 erl_parse.yrl list_comprehension/lc_exprs/lc_expr productions, erl_eval eval_lc/eval_lc1/eval_generator/eval_generate/eval_filter and erl_lint lc_quals/handle_generator/is_gexpr were inspected. v3_core lc_tq/lc_tq1/filter_tq/bad_generator were inspected to distinguish compiled semantics. Reuse the existing parser/AST, transactional Pattern.Match, immutable Cons and CLR Dictionary/List plus ValueTask; these are already licensed/project dependencies. LINQ SelectMany/Where alone does not provide Erlang generator shadowing, key/size input scopes, strict mismatch errors or guard versus ordinary filter failures. No external C# actor/interpreter dependency is selected. Port source control flow into an asynchronous evaluator, with a forward CLR result list instead of OTP's reverse accumulator. Generator traversal must be iterative to avoid stack growth with input length. Full Core optimization, binary/map/zip generators, comprehension assignment feature, warnings/source spans and resource/performance equivalence are outside this part.

## Part 074 — Binary comprehension reuse decision (before implementation)

Inspected pinned OTP-29.1.1 erl_parse binary_comprehension/lc_expr productions, erl_eval eval_bc1/eval_b_generate and eval_bits bin_gen/bin_gen_field1. Reuse existing immutable BitString, BitPattern decoding, BitConstruction and comprehension scope/filter traversal; BCL List/Dictionary/ValueTask and existing Roslyn suffice. No external parser/actor/package supplies the required consuming mismatch, strict remaining-tail errors or guard-filter distinction. Collect result chunks and materialize once, rather than copying a growing accumulator after every result; retain evaluation order and bit alignment. Shared bit-field scanner must preserve ordinary exact matching and distinguish consumed mismatch from exhaustion. Unsupported unsized generator fields are rejected as in erl_lint. Full Core/optimizer, zip/map/assignment forms, resource equivalence and BEAM speed are outside this increment. Consolidated upstream attribution remains in OTP-PORTS.md; do not add source license banners or commit LICENSE.local.txt.

## Part 076 — Map comprehension reuse decision (before implementation)

Inspected pinned OTP-29.1.1 erl_parse map_comprehension/map_field_exact/lc_expr, erl_eval eval_mc2/eval_m_generate, erl_lint comprehension_expr/comprehension_pattern and v3_core mc_pairs/mc_tuples/generator. maps iterator/next/foreach validation was inspected. Reuse transactional tuple patterns, private comprehension traversal, immutable exact-key MapTerm and BCL collections/ValueTask; no external package supplies these scope/error contracts. Templates evaluate each key/value pair with isolated incoming bindings; fields do not share new bindings. Accumulate key/value pairs in source order and materialize once (last exact key wins). Iterators use Erlang terms rather than opaque CLR handles. Default map traversal order is unspecified by maps API; large HAMT traversal/path encoding and custom comparison iterators need explicit boundaries. Existing CLR sorting/terms suffice for ordered/reversed iterators. No new dependency or production BEAM; existing consolidated copyright/register and Apache notice remain, LICENSE.local.txt must stay uncommitted.

## Part 077 — Compiled map template scheduling correction

Exact-source run38058777055 at59768e8 completed1066/1068: all687 expressions/73 diagnostics match; two compiled modules expose v3_core:mc_pairs value-before-key pre-expression scheduling. Retained original outputs are the independent authority for correcting only these two new compiled fixture expectations (sources/generated AST hashes unchanged). No historical631/252/67 contract changes. Inspect mc_pairs/mc_tuples/lc_tq2/is_safe/is_safe_same/uses_bound_vars/add_bound_vars before correction; reuse existing CompiledExpressionBindings constraints and semantic variable traversal. Implement a source-directed safe-template scheduling plan for the current AST, not full Core IR. Preserve reverse key scheduling while maps:from_keys remains eligible, value/key/reversed-prefix scheduling at the first fallback, then tuple order for remaining fields. No dependency/backend/license change; source provenance remains consolidated, draft uncommitted.

## Part 079 — Comparator sorting reuse decision (before implementation)

Inspect pinned OTP-29.1.1 lists:sort/2, fsplit_1/2, fsplit_1_1/2_1, fmergel/rfmergel and fmerge2/rfmerge2 plus maps:iterator/2. BCL Array.Sort/List.Sort cannot preserve async Erlang comparator invocation order, exact case_clause/error propagation or stable tie handling. Adapt the original natural-run split and alternating merge schedule using immutable Cons and ValueTask; use loops instead of tail recursion to protect CLR stack depth. Register lists:sort/2 and reuse it for function-ordered map iterators. No dependency or production BEAM. Comparator traces on large maps remain dependent on the unspecified maps:keys traversal; do not claim HAMT-order equivalence. Preserve consolidated upstream copyright/license notices, omit source license banners and leave LICENSE.local.txt uncommitted.

## Part 081 — Zip generator reuse decision (before implementation)

Inspect pinned OTP-29.1.1 erl_parse lc_exprs/zc_exprs, erl_lint filter_generators/handle_generators, erl_eval convert_gen_values/check_bad_generators/bind_all_generators1/zip_add_bindings and v3_core preprocess_quals/preprocess_zip_generators/zip_tq/get_strict_patterns. LINQ Zip truncates to shortest source and cannot preserve shared strict-pattern bindings, remaining-source errors or async callbacks; reject it as semantic implementation. Reuse immutable Cons/MapTerm, transactional Pattern, BitPattern scanner, private comprehension traversal, ValueTask and ProcessContext reductions. Add grouped AST/iterative cursors; evaluate sources once from common incoming scope, validate map iterators before traversal, retain raw and normalized binary remainder distinctions. Adapt source state transitions; preserve explicit existing empty-pattern/zero-progress/typed-string limitations. No production BEAM/new package; consolidated copyright/modification register and exact upstream notice, no source banners, license draft uncommitted.

Part081 source audit extension: inspect erts_internal:mc_iterator/mc_refill and erts/emulator/beam/erl_map.c:erts_internal_map_next_3 before adapting compiled zip error payloads. Flat-map iteration eagerly returns key/value/continuation tuple chains terminated by none; compiled zip failures retain these iterator terms, while erl_eval reconstructs remaining maps. Preserve supplied tuple chains and reconstruct selected flat/ordered iterator batches for failure reporting. Reduction-dependent ordered batches/nonzero HAMT paths remain explicit limitations; no general large-map raw-state equivalence claim.

Part081 source-scope audit correction (2026-10-10): erl_lint:handle_generators calls sequential exprs/3, so source-expression bindings are visible to subsequent sources during linting. Its vtold boundary removes these bindings before patterns and the template. v3_core carries the source pre-expression bindings sequentially, while erl_eval:convert_gen_values evaluates each source against the original incoming bindings. Preserve that intentional interpreter/compiler difference and add an independently expected dependency case: interpreted {error,error,{unbound,'T'}}, compiled {ok,[2]}. This corrects the earlier common-input-source wording; no prior published fixture is changed.

Part082 original execution and order-contract correction (2026-10-10): original run38065560450 completed1245/1252 atabc1463, with seven differences in five new default atom-key map fixtures; same-head1423/full CI passed. Pinned erts/emulator/beam/erl_utils.h:erts_cmp_flatmap_keys compares atom keys by tagged internal atom IDs (key_a-key_b), not lexical atom order. erl_map.c:erts_internal_map_next_3 traverses that flatmap key array. maps.erl documents default iteration as undefined; ordered iterator/2 uses sorted map-key order. Therefore atom table creation/load history changes the compared default-order outputs/remainders between interpreter and compiled probes. Do not emulate this with global reverse lexical order or silently rewrite expected values. Change only those five new fixture inputs to explicit ordered iterators, retaining their independent expected values and all prior part080 contracts; add default numeric-key traversal/error cases separately. Production default map traversal stays documented CLR exact-key order, with no BEAM atom-table/HAMT order guarantee. Original mismatch evidence is immutable in part082; this is an explicit source-input correction, not a claim the original run passed. No new dependency/license/backend.

## Part 084 — Erlang behaviour callback adapter decision

Before implementation inspected exact OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 gen_server:init_it/try_handle_call/cast/info/continue/handle_msg/handle_common_reply/try_terminate and supervisor:init/init_children/check_flags/check_childspec/do_start_child_i/start_children. Also inspected gen call/stop and proc_lib startup acknowledgement. Reuse current ModuleRegistry, immutable Term/MapTerm, mailbox, process links/monitors, ValueTask and C# behaviour hosts. Actor frameworks do not provide these return-shape, throw-as-return, init failure, stop reply and child validation contracts; no additional package is warranted. Adapters remain C# and invoke registered compiled/interpreted Erlang functions in the owning process. Separate partial standard-module exports from full OTP behaviour equivalence. Source-directed modifications retain consolidated Ericsson/Apache attribution; no source license banners; LICENSE.local.txt uncommitted/unpublished/unpackaged. Full sys/debug/upgrade/hibernate/aliases/distribution/options/dynamic/significant semantics must remain explicit unfinished scope rather than approximated as complete.

## Part 087 — Dynamic supervisor child management reuse decision

Before implementation inspected pinned OTP-29.1.1 supervisor start_child/restart_child/terminate_child/delete_child, call/2, handle_call, do_start_child_i/result, handle_start_child, save_child/del_child and the child record layout. Existing C# actor hosts plus ordered List state, immutable Term exact equality, ValueTask and mailbox serialization are suitable; BCL collections supply storage only, not restart/error/shutdown semantics. No new dependency or production BEAM. Preserve permanent/transient inactive specifications, remove temporary children, validate before duplicate lookup, distinguish already_started/already_present/running/not_found, retain Extra and source-shaped failure records, and use gen_server call with infinity. Source-directed adaptation retains consolidated Ericsson/Apache notices without banners. Simple_one_for_one, significant/auto_shutdown, restart retry/restarting states and full signal/dead-child races remain incomplete; do not promote full supervisor compatibility.

## Part 089 — Automatic supervisor retry/source decision (before implementation)

Read pinned OTP-29.1.1 supervisor handle_cast/try_again_restart, restart(one_for_one/rest_for_one/one_for_all), restart_multiple_children, start_children/children_map, count_child and add_restart/can_restart; read prior exact-source dynamic proof. Adapt actor-owned restarting ID state and a per-supervisor tagged self-cast, not BCL timer/backoff or an inline retry loop: each failed attempt returns to mailbox dispatch and consumes restart intensity. Group retries stop/rebuild only the source-selected suffix/all children and abort startup at the first failure, preserving already-started and not-yet-started peer states. Keep manual restart failures inactive rather than restarting. Existing immutable terms/ValueTask/mailbox/BCL collections are sufficient; no package/production BEAM. Retain consolidated Ericsson Apache notices without banners; draft excluded. Simple_one_for_one/significant/sys and full clocks/signal/dead-child boundaries remain explicit partial scope. Coarse monotonic second buckets/inclusive expiration follow source counting; no full BEAM scheduling/resource equivalence claim.

## Part 091 — Retry cast rejection correction
Pinned supervisor handle_cast has no fallback clause: an invalid shape or wrong private tag raises function_clause, rather than being ignored. Only correctly tagged stale retry requests are ignored. Correct part089 implementation/independent malformed-cast expectation; retain the original published reports and first oracle run. Existing exception reason and runtime error tuple preserve the selected reason class; full source stack frames remain partial. No dependency, production BEAM or banners; draft excluded.

## Part 093 — Supervisor PID ownership and monitor shutdown decision (before implementation)

Read pinned OTP-29.1.1 supervisor do_start_child_i/result/start_children/handle_call restart/count_child/shutdown/unlink_flush. The source accepts any PID result without adding a link or testing liveness; the callback establishes links. Shutdown monitors before signalling, waits for DOWN (including already dead or self-unlinked children), then unlinks and removes the queued child EXIT. Existing C# runtime monitors/mailboxes/links, immutable PID terms and ValueTask are sufficient; no new dependency/production BEAM. Store a PID rather than requiring a live ProcessHandle in the Erlang adapter, invoke normalized StartResult during initial startup, and use selective monitor completion for shutdown. Preserve the existing C# host startup/live-handle and automatic-link convenience contract explicitly; it is a CLR API adaptation, not the Erlang callback path. The runtime serializes monitor/EXIT publication under its gate, so unlink then queued EXIT removal adapts the source signal-flush sequence; full priority/distributed queues, logging, resources and races remain unverified. Retain consolidated Ericsson Apache notices without source banners; draft excluded. Add independent compiled cases for unlinked/dead/linked-then-dead PID returns, Extra/manual/dynamic starts and shutdown completion/timeout before claiming selected compatibility.

## Part 095 — Significant children and automatic shutdown

Before implementation: inspect pinned supervisor.erl init_children/check_childspecs/check_childspecs1/check_startspec/validSignificant/do_restart/do_auto_shutdown and applicable supervisor_SUITE. Reuse the existing C# ProcessRuntime/ValueTask ordered supervision engine and immutable terms; generic actor frameworks do not provide source validation/error-order or significant-child shutdown contracts. No new dependency. Distinguish standalone undefined validation policy from startup never; only unrestarted significant exits trigger automatic shutdown, manual termination/deletion/ignore do not. Track remaining significant PID/restarting state without process-alive substitution. Preserve public positional constructors via init properties. CLR adaptation uses existing monitored ordered cleanup; full logging/sys/distribution/clock/races remain partial. Consolidated Ericsson Apache notices retained; no banners; license draft excluded.


## Part097 — Improper childspec list validation correction
Pinned supervisor.erl check_childspecs/2 uses the is_list cons/nil shape guard, then checks auto_shutdown. check_startspec/4 validates each head before recursing; a valid improper prefix reaches a missing clause and raises function_clause. Reuse immutable cons cells and existing exceptions; no dependency. Correct early proper-list traversal and error swallowing. One provisional new source assertion changes per original096 output plus pinned source; all prior778/491/79 and29 other new fixtures remain unchanged. Source startup stacks/full malformed init errors remain partial; draft excluded, notices retained.


## Part 099 — Simple supervisor and query/stop source/reuse decision (before promotion)

Inspected pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 lib/stdlib/src/supervisor.erl init_dynamic, handle_call start/terminate/restart/delete/which_child/get_childspec/count, find_child/find_dynamic_child/find_child_and_args, dyn_init/store/erase/map, simple restart, terminate_dynamic_children/monitor_children/shutdown, check_flags, save_child and child_to_spec; inspected supervisor_SUITE simple cases. The existing process/mailbox/link/monitor engine and gen_server stop protocol are reusable. BCL Dictionary stores PID/restarting identities, ValueTask invokes compiled Erlang MFA callbacks, Stopwatch supplies monotonic timeout budgets; no actor package provides these semantics or warrants a dependency. Extend existing StartChild normalization instead of duplicating exception/result handling. A one-template simple strategy starts no initial child, appends dynamic arguments and keeps them across retries, indexes dynamic children by PID, and shuts down all children concurrently using one timeout budget. Preserve ordinary strategy shutdown ordering. Expose which_child/get_childspec and delegate supervisor stop to existing gen_server stop. Temporary dynamic ordinary specs erase stored start arguments as source save_child does. Host simple configuration requires an Erlang MFA; a general CLR argument-factory API is not included. PID-key query order remains unspecified rather than pretending Dictionary insertion order equals BEAM map order. Full clocks/sys/upgrades/logging/distribution/stacks/resources/upstream suites/performance remain partial. Consolidated exact Ericsson Apache notices, no banners/new dependency/production BEAM; draft LICENSE.local.txt excluded. Isolated draft35/35 plus124/124 prior callback probes are preparation evidence, not original/full-product verification. Promote, style-check and run full product pipeline before publication; compare all35 independently expected new modules against exact-source OTP.

## Part 101 — Collections, timers, tuple records and runnable platform source decision (before promotion)

Inspected exact OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 maps.erl get/update_with/filter/filtermap/map/fold/foreach/with/without/try_next/is_iterator_valid and exported map BIF contracts; lists.erl foldl/foldr/all/any/partition/filtermap/foreach/mapfoldl/mapfoldr/takewhile/dropwhile; erts/emulator/beam/erl_hl_timer.c parse_timeout_pos/setup_bif_timer/access_bif_timer/bif_timer_timeout and recipient timer-tree cleanup; erl_parse.yrl record productions, erl_lint record definitions/fields/guards and erl_expand_records record_inits/pattern_fields/record_exprs/record_update/strict_get_record_field/record_info/bool_operand/strict_record_access. Reuse privately immutable exact-key MapTerm, FunctionTerm/ValueTask, MapIteration and mailbox/registry/link/monitor/gen_server/supervisor, generated AST and MSBuild integration. BCL Dictionary/Stack/List store state and preserve source-directed traversal; System.Threading.Timer only schedules callbacks under the runtime's existing shared gate, avoiding timer/process lock inversion. No external actor/scheduler/parser package implements these contracts or warrants a dependency; no production BEAM.

Maps callbacks preserve arity-before-source errors, update_with map-before-callback errors, exact keys, duplicate iterator replacement and malformed continuation versus callback errors; ordered iterators are explicit, default traversal remains unspecified. Lists distinguish foldl/all/any initial case_clause from recursive improper-tail function_clause, right-fold validation before callbacks, left-fold side effects, short circuit and predicate/mapped-tuple failures. Timers survive creator death, cancel on PID recipient death, resolve atom destinations at delivery, use independent reference identity and segmented long delays, and do not flush delivered messages. Only relative send/start/read/cancel basic arities are selected; absolute/async/info options, monotonic epoch/range limits, pause/distribution/ordering/races and full resource/performance equivalence remain unfinished.

Tuple records lower construction/defaults in declaration order, pattern omissions to wildcards, access with strict tag/arity, update non-simple values before base and preserve immutable tuple state, indexes and record_info. Guard checks follow source boolean operand boundaries, including false record tests in orelse; normalize immutable guard ASTs through weak caching, never callback results. Typed/native/imported records, macros/includes, forward record declarations, full lint warnings/span/default/guard/update corner cases remain partial. New independent cases target these selected contracts rather than whole-language compatibility. Existing C# compiler/runtime code is reused and modified; no claim of a complete erl_parse/Core/BEAM port or faster performance. Consolidated exact Ericsson Apache notices are retained, no banners; LICENSE.local.txt excluded/uncommitted/unpublished/unpackaged.

Isolated preparation passed41 map,24 list,20 timer,30 record and1 integrated source application cases, plus all558 prior compiled modules. Initial map probe had missing draft registration insertion; fixed before production promotion, independent expectations unchanged. Initial record guard probe exposed orelse suppression; corrected from bool_operand/strict_record_access, expectations unchanged. Preserve both initial logs. Full product build/packages/CI and new exact-source comparison are required before completing the requested early platform milestone.

## Part 105 — Complete maps export surface source/reuse audit (before implementation)

Inspect OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 maps.erl intersect/intersect_with/merge_with/error_type_merge_intersect/groups_from_list/to_list and maps_SUITE combiner, grouping, iterator and consistency contracts. Reuse the existing exact-key immutable MapTerm dictionary/index, FunctionTerm async callbacks, Cons validation and MapIteration; BCL Dictionary avoids rebuilding and sorting a MapTerm for every group/merge entry. System.Collections.Immutable is unnecessary; BCL map equality alone would not replace Erlang exact-key semantics. No external package/production BEAM/dependency. Inspect existing code, MIT/.NET BCL reuse and consolidated Ericsson Apache source notices; preserve all notices, no source banners, draft license excluded.

Port smaller-map traversal with original Map1/Map2 callback argument orientation, callback-arity-before-badmap errors, grouping full-list validation before callbacks, right-to-left key then value callbacks with left-to-right group results, and iterator to_list badmap wrapping. New reference fixtures are independently asserted; prior674 modules/778 expressions/84 diagnostics remain unchanged. All34 exports does not by itself establish HAMT traversal/resource/stacks/error_info or whole upstream-suite/performance parity.

### Maps iterator correction before implementation — part 107

Part106 retains the complete failed original report. Inspect pinned maps.erl:next/is_iterator_valid and erts/emulator/beam/erl_map.c:erts_internal_map_next_3: ordered keys are resolved into a tuple chain before returning, missing keys/improper tails raise badarg before callbacks. Port that chain construction using existing exact MapTerm lookup, Cons and immutable tuple terms. Native reduction-dependent batching/HAMT paths remain an explicit resource/internal-layout boundary. Correct the one provisional new source assertion from true to false and add five independently expected small-map batching/error-side-effect cases; preserve prior part104 corpus and the failed part106 source/outcome. No new dependency.

## Lists completion source/reuse plan — part 109 (2026-10-11)

Objective: complete all 91 OTP-29.1.1 lists exports, audit the existing 31, and compare independent functional/error/callback cases against original OTP before promotion. Baseline ad05823719d77c8faee87348ea39513d4e2f99c5; inspect lib/stdlib/src/lists.erl, lib/stdlib/test/lists_SUITE.erl and applicable emulator list BIFs. Preserve immutable part108 corpus 778 expressions/764 compiled modules/84 diagnostics and same-head 1869 validation.

Reuse candidates: existing immutable Cons/Term/TupleTerm, exact hash/equality versus NumericEquals/TermOrder, FunctionTerm and ProcessContext async reduction checkpoints, ComparatorSort's source-directed natural-run sort. BCL List/Stack/HashSet and stable Enumerable.OrderBy are already SDK dependencies; they can own temporary buffers, exact uniq membership and stable non-callback ordering, but .NET equality, comparer callback schedules and exception defaults cannot replace Erlang contracts. No new package or actor abstraction is needed. Callback unique sorting and merges follow pinned helper control flow; native recursion is adapted to loops/state to avoid CLR stack growth. List/tail/arity errors and short-circuit validation are ported per routine rather than globally normalized. Concat float rendering needs Erlang legacy scientific formatting rather than .NET default ToString.

Source-derived Ericsson Apache notices stay consolidated in OTP-PORTS and full OTP license/package notices; no file banners. LICENSE.local.txt remains uncommitted/unpublished/unpackaged. Native resource/reduction thresholds, all source stack/error_info annotations and complete upstream Common Test infrastructure are distinct runtime boundaries; no speed claim. Every export receives a direct permanent contract plus broader independent scenarios. Failed original comparisons will be preserved and fixed before a completion claim.

## Erlang completion source/reuse plan — part 111 (2026-10-11)

Target the union of pinned erlang.erl exports and emulator bif.tab declarations: 351 functions/arities, 44 currently registered. Complete functional implementations and independent reference contracts progressively; no generic badarg/undef stubs count as implemented. First port registered operator calls, numeric/predicate/tuple/list operations, apply and dictionary enumeration. Audit erlang.erl, erl_bif_guard.c, erl_bif_op.c, bif.c and erl_arith.c; preserve all part110 historical778/1016/84 sources, expected outcomes and generated hashes.

Reuse existing immutable term/order/exact equality, list helpers, FunctionTerm, ProcessContext and ModuleRegistry. Centralize existing compiler operator value semantics in a runtime helper while retaining compiler exception stack wrappers. BCL BigInteger and Math provide exact integer operations and explicit AwayFromZero rounding; .NET arrays own temporary immutable tuple copies. No package dependency is needed. Native badarg versus badarith, float overflow, numeric tie selection, tuple initialization ordering, arity checks and proper argument lists are explicit source contracts, not .NET defaults. Guard and runtime registration must be aligned.

Later increments must deliver binary/text/ETF/checksum/hash conversion, clocks/timers, process/alias/monitor/spawn options, runtime inspection/flags/GC, code lifecycle, ports/distribution and tracing/host lifecycle, including required underlying runtime state. These are not equivalent to superficial registration. Native records require a real representation before their positive is_record contracts can be claimed. Dynamic/resource/OS observations need relational original checks rather than invented fixed host values. No performance promotion without measurements. Consolidated notices remain in OTP-PORTS/OTP-LICENSE; no banners and no publication of LICENSE.local.txt or public NuGet packages.

## Erlang conversion/checksum completion plan — part113 (2026-10-11)

# Part113 source/reuse plan (draft, before production implementation)

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Read erlang.erl integer/list/binary/iolist/float contracts, bif.c conversion wrappers, erl_unicode.c atom encodings/existing behavior, binary.c binary/list/bitstring slices, erl_bif_binary.c binary_part, erl_bif_guard.c admission, erl_iolist.c iterative traversal/512-byte iovec grouping, sys/unix/sys_float.c signed decimal/comma/exponent/NUL/underflow acceptance, erl_bif_chksum.c seed and length uint32 validation, zlib/adler32.c and crc32.c combination values. No reference checkout mutation.

Reuse existing immutable terms/arrays, exact error reasons, module dispatch and compiled-module independent oracle. SDK BigInteger/MemoryStream/Stack own integer conversion and iterative deep iodata traversal. Batch integer bytes up to the iovec copy threshold, reuse larger immutable binary segments, avoid recursive CLR stack growth, retain logical atom/domain constants separately. CRC uses a private standard reflected-polynomial table and GF(2) matrix powering rather than bundled zlib interleaving implementation; Adler combination adapted from audited source formula, SDK IncrementalHash MD5 consumes segments. No new dependency or production BEAM. No measured speed claim.

Zlib source-guided checksum adaptations must be explicitly marked as altered C# implementations in consolidated OTP-PORTS; preserve full zlib1.3.2 notice from pinned zlib.h (Gailly/Adler1995-2026, Mark Adler adler321995-2011,2016/crc321995-2026) in consolidated OTP-LICENSE without file banners. Part111/112 package notices remain unchanged; amend notices only after112 proof, before next validated packages.

Draft isolated baseline probe121/126: four real parser defects where string '+'/'-' is mistaken for a prefix operator and one unsupported local fun reference. Change iovec fixture to explicit fun(X)->is_binary(X) end (function-reference implementation remains future function/runtime work), not a changed expected value. Expanded probe200/204 retains four real token-kind parser failures. Fix token recognition for syntax only in Keyword/Symbol tokens; do not alter problem strings or independent {ok,done}. Add string/operator/keyword/delimiter and binary-part guard regressions, audit historical sources/generated hashes before original. No generic error stubs.

19 integer/io exports +2 float parsers +7 checksum/MD5 =28 missing MFAs. Positive, exact-byte, malformed/type/base/index, deep nested/tail/partial bits, default native optimization and error propagation cases are independently expected. Native storage offsets/iovec partition optimizations and resource limits must be reported explicitly; full erlang remains unfinished at133/351 API presence if this increment completes. Atom existing table/module literal lifecycle, complete float rendering, ETF/native identity/hash, clocks/options, processes/native records/code/ports/distribution/tracing/host support are later real implementations.

Additional part113 source/reuse audit before identity fix: the unchanged5000-deep iolist fixture reveals TermOrder recursion while merging an identical immutable binding, before iodata traversal. Pinned utils.c eq/erts_cmp_compound first use is_same (erl_term.h identity). Add CLR ReferenceEquals shortcut after existing rank/type validation; preserve exact/numeric ordering and all distinct-object paths. Keep depth unchanged, retain process exit/stack excerpt, add direct20000-deep identity regression and rerun full checks. Nonidentical arbitrary compound recursion remains a separate boundary.

# Part115 atom and float output source/reuse plan

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Inspect erlang.erl conversion contracts, bif.c atom/list and sequential float options, erl_unicode.c binary encodings, atom.c/atom.h UTF8 length/error ordering and default table limit, atom.names primordial names, utils.c Unicode list validation, erl_scan.erl identifier/quoted atom observation, sys/common/erl_sys_common_misc.c fixed fast-path/fallback/compact rounding, sys/unix/sys_float.c scientific output, ryu/to_chars.h shortest presentation thresholds. Exact source is accessed through git show; reference checkout remains unchanged.

Reuse ConcurrentDictionary with an insertion lock for globally retained atom names, independently of the ten cached Atom objects; permanent names implement existing-name semantics and disappear only with the VM. Bootstrap exact ordinal primordial names plus erlang function names; observe created terms, scanned identifiers/quoted atoms and loaded module/function names. Capacity follows the native default1048576 names, but exhaustion raises local system_limit instead of crashing the host; configurable native capacity and fatal exhaustion behavior are explicit resource differences. Host Atom construction retains existing acceptance boundaries; conversion APIs enforce255 Unicode scalars,1020 UTF8 bytes,strict Latin1/UTF8 validation and source-specific precedence. SDK Rune/StringBuilder/strict UTF8Encoding own Unicode decoding. Existing conversions do not intern missing names.

Reuse the repository's exact binary-rational nearest-even scientific formatter with parameterized precision; preserve its20-digit lists formatting. Port fixed-mode source semantics using Math.Floor/Math.Round AwayFromZero for abs<=2^53 and precision<=18, SDK nearest-even fixed formatting otherwise. Bound the256-byte native output buffer before fallback compaction and after final output. Preserve compact fallback at zero precision, signed zero, last recognized format option, improper/options/type/small-integer errors. For short, use SDK round-trip digits and independent minimum-length fixed/scientific rendering with required .0, no positive exponent sign/padding, fixed tie preference and scientific at2^53. No Ryu/STL source body, tables or code are copied; inspect source thresholds as behavioral guidance rather than incorporate dual-licensed code. This choice requires native vectors and remains unverified until original comparison.

Consolidate Apache source credits in OTP-PORTS/OTP-LICENSE without banners. Primordial names are generated factual metadata from pinned atom.names; preserve its Ericsson1996–2026 credit. No new dependency, package publication or production BEAM. Mathematical/SDK reuse is not a measured speed claim.

Ignored initial probe100/100 and expanded174/174 use explicitly simulated atom-name observation; they establish draft conversion/format paths only, not production integration. The bootstrap audit detects case-insensitive PowerShell Sort-Object deduplication discarding EXIT when exit also exists; replace with SortedSet StringComparer.Ordinal before production (956 names instead of955), preserve audit and add permanent case-sensitive name regression. Additional175th case covers primordial EXIT/exit/DOWN without atom literals in its input. Full production tests and exact original/same-head CI remain required. Register9 atom+4 float output MFAs only after real implementation;133 to146/351 and218 to205 remaining if validated. Do not claim complete erlang.

# Part117 clocks/calendar/timer and oracle execution source/reuse plan

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Read erlang.erl units/clock/unique/calendar/timer contracts and convert_time_unit body, erl_time_sup.c clock/offset/timestamp/now/civil date and timezone code, erl_time.h native64 nano epoch/range, erl_bif_unique.c option sets/offset/counters, erl_hl_timer.c timeout/options/recipient/access/async replies; inspect time_SUITE.erl and timer BIF suite where applicable. No checkout changes.

SDK Stopwatch owns monotonic/performance sampling, BigInteger owns arbitrary unit conversion/floor arithmetic/counters, DateTimeOffset supplies initial POSIX wall time and DateTime/TimeZoneInfo supplies current local/UTC data and timezone rules. Port native64 nano initial monotonic epoch and timer internal limits, exact Gregorian civil conversion formulas and1600/1902/int32-year/int64-seconds gates. Unique values are guaranteed only per modifier set; native explicitly permits overlap across sets. Separate default/monotonic counters, positive mapping and native immediate lower bound with insertion lock; default may also increase monotonically. Timestamp returns system time; now adds a globally serialized strict microsecond increment. No scheduler-thread bit packing copied; actual numeric values/scheduler distribution and64-bit exhaustion policy remain explicit boundaries. Clock uses a stable initial wall offset; full OS correction/time-warp/offset-monitor state remains future work.

Local calendar honors host TimeZoneInfo, forced/automatic DST and latest-rule no-DST handling, with400-year Gregorian future-year mapping outside SDK DateTime range. Exact native tzdb/libc/DST ambiguity/gap/historical/far-future/leap-second behavior requires further proof; do not claim it from UTC or Russian Standard Time probes. Universal/epoch conversion is independent of SDK year limits and follows audited native integer formulas. Native local conversions require1902+, UTC epoch conversion1600+, maximum input year int.MaxValue-1, input components native small integers; POSIX decoder accepts full int64.

Timer options are proper tuple lists, last duplicate wins: start/send accept abs only, read accepts async only, cancel accepts async/info. Source runtime validates time before destination, distinguishes native monotonic bounds from Int64.MaxValue, accepts valid past absolute deadlines as immediate, supports recipient-owned cancellation and atom lookup at delivery. Read/cancel async replies carry operation-specific protocol tags/reference/result; async returns ok, info=false suppresses replies; synchronous info=false no-timer behavior distinguishes native ordinary local references from external/invalid scheduler references. Use actual C# timer/mailbox state and locked expiry/cancel access, no generic error stubs. Pause/distribution/full scheduler races and precise boundary races remain unfinished. All existing /3,/1 timer paths receive source-range validation as well.

Twenty clock/calendar MFAs plus four timer-option MFAs add24 exports if validated (146 to170/351,205 to181 missing). Ignored85-case clock/calendar probe passes against Russian Standard Time host only; native proof pending. Permanent independent /export, signed floor/bignum/custom units, monotonic/offset/system/timestamp brackets, unique options, UTC leap/extreme civil dates, local/DST/range/type and absolute/async/info timer cases are required.

Development oracle currently starts a fresh OTP VM per input and runs CLR comparisons/report writes sequentially. Reuse SDK Tasks/CancellationToken/Queue for bounded two-process prefetch of unchanged commands in independent fresh VMs; consume/report in original input order, assign active source immediately before awaiting that input, cancel/kill/reap pending children on abort/disposal, retain version gate/default compiler flags/exact source pin. Add meaningful ordering/concurrency/future-failure/disposal/starting-callback-failure tests. Cache immutable Roslyn PE metadata references for the fixed process platform/core assemblies rather than recreate them per fixture; generated code/hashes/runtime isolation remain unchanged and must be audited. No shared native VM, fake responses or relaxed optimizer flags. This is infrastructure reuse, not an Erlang-versus-OTP speed claim.

Source-guided C# adaptations are explicitly altered implementations under consolidated Ericsson Apache credits; SDK and new infrastructure are existing dependencies. No banners/new packages/production BEAM/public package publication. Finish116 proof before production mutation;117 rolls detailed English changelog to0017 if its whole entry exceeds0016's remaining budget, preserving closed0016 hash and all previous evidence.

## Part119 incremental MD5 and explicit raise-stack adaptation

# Next incremental MD5 source/reuse plan

Read pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 erl_md5.c/h and erl_bif_chksum.c update/final/do_chksum. OTP29 has a new Ericsson2026 Apache implementation; no older RSA implementation or attribution is introduced. Carry consolidated altered-implementation/source credits rather than banners. Preserve LICENSE.local.txt unpublished. Runtime remains C#/.NET, no BEAM/dependencies.

Existing SDK MD5.HashData remains suitable for one-shot digest and independent development cross-checks; IncrementalHash lacks public clone/serializable internal context, so it cannot preserve immutable binary state branching. Port audited transform/update/padding using BinaryPrimitives and BitOperations.RotateLeft; use existing iterative iodata traversal to avoid source list recursion. Keep MD5 mathematical constants, binary context offsets and implementation policy in separate semantic owners.

Pinned native64 little-endian context is96 bytes: four uint32 states at0..15,total processed bytes uint64 at16,pending length size_t at24,buffer64 at32. Native keeps a pending block of exactly64 bytes when an update fills an existing fragment exactly; final pads that block into128 bytes and later nonempty updates flush it. Preserve stale buffer bytes, modulo uint32/uint64 arithmetic and zero-length updates. Copy state for both update and final; branch/repeated-final originals remain unchanged. Input context binary length/type checked before iodata; nonbyte bitstrings and invalid nested/improper data produce badarg.

Opaque context layout is pinned native64 ABI, not a portable on-wire contract. CLR rejects forged pending lengths greater64 for memory safety instead of reproducing native unsafe reads; no live oracle inputs with corrupted length. Exhaustive reduction/trapping/resource/malformed-context/other-endian/32-bit behavior remain explicit boundaries. Existing iodata integer batching may vary internal context bytes for large nested chunking while digest/immutable continuation semantics remain checked; only defined native64 initial/short binary contexts receive raw byte goldens.

Draft51/51 digest/options/boundary/branch/iodata cases passed before adding two independent native64 context goldens. Initial unaligned-binary fixture lacked whitespace between pattern assignment and binary opener, causing parser failure; repair draft source before integration, retain failed log. Permanent tests must include RFC abc digest, empty/block55/56/63/64/65/127/128/512 and100000-byte ranges,split updates,pending64,branch/repeated final,unaligned binaries,nested iodata,invalid type/length/data and byte context goldens. Proof for published clock117 remains in progress; do not integrate production until118 concludes.

# Future raised-exception source plan

Pinned OTP29.1.1 bif.c raise_3 validates error/exit/throw class and proper outer stack list. Invalid format returns badarg normally; it does not raise a badarg exception. MFA frames accept unvalidated Args/Arity field; old3-tuples gain empty location;2-tuples {Fun,Args} gain empty location;3-tuples {Fun,Args,Location} and4-tuples {M,F,ArgsOrArity,Location} require a list head or nil for Location, not a proper metadata list. Validate the entire outer list even past backtrace-depth truncation. Default backtrace depth8 from error.h, zero still retains one element for nonempty input. Future system_flag must control actual runtime setting.

Port normalization/validation and explicit stack propagation. Add an ErlangException ExplicitStackTrace flag, preserve it across evaluator/registry/caller propagation and suppress new synthetic frames only for explicitly raised stacks. Keep old automatic frame handling unchanged in this unit; do not claim full automatic physical-source/native stack parity. Source structs/unsafe allocation need not be copied. Existing immutable Term/Cons/Tuple infrastructure is reused, under consolidated altered Ericsson source credits. No dependencies/banner/BEAM/public package/draft license publication.

Other exception MFAs require follow-up source behavior: nif_error1/2 equal error1/2 at runtime (Dialyzer distinction only); error2 accepts arbitrary args without validating a proper list; error3 parses a proper sequence of {error_info,Map} and last valid map wins, invalid/options/improper fall back to error2 rather than raise badarg. Actual top logical caller/argument override/error_info ownership must be implemented, not represented by a generic throw with discarded metadata. Read beam_common.c EXF_ARGLIST and save/build_stacktrace before registering these. Preserve historical input/generated hashes and independently compare normalization/truncation/classes/invalid-format normal returns/nested rethrows against OTP.

Part119 integration began only after completed original2523/2523 and same-head2959/full CI118 were published as884f33f. The planning notes above were written while118 was pending. Only MD5_init/update/final and raise3 are registered in119; error2/3 and nif_error remain audited follow-ups. Process termination now preserves real supplied exception stacks and wraps uncaught throws as {nocatch,Reason}; permanent monitored-child cases cover all three exception classes. Native corrupted MD5 pending-length memory safety is deliberately a local-only check.

# Part119 local process/node/scheduling increment

Read pinned OTP29.1.1 bif.c processes0/bump_reductions1,dist.c node0/1,register.c registered0,erl_bif_info.c is_process_alive1,erl_vm.h CONTEXT_REDS4000 and erlang.erl yield0 contract. Existing ProcessRuntime gate-protected process/name tables and typed node containers are real state, not placeholders; expose immutable snapshots and source-directed queries. The default node already is nonode@nohost in the current repository; no identity default change needed. Named host runtimes keep their supplied identity; distribution/connectivity remains separate unfinished scope.

node1 accepts pid/port/reference containers and rejects other terms. is_process_alive validates pid/local node,returns actual registry state,old local creation false;foreign-node pid badarg. Current signal/termination locks provide local ordering,not full distributed queues/incarnation reference metadata. registered0 returns actual names; processes0 actual live runtime pids; background host service/port/native scheduler entities and exact native iteration/order/VM population are not invented. These exports do not count native processes iterator or ports as implemented.

bump_reductions requires native64 nonnegative small integer (max2^59-1),clamps charge at pinned CONTEXT_REDS4000 and returns true after charging the actual cooperative budget. yield awaits SDK Task.Yield and checks process cancellation. Keep source cap4000 separate from existing CLR cooperative slice2000; extract the latter resource-policy number into an owner constant instead of merging these distinct contracts. Exact native reduction-accounting/scheduler resource/throughput parity remains unclaimed. Reuse existing SDK collections/locks/Tasks; no dependencies/BEAM,banners/public NuGet or draft-license publication. Seven exports add to MD5/raise's four, giving eleven new real MFAs if checks pass. Permanent queries/lifecycle/name cleanup/invalid types/bump boundaries/yield fixtures and original comparison required.

Supplement: inspect lib/stdlib/src/erl_internal.erl and enable only node0/1 as guard BIFs; is_process_alive is not admitted as a guard. Three independent guard cases cover qualified/unqualified calls and bad-node-container guard failure. Runtime node0 reuses a readonly per-runtime Atom, observing a supplied host node name once without globally caching arbitrary Atom objects. All119 changes occur after completed118 proof; no published117 executable sources were modified during its original run.

## Part121 external-function identity and error argument/info support

Audit pinned OTP-29.1.1 bif.c function_exported/is_builtin/make_fun/error2/error3/nif_error1/2; beam_common.c erts_is_builtin and EXF_ARGLIST/save/build stack; utils.c external fun equality/order and erts_build_mfa_item; bif.tab native classification; erl_vm.h MAX_ARG and native64 small representation. Builtin metadata has553 factual bif.tab entries plus apply2/apply3/yield0 instruction special cases=556 distinct keys. Parse quoted operator atoms and slash-containing operator names correctly; initial ignored draft catalog parser omitted three slash names and kept quoted spellings, corrected before integration.

SDK Delegate.Equals compares targets/methods rather than Erlang module/function/arity and bound environments. MethodInfo/CLR reflection cannot report source Erlang export visibility. Reuse existing immutable Atom/FunctionTerm/TermOrder and ModuleRegistry; add readonly external MFA descriptor, typed catalogue and actual export lookup. Creating an external function does not require target loaded/exported; its call resolves through the invoking process's current registry, not captured runtime state. Exact equality/order/hash use external MFA; externals sort after locals. Keep legacy/local code-entry identity unchanged until full local metadata/free-capture support is ported. No global arbitrary function-object cache is required.

SDK Exception.StackTrace lacks source MFA/argument override/error_info. Reuse AsyncLocal execution-flow storage with disposable logical named/anonymous frames, including tail trampoline replacement and callback cleanup. ConditionalWeakTable stores weak per-AST anonymous frame identities instead of strongly retaining code trees; these CLR code-entry indexes are implementation-specific physical metadata, not native BEAM indexes. Add error2/3 actual arguments/error_info stack ownership. Cons or nil args appear verbatim, including improper cons tails; other args fall back to caller arity. error3 requires a proper ordered sequence of {error_info,Map}, last map wins; invalid/improper options discard metadata and fall back to error2. nif_error1/2 retain source error class/reason and builtin frame arguments. Finalized captures suppress duplicate automatic wrapper frames; explicit raise behavior remains unchanged. Full native physical locations, anonymous code identity/free environments, host-call frames/init/process-info/stack/error_info suites remain unfinished.

Ignored prototype50/50 function contracts passed after repairing incorrect draft expectations that integer_to_binary1 and universaltime_to_posixtime1 were wrappers: exact bif.tab shows native BIFs. First error draft had a receive without a timeout and was interrupted without a completed report; subsequent57/94 exposed illegal catch-stack patterns and binary assignment spacing in newly authored fixtures. OTP requires a variable after the second catch colon; use Stack and match its body. Fix before integration, preserve logs/source; do not change old corpus. Final ignored prototype94/94 passes (50external+44error metadata); original comparison is unrun. Consolidate altered Ericsson source credits in OTP-PORTS, without banners/new packages/production BEAM/public package publication/license draft inclusion.

Part123: SDK ConditionalWeakTable, MD5.HashData, BinaryPrimitives and private Queue/monitor locks are reused for weak canonical CLR code metadata and actual initialization work ownership. Existing SDK .NET MIT licensing applies; no new package. Delegate.Equals/MethodInfo do not represent Erlang code plus free variables, and a synthetic background PID would not establish an initialization process. Typed source scope traversal, immutable code/environment descriptors and a real registered mailbox actor implement the audited contracts. Canonical CLR module MD5/index allocation is backend-specific and is not native BEAM assembly identity. Local57 metadata fixtures and scope/ownership/rollback/hash/lifecycle drafts pass; original proof remains pending. Source-directed printer/info/free-variable contracts carry consolidated Ericsson/Richard Carlsson Apache credits in OTP-PORTS. Full SSA, unloaded code, local ETF and native init scripts/kernel state remain unfinished.

Part125: existing SDK Encoding.UTF8 plus immutable Cons implements pinned bif.c buf_to_intlist printer bytes. Term.String retains its general Unicode codepoint contract. No new dependency/license change. Original124 failures remain immutable;only three new fixtures corrected and one local Unicode regression added. Full original126 pending.

Part127: reuse SDK string.EnumerateRunes/LINQ and existing CompileException for source-directed module-name admission. Generic .NET identifier/Unicode-category validation cannot replace OTP exact Latin1,space/NBSP/soft-hyphen and C0/DEL/C1 rules; equal codepoint constants remain in a module-name domain catalog. Apache Ericsson1996-2026 adaptation receives consolidated OTP-PORTS credit;no new dependency. General Unicode atoms remain supported. First-error offset0 is a documented compiler annotation/aggregation boundary;four valid names/nine independent error contracts added;full original128 pending.

Part129: reuse SDK ZLibStream with exact -1..9 ZLibCompressionOptions, BinaryPrimitives, RandomNumberGenerator, spans, pooled Huffman arrays and counting/segmented Stream subclasses. BCL decompression validates actual data/checksum but read-ahead cannot provide binary_to_term used bytes; source-directed preflight and an RFC1950/1951 physical scanner cover that distinct contract. Delegate identity is not a serialized closure: per-runtime code factories reconstruct decoded free variables and actual invocation. Immutable BitString borrowing avoids copying large uncompressed non-local iovec payloads; local/compressed paths flatten and no benchmark advantage is claimed. Existing SDK MIT licensing/no new dependency; consolidated altered Ericsson Apache credits in OTP-PORTS, no banners or draft license publication. Native records/BEAM code loading/purge/large export arities and complete resource semantics remain separate work. Exact original verification pending after completed-part publication.

Part131: retain System.Numerics.BigInteger and the existing owned ETF reader; no new dependency. Pinned big.c/external.c require nonzero-sign normalization. Preserve the safe managed LOCAL function encoder rather than reproducing original size/hash defects; explicit oracle exclusions retain all managed tests and expose incomplete reference coverage.

Part133: existing SDK BigInteger,BitConverter,Encoding.UTF8,ReadOnlySpan and typed Stack frame values implement pinned portable hash algorithms;do not substitute randomized CLR GetHashCode or add a package. Source polynomial/mix arithmetic and actual function/reference identities retained. Owned binary spans eliminate whole payload copies;iterative state restores nested map-pair XOR contexts. Native scheduler trapping/record terms and BEAM-generated code identity remain explicit boundaries. Consolidated altered Ericsson notices extended before package validation.

Part135 plan: audit pinned bif.c alias_1/unalias_1/do_send/send_3, erl_bif_unique.c/h PID-reference creation and lookup, erl_term.h immediate PID tags, erl_proc_sig_queue.c handle_altact_msg, erlang.erl send_nosuspend wrappers and upstream alias/priority-message suites. Reuse SDK Dictionary plus the existing runtime gate for alias ownership and atomic admission/deactivation, immutable ReferenceTerm for ETF reconstruction, and LinkedList mailbox nodes for stable priority insertion/removal. No third-party actor framework or dependency: generic priority queues lose Erlang selective-receive ordering; delegate/GC identity is not an alias. Alias options are fully validated before allocation, last explicit_unalias/reply wins, priority is sticky; ordinary references accept sends but discard them. Reply aliases deactivate at signal admission rather than pattern selection; admitted messages survive unalias. Alias owner termination removes live entries. Five-word references preserve owner PID immediate payload with the pinned marker; CLR process allocation and lack of connected distribution remain explicit boundaries. send3 and send_nosuspend wrappers supply actual priority request, distinct from alias permission; local/no-connect behavior will be tested independently. Native ports, connected distribution, monitor-associated aliases and link/exit priority remain future prerequisites, never hidden by error stubs. Existing SDK/Apache consolidated notices apply, no banners or license-draft publication. Permanent compiled contracts and host mailbox/lifecycle checks precede full pipeline and original verification.

Part137 alias repair plan follows immutable failed original136 (ab94002 evidence,569cba8 executable). Audit erl_proc_sig_queue.c alt-action message enqueue/handle_altact_msg/priority insertion, bif.c unalias_1 and process signal handling at receive/yield. Reuse existing linked list with a nested value entry containing the immutable Term and an optional internal admission resolver;plain messages need no additional heap wrapper. Runtime gate owns alias state,mailbox gate owns queue;drain callbacks run outside mailbox locks and acquire runtime-before-mailbox in the same order as send,avoiding inverted locks. Pending placeholders preserve sender order among ordinary messages;admission checks live alias and only then consumes reply mode or promotes to priority prefix. Receive retries rather than missing a wakeup when a pending signal arrives between drain and scan. Inspection/remove/receive and cooperative yield provide managed processing boundaries;unalias itself never drains pending signals. Owner death discards pending resolver/payload ownership. Native reduction budgets/asynchronous scheduler timing remain partial. No package/dependency/banner needed;existing altered Ericsson signal-queue credit applies.

Retain exact failed136 compiled source and generated hash;correct only its provisional Expected from done to error/error/timeout using the preserved actual native result,with an explicit ledger. Add separate pending-drop/admitted-kept/FIFO/priority/reply/wakeup cases. Existing lifecycle host test also assumed immediate admission:establish actual admission by inspecting mailbox before unalias;record that test scaffolding repair. Do not exclude the valid native mismatch. Keep monitor/dynamic-trace prototype isolated until this repaired core passes full checks and original. Existing closed0019/0018,sibling and uncommitted LICENSE.local.txt must remain unchanged.

Part139 plan: audit pinned bif.c monitor/demonitor/monitor-options and exact !USE_VM_PROBES dt_* branches, erl_proc_sig_queue.c DOWN/reply admission, configure.ac default-no-probes and erlang.erl wrappers. Reuse existing runtime gate,reference identity and pending mailbox rather than new dependencies. Add real process monitor/3,demonitor/2 and seven exact default-disabled trace contracts. Port,time_offset,connected distribution and enabled-probe backends remain unfinished. Keep semantic send-option/send-result/alias/monitor/trace constants separate. Preserve137 sources/expectations/hashes and original138 before integrating draft. Require full3902 and pinned3398 original140; no source banners or license draft publication.

Part141 plan: follow pinned bif.c group-leader helpers/spawn_3/spawn_link_3,erl_process.c erl_create_process inheritance and atom/proper-list validation,erlang.erl wrappers,beam_common.c apply_fun/call_fun, and io.erl o_request/execute_request/io_request/conv_reason. Reuse the runtime gate,owned monitor/pending-signal mailbox and existing formatter. Implement actual per-process group leader and five real erlang exports;spawn/1 validates in the caller but applies its accepted value in the child,including tuple badfun. Existing io:format/2 must send monitored io_request/io_reply to the actual leader;managed initializer services default output. Preserve all 139 sources/expectations/hashes;port/connected distribution/full system graph/io_lib/input/file/error_info and physical scheduling remain unfinished. Fixed immutable I/O atom cache and separately named protocol/control constants;no dependency,banner,production BEAM or license draft publication. Require full3968,original3458 and same-head CI before promotion.

## Part143 - Exit signals, priority links and monitored-spawn inheritance

Inspect pinned bif.c send_exit_signal_bif/link_opt/erts_link_opts,erl_proc_sig_queue.c send_gen_exit_signal/handle_exit_signal,and erlang.erl wrappers at OTP-29.1.1/ad058237. Reuse actual C# runtime gate,per-owner links/aliases,selective pending mailbox and existing spawn/monitor core;no external dependency. Add four actual exports exit_signal/2,3,exit/3,link/2 and route existing exit/2 through its legacy self-normal path. Resolve alias/link permission at admission;exit signals do not consume reply aliases;priority requires alias permission or receiver-owned priority link. Missing local untrapped link raises error:noproc,not process-exit. Repair SpawnMonitor passing observer as parent. Make mailbox pending bookkeeping safe when admission terminates process and clears remaining nodes;recheck cancellation before scanning payloads. Normalize native compiled execution into a fresh monitored child matching SDK semantics;retain old initial-process command for five separate context diagnoses and raw framed outputs,not additional coverage. Preserve published2593 fixtures/six exclusions,run4052/full local and plan3537 comparable original. Connected distribution,real ports,physical scheduling/reductions/error_info and exhaustive suites remain unfinished. Consolidated altered Apache credits apply;no source banners,production BEAM,public package or draft-license commit.

## Part145 - Native compiler and startup-context investigation

Before this unit,read published144 full failure checkpoint and current index/journal/progress/compatibility/decisions. Inspect pinned proc_lib.erl sync_start/init_fail/start_link documentation,erlang.erl spawn wrappers,and beam_disasm.erl/beam_disasm.hrl public disassembly record layout. Source confirms native linked startup can terminate an untrapped caller;C# adapters currently link only after successful initialization,so an actual repair is required. Do not silently change four historical independent returned-error expectations:their old initial-process trap-exit context also needs explicit investigation. Source confirms spawn/1 accepts arbitrary fun/MFA pair and invokes apply in child;persistent PID outcomes remain unexplained. Reuse development-only pinned native oracle,framed ETF protocol and exact unchanged fixture sources. Add an investigation-only mode:36 native evaluations,six failures times three caller contexts times default/disabled compiler optimizations,with raw command/output and run/0 disassembly retained. These diagnostics are not additional corpus coverage or implementation completion. Production code/dependencies and all fixture hashes remain unchanged;no banner,public package or license-draft publication. Validate full local pipeline,commit/push tooling checkpoint,then dispatch exact pinned-source diagnostic workflow before designing runtime/context repairs. Clock/record drafts remain isolated.

## Part147 plan - Isolate the compiler pass before changing the reference policy

After preserving146,inspect pinned beam_call_types.erl spawn/1 and beam_ssa_type.erl call/result-type simplification. The pinned type table restricts spawn/1 to arity-zero fun even though erlang.erl accepts any fun and the historical MFA-pair wrapper. This is a plausible source of unreachable-continuation inference,not yet a proven pass diagnosis. Reuse existing investigation-only tooling with exact unchanged sources,raw outputs and disassembly;add no_copt,no_ssa_opt,no_postopt separately alongside the existing default/all-disabled modes.90 evaluations are diagnostic only and corpus counters remain zero. A permanent isolated-options transport check and full pipeline precede publication/native execution. No production implementation,fixture expectation/source/exclusion/global default change,dependency or banner. Preserve146/144 evidence and consolidate existing SDK/OTP notice policy. Startup repair and state-query drafts remain isolated until native pass policy is justified.

## Part149 implementation plan - Owned startup, process inspection, clock monitors and tuple-record predicates

Read current148/native90 proof and pinned proc_lib.erl start_link/sync_start/init_fail/flush_EXIT,gen.erl registration and normal failed-start exit;erl_bif_info.c process_info_bif/links/trap_exit/group_leader/registered_name; bifs/time-offset monitor backend and erts_internal flush;erl_bif_op.c is_record/2,3 and erl_expand_records record_test plus lint/guard rewrites. Reuse actual C# runtime gate,per-process link/group/name state,owned monitor/pending selective mailbox,immutable clock offset/terms,existing record declarations/AST/guard/codegen. No new dependency or actor framework. Adapt atomic link+monitor startup and actual ack/nack/DOWN waiting;unlink before flushing startup EXIT;preserve host facade exception handling. Register startup names atomically and reject collision with a normal child exit. process_info/2 covers four real state keys,proper-list/duplicate/wrapping/liveness rules;other native inspection fields and process_info/1 remain unsupported,not fabricated. Runtime gate-driven target signal admission adapts the managed scheduler.

Retain owned time_offset/clock_service monitors with actual fixed-offset no-time-warp clock and all/one flush behavior;cache separate immutable CHANGE/DOWN default options. Add tuple is_record/2,3,declared literal record sizing,guard/body/runtime evaluation order and literal nonpositive-size suppression after lint. Native-record terms/is_record/1/native-record positive branches remain unimplemented. External-fun literal grammar remains a known gap;new record fixtures explicitly use existing make_fun/3. No default/global optimizer alteration:148 establishes no_ssa_opt solely for two preserved spawn sources;store explicit per-fixture compiler policy and process trap context in reports. Historical OtpBehaviourCases Server/Supervisor builders use a trapped fresh caller on both engines,matching the error-return contract established before144,without any source/independent expectation/hash rewrite. Fresh untrapped remains all other cases. Add two permanent context/policy checks.

Isolated source-directed drafts now pass4168 combined tests (107 new module contracts,four compiler diagnostics,three exports);two final host checks and full production/CI/original validation remain required. Preserve initial startup9/14,post-link10/14,missing-inspection four failures,wrong boolean catalog build,initial record41/45 and clock27/28 premises,plus missing example-file scaffolding failure. Consolidated altered Ericsson notices/full Apache license in both local packages;no source banners,draft-license commit,production BEAM or public package. Maintain explicit ports/distribution/scheduler/resources/native-record/time-warp/full-inspection boundaries. Require all2672 prior fixture sources/expectations/generated hashes and six exclusions unchanged,full4170 local plus pinned3648 original and same-head CI after publication. Journal0021 will close before149 would exceed32KiB;verify working and canonical Git-blob digests and append149 only to0022.
# Part151 - 2026-10-12 - Repair the native record guard boundary

Before implementation, inspect150 immutable abort/149 ledger and pinned OTP-29.1.1 lib/stdlib/src/erl_lint.erl gexpr is_record clauses. Reuse existing C# AST literal types, guard validator and diagnostic corpus; no external library or production BEAM dependency. Native is_record/3 guard requires a literal atom name and literal integer or atom third argument; it does not require a positive integer (correct the provisional150 next-step wording). Keep weak dynamic body/apply BIF behavior unchanged. Reject dynamic guard operands with the existing IllegalGuard diagnostic and independent native illegal_guard_expr expectation. Preserve the published invalid positive source in150 and migrate that exact source into a rejection contract; replace its positive slot with a newly named literal guard source and record an explicit before/after ledger. Add separate local/remote dynamic-name/dynamic-size/nonliteral guard rejections and literal integer/atom guard positives. Do not add exclusions, change historical expectations or mask the native compilation abort. Validate locally then full source-built OTP and same-head CI before declaring repair verified.

## Part153 - 2026-10-12 - Record expansion and compiled tuple-test repair plan

Before implementation,read152 complete3654/3657 failure and pinned erl_expand_records.erl literal integer nonpositive clauses,sys_core_fold.erl simplify_apply/get_fixed_args,beam_core_to_ssa.erl call_type/is_guard_bif/internal_cg is_record,and erl_bif_op.c weak is_record3. Reuse C# AST,existing argument evaluation/bindings and immutable tuple terms;no dependency. Source suppression only applies to a literal integer node before Core folding;unary +/- nodes must execute operands. Native Core converts known literal atom tag/positive integer size (including big integers) into tuple/arity/tag checks after argument evaluation;literal proper apply3 can become that direct call. Actual weak dynamic BIF still requires small integer size or native-record atom. Adapt those selected compiler contracts without weakening runtime validation. Preserve152 failures/sources and explicitly correct only three invalid independent premises using source/actual native evidence,with a before/after ledger. Add positive big literal/side effects,unary zero/negative,proper-static-apply and genuinely dynamic weak-BIF cases. Keep historical sources/expectations/generated hashes unchanged outside the named ledger exceptions,no new exclusions/default optimizer policy. Full local/source-built native/CI proof required;identifier/character and HTTP packet drafts remain isolated until repair proof.

### Part153 execution decision - 2026-10-12

After publishing immutable152 failures and auditing their exact expansion/Core/weak-BIF paths,combine the repair with the independently tested identifier/character draft in one production checkpoint/full pinned run. This replaces the provisional isolation-until-repair-proof scheduling choice above;it does not change proof requirements or declare the repair verified. Draft4320/full passed118 new modules/17 scanner diagnoses/two host checks/four language exports,with2782 old source/hash/expectation contracts preserved. Integrate selected files only,retain main record/Execution repairs and explicit three-premise correction ledger,then run full combined production pipeline and new-head oracle/CI. This avoids a separate pinned compiler rebuild for the already tested draft. Packet75/75 framing/TLS remains entirely isolated until HTTP/reference validation;no packet export promotion.

Identifier/character reuse/source decisions were recorded before their isolated implementation in artifacts/identifier-text-prototype/implementation-plan.md. Pinned bif.c text/unsigned field rules,utils.c proper byte lists/64 and38 byte bounds/NUL validation,erl_printf_term.c reverse identity fields,erl_node_tables.c/channel atom retention/atom.names false index0,and erl_scan.erl Unicode/escape/scanner errors guide the port. Reuse actual C# term identities/bounded concurrent atom registry,SDK spans/unchecked arithmetic/Rune decoding;no dependency. Preserve Term.ToString/ETF debug behavior and common ten-Atom cache. Foreign channel indices are backend specific;only actual disconnected local parse is implemented,with no alive/OS port fabrication. Compile malformed lexical fixtures through an explicit erl_scan framed diagnostic route;all103 existing compiler diagnostic commands remain unchanged. Invalid UTF16 stays local-only;full scanner annotations/continuations/quoted-string escape expansion,connected distribution/ref magic identity/physical resources remain unsupported. Consolidated altered Ericsson notices required before the production package check.

## Part155 - 2026-10-12 - Packet decoding source/reuse plan

Read the isolated implementation-plan before promotion and exact pinned erl_bif_port.c decode_packet3/options/HTTP/TLS callbacks,packet_parser.c framing/HTTP catalogs/token normalization/URI/TLS,packet_parser.h body stripping/dispatch,and decode_packet_SUITE.erl bit offsets/prefixes/limits/neg/HTTP/IPv6/line/TLS cases. Reuse existing immutable BitString/terms/registry,SDK BinaryPrimitives/read-only spans and ordinal catalog lookups;no external parser/network library/production BEAM. Native HttpClient/Uri parsing would change permissive OTP request/version/URI/error and header continuation rules,so adapt original control flow. Cache only semantically owned immutable protocol/header/method atoms. All selectors and options are implemented together;no unsupported HTTP stub is promoted. Managed owned copies replace native subbinary views;entire raw input reuses its immutable binary. Resource/allocation/error-stack metadata and native unsafe overflow slices remain explicit limits. Before promotion standalone289 tests,then4619/full and expanded4650 tests pass;exact new-head original proof remains required. Preserve all old2909 source/independent expectations/generated hashes/caller policies and120 diagnostic contracts/six existing exclusions. Consolidated altered Ericsson Apache credits required;public-license draft excluded. No new source license banners,dependency,public NuGet or physical OS port claim.

## Part157 - Current-code and process-table source/reuse plan - 2026-10-12

# Module loading queries draft plan - 2026-10-12

Main155 b4db97e stays frozen for original4121/CI4650. Before implementation read pinned OTP-29.1.1/ad058237 beam_bif_load.c module_loaded1/loaded0 and erlang.erl loading docs,actual ModuleRegistry register/Apply rollback,ModuleDefinition.Register and OtpModules installer. Native module_loaded requires an atom and current code whose on_load is completed;loaded lists current or old code units. Implement actual owned CLR current module state,not a native-name whitelist or fabricated constant. Registered runtime/OTP callbacks publish real owned module code;compiled definitions publish even with zero user exports. ModuleDefinition registration becomes atomic;ModuleRegistry rollback restores both exports and module state. A copied immutable snapshot protects loaded-query concurrency. Current backend has no pending on_load/hot-code/old-version lifecycle;do not claim those paths or implement check_old_code via a constant. Reuse existing registry gate/sets/immutable result terms and compiler definition;no external loader/reflection/production BEAM. Read source/license before port;consolidated Ericsson beam_bif_load copyright/full Apache notice required before promotion. Add independent native modules for current/unknown/nonatom/list uniqueness/backend-variable module inventory and host zero-export/rollback/snapshot/concurrent publication tests. Preserve all3229 old fixtures/hashes/expectations,120 diagnoses,778 expressions/six exclusions. Full local then same-head exact original proof required. No whole module,physical code unload/NIF/default module_info metadata/performance claim.

Execution: initial draft4671/full passes,including18 independent native-source query cases/two direct MFAs/one host lifecycle check. Strengthen the host test to parse a real zero-export module and start a dedicated reader while a writer has published only one of two exports;observe that reader blocks before releasing the writer. Separate test deadline/observation constants;host probe passes. Optimize owned query snapshots with a dictionary retaining one immutable Atom per actually loaded module;fresh result cons cells are built directly under the gate without LINQ/intermediate arrays. Restore the same Atom objects on rollback;no global atom-object cache extension or native-speed claim. Main stillfrozen155 until156 original result. Full main pipeline/reference proof required after promotion.

Process-table extension plan before implementation:read pinned erlang.erl processes_iterator/processes_next wrappers,bif.c erts_internal_processes_next1,erl_ptab.c erts_ptab_processes_next and25 scanned indexes per reduction,and erl_process.h ERTS_DEFAULT_MAX_PROCESSES=(1<<20). Inspect actual ProcessRuntime creation/removal/monitor/link/initializer lifecycle. Add a real bounded owned sparse slot table with freed-slot reuse and distinct existing monotonic opaque Pid identities;include initializer in capacity,throw actual system_limit before starting excess work,and free slots atomically before completion notifications. Sparse sorted occupied indexes avoid allocating a million-entry array or repeatedly scanning absent slots;batch cursor/maximum result/scan window follows original structure,with an explicitly separate managed work budget instead of pretending native reductions. No snapshot-as-cursor facade. Preserve the wrapper's deliberately unvalidated cons-head/cursor/tail passthrough,empty-list private cursor checks,inclusive capacity endpoint and none termination. Cursor state is opaque and inventory/backend scheduling differs;do not claim native lock-free tables/pid bit-permutation/full reductions. Add independent original corpus plus meaningful host capacity/reuse/stale-Pid/teardown/buffered-dead-Pid checks. All inherited query/source/fixture contracts must remain unchanged;full local/native proof required before promotion. Consolidated source credits:erlang.erl/bif.c Ericsson1996-2026;erl_ptab.c Ericsson2012-2025;erl_process.h header credit to verify. No dependency,BEAM,source banners or public license publication.

## Part163 - 2026-10-12 - Owned module information plan

Read isolated implementation-plan before coding and pinned erl_bif_info.c query dispatch/default field ordering/current export/private code/NIF/HiPE contracts,erl_internal.erl real predefined functions and beam_asm.erl default version derived from code MD5. Reuse actual gated ModuleRegistry/Apply rollback,immutable terms,compiler IR/local identities and SDK FrozenDictionary/MD5/assembly identities;no new dependency or production BEAM. Cached immutable snapshots are a C# optimization that must invalidate on real publication and restore on failed transactions. Keep source-declared arrays separate so old generated constructor output remains stable. Bare module names without owned code provenance do not fabricate metadata. Actual C# metadata is backend specific;full hidden/anonymous/unused optimization,user attributes/hot code/options and native resource/performance contracts remain unfinished. The consolidated source notice must precede local package validation;owner license stays untracked.

## Part165 source-attribute plan

# Owned source attributes plan - 2026-10-12

Freeze published163f64c951 for1644311 native comparison/4855 CI. Read OTP-29.1.1 erl_parse.yrl build_attribute/attribute_farity/normalise/term,v3_core attribute proper-list preservation/scalar wrapping,beam_core_to_ssa attributes/include_attribute and beam_asm build_attributes/set_vsn/module_md5 essentials before code. Retain custom source attributes in exact declaration order including repeated names;proper-list values remain lists,scalar/improper lists wrap once. Explicit vsn stays unchanged;default version is prepended only if absent. Code MD5 excludes source metadata chunks;do not hash arbitrary attributes into local function identity or actual code fingerprints.

Reuse typed immutable terms/compiler AST/bit construction/backend canonical serialization and gated registry descriptors/cache. Normalise literal atoms/numbers/strings/list tails/tuples/maps/constant binaries and direct signed numeric literals;preserve farity-to-tuple rewriting inside attribute values but not map keys/binary expressions. Reject computations,variables,matching/effects and compile/import/on_load/NIF/type/spec/native-record control attributes that are not implemented,never silently pretend they are passive metadata. Current no_ssa_opt/records/module/export handling stays unchanged. Full remote fun literals/documentation/type annotations/compiler directives remain scoped rather than false broad normalise parity.

ModuleDefinition gets an optional immutable source-attribute list;generated C# constructor gains a fourth argument only for actual attributes,all3407 inherited hashes unchanged. A separate serialization option excludes metadata for canonical code identity/digest;all old identity digests stay unchanged. Runtime descriptor/snapshot includes real attributes and preserves source/default vsn rules under existing publication/rollback gate. General declaration APIs and complete user compile options/hot code/NIF/optimizer inventory remain unfinished.

Independent permanent source scenarios cover explicit vsn scalar/list/binary/map/string/negative,float,custom atom/list/improper/tuple/map/bits/function-arity values,repeated ordering and default vsn/list preservation. Host proof rejects executable attribute expressions/unsupported semantic controls without effects and verifies attribute-only edits do not change code/fun fingerprints. Whole isolated/main pipeline/notices/old corpus and diagnostic routes guard before promotion;native164 must be retained before promoting165. Preserve all old expectations/context/policy/exclusions and record every failure before correction. No new dependency/production BEAM/source banners/public owner-license or NuGet publication. Consolidated altered-source credits:erl_parse/beam_asm Ericsson1996-2026;v3_core Ericsson1999-2026;beam_core_to_ssa Ericsson2023-2026 (verify headers).

Part167:retained source-built OTP discrepancy proves v3_core.erl attribute/1 uses is_list/1 as a list-tag check,including improper cons. Reuse existing immutable Cons/Nil type checks; a proper-list walk is semantically incorrect. Correct Values and the single published wrong assertion with immutable before/after ledger;add four native boundary fixtures. No new dependency/license/package publication;owner license remains untracked.

Module deletion candidate audit:use existing ModuleRegistry gate,transaction rollback and immutable callback/descriptor snapshots;no additional package. AssemblyLoadContext unloading operates on assembly ownership and cannot implement Erlang per-module export deletion or retained local functions;do not use it as a substitute. Read exact pinned beam_bif_load.c current-to-old transitions and current/old loaded catalogue distinctions;per-process active code/purge requires separate ownership work.

Explicit same-module remote calls use the existing current registry dispatcher;source audit v3_core.erl expr/2 keeps remote icall distinct from local iapply. Remove the compiler's old self-qualified shortcut;add one permanent delete-then-explicit-call comparison. Existing unqualified local calls/closures retain old code. General remote tail/code-frame tracking remains pending before CPC/purge.

Source tail candidate audit:reuse typed source IR,actual per-entry owner registration and the existing loop/ValueTask backend. Reject reflective delegate-target guessing and actor frameworks as code provenance substitutes. Source-directed transfer matches pinned remote call_ext_only/local call_only intent;this CLR adaptation does not establish physical BEAM frame/resource or faster-than-OTP performance. NativeBuiltin/anonymous/local-function tail generalisation and execution version leases remain separate scope.

Canonical external ownership audit:reference identity in RuntimeFunctionEntries,not only equal ExternalFunctionIdentity metadata,proves the standard cached current-dispatch callback. Preserve arbitrary C# external callbacks even when metadata equals a canonical entry. Focused failed/passed evidence precedes full validation;no package/dependency change.

Part173 static runtime callback tail audit:read pinned beam_ssa_codegen build_call/build_fun_call separately. Reuse existing frame scopes/ValueTask/control transfer;no new dependency. Static qualified non-erlang callback transfer releases source frame;general fun call_fun retains continuation. Do not generalize canonical metadata into compiler lowering provenance. Actual image admission/CPC/purge pending;no native performance claim.

Part175 source-image foundation:reuse actual source callbacks/IR owners,registry gate,immutable old snapshots and separately locked active counts;AsyncLocal identifies the local owner but cannot serve as another process execution inventory. Carry exact image on local tails even when the same IR object is republished. Generic call_fun retains caller continuation;canonical external metadata does not prove static lowering. No new package/native code-resource/performance claim;CPC/purge admission and matching-code fun retarget remain separate scope.

Part177 CPC audit:reuse real current/old image counts and owned process pending-signal admission/selective mailbox. Each request has reference identity even for equal IDs;termination replies false before discarding pending signals. Preserve synchronous callback throws/caller AsyncLocal restoration while asynchronously held callback image counts remain live. Cache two bounded immutable protocol atoms. Native priority/dirty schedulers/physical stacks/literal GC/purge admission remain unfinished;no external framework/new package or fabricated false answers.

Part179 source fun audit:reuse canonical owned code descriptors,source publication owners/image handles and existing tail transfers. Inspect pinned erl_fun.c and beam_ssa_codegen.erl;matching metadata on arbitrary host callbacks does not prove compiler ownership. No new dependency. Native purge admission/resources remain separate scope.

Part181 purge audit:reuse immutable image identities,registry ownership gate,queued real CPC/monitor/termination and SDK SemaphoreSlim/TCS/CWT. Separate image reservations from AsyncLocal flow;keep local-tail reservations continuous;new old-fun waits never count as executing old code and still admit control signals. Source wrapper purge_module differs from inline BIF calls. Read pinned erts_code_purger/erlang/erl_fun/beam_bif_load;no new dependency. Owned cooperative workers are not native global actor/process capacity/kill concurrency/autoload/physical resources or uncooperative CLR preemption. Parent Git metadata is shared by isolated drafts:never commit while a draft pipeline is running;SDK SourceRevisionId changes invalidate incremental output.

Part183:reuse .NET10 Stream/ArrayBufferWriter/Rune and immutable BitString.WriteTo for actual raw bytes,not TextWriter transcoding. Preserve three-argument ProcessRuntime constructor ABI;new overloads borrow streams. Default Windows standard handles wrap borrowed unbuffered FileStream independently of Console.OutputEncoding;conditional Unix stdin uses real TTY ioctl,never exercised by development checks. Pinned erlang/bif/utils audited;no new package/source copying/production BEAM. Framework MIT notice remains. Native errno cause/resource/scheduler/partialwrite/realTTY verification unfinished. Raw oracle BaseStream capture verifies independent new outputhex;old sources/outcomes/hashes/contexts/policies unchanged.

Part185:reuse owned process registry/gate,mailbox control wakeups,SDK TCS/cancellation and actual timer handles. Pinned erlang/erl_process/erl_proc_sig_queue/erl_hl_timer audited;no new package or actor framework. Synchronous acknowledgment occurs only on actual cooperative pause;per-owner lifetime and receive/PID timer deadlines are retained. Native dirty scheduler/preemption/cross-owner signal races/cycle diagnostics/counter exhaustion/resource/performance unfinished.

Part187 prototype:source-guided iterative native term printer using SDK ArrayBufferWriter/Rune/UTF8/BigInteger/scientific formatting and actual owned streams. Reuse source-owned identity/channel fields;never approximate with general Term.ToString. Preserve raw Latin1 strings versus UTF8 atoms and ignored output write failures. No new dependency. Qualified literal fun syntax uses pinned v3_core make_fun lowering;dynamic syntax/guard folding and full native metadata/HAMT/platform/resource/upstream/performance contracts remain unfinished.

Part189 prototype:reuse owned runtime identity and actual decoded PID/port/reference nodes with SDK weak references/CWT;do not pin foreign containers or share registry across runtimes. Iterative traversal supports nested lists/maps/tuples/fun environments. Pinned type union/options validation and local/known metadata ported. Networking/dynamic naming/native GC timing/host imports remain explicit prerequisites;no fabricated connected entries/new package/performance claim.

Part191 prototype:reuse actual owned spawn/atomic monitor/link,group leader inheritance and termination/slot cleanup. Port pinned local shortcuts and unconnected crasher behavior instead of fabricated remote PIDs. Source spawn2 legacytuple dispatch retains apply2 badfun behavior;do not substitute directMFA invocation. No dependency. Real networking/remote code/handshakes/logger warnings/resources/upstream/performance remain unfinished.

Part195 draft:reuse actual bounded AtomNameRegistry indices,owned immutable maps and iterative printer;no package. Flatmap direct atoms use source internal identity order while language comparisons remain exact semantic order. Explicit bounded byte alternatives keep all original payloads and reject every other byte sequence;no normalization or erasure of failed192 proof.

Part195 combined draft also reuses actual auth name lookup and existing gen_server infinity protocol;four erlang cookie wrappers do not create fake auth state or read user cookie files. Automatic auth/distribution startup remains unfinished.46 cookie scenarios and5236/full separate cookie draft pass before combining with the5187/full display-order draft.

Part197 prototype:source-directed node monitor/disconnect/trap current unconnected-state port. Reuse actual runtime identity,mailbox/name registry,existing lists member short-circuit and real gen_server kernel infinity requests. No actor framework or dependency. Connected channel/monitor counts/automatic-connect/dynamic-naming/transport remain unfinished;do not fabricate a connected node or a successful disconnect flag. Reformat source-wrapper classification as semantic switch arms without changing historical signatures.

Part199 prototype:reuse actual owned process control admission/death cleanup,registry export dispatch and mailbox selection hooks. Port pinned saved-call ring replacement/chronological observation into a bounded SDK array with allocation-free disabled fast path. No package/framework/production BEAM. Record actual logical exports/send/receive/timeout events;native compiler/JIT internal MFA choices and physical scheduling/reduction/heap/resource equivalence remain explicit scope. Cross-owner flag updates must run via real control requests,not shared flag mutation.

Part201: source-directed C# adaptation of OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 erlang.erl port wrappers, erl_bif_port.c open/settings/data lookup, io.c port connect/exit/info/data signals, erl_port.h default information keys and erl_bif_info.c item construction. Copyright Ericsson AB 2001-2025, Apache-2.0; modified for CLR Process/Stream resources and the existing owned runtime/mailbox. The external-process backend has real handles, linked owners, registered names, actual byte pumps, EOF/exit-status and immutable data storage. Driver call/control callbacks are absent and return badarg as the original spawned driver does. Unported: native driver ABI/fd/stdio drivers, all options/metadata, exact busy watermarks/async scheduling/platform errno and distribution; incoming packets have an explicit managed16MiB resource cap. No production BEAM, new package, source banners or owner-license publication.

Part203 prototype: altered source-directed C# adaptation of OTP-29.1.1/ad058237 io.c read_linebuf/flush_linebuf and erl_bif_port.c line/packet settings. Preserve the original Main/Full/CR-inside/CR-after transitions, byte boundaries, CRLF stripping, exact-capacity newline and two-stage deferred-CR EOF flush. SDK StreamReader.ReadLineAsync cannot preserve raw bytes or noeol chunk boundaries;reuse existing Process/Stream and a bounded byte accumulator,no package or actor framework.20 new source cases and inherited28 port cases pass in focus. Managed line buffers have an explicit16MiB limit,separate from packet limits. Native driver/fd/stdio/busy/platform/resource/upstream/performance remain incomplete. Correction:Unix sys_drivers.c spawn_control has a private forker/SIGCHLD callback;the selected ordinary guest control operations return badarg. The managed backend does not reproduce that private native memory protocol;Windows spawn driver has no control callback.

Part203 final draft adds a21st source case proving that explicit port_close discards a buffered partial line. The first draft incorrectly flushed that tail on close;io.c only flushes it for EOF before soft EOF/termination. Preserve the failed focused scenario and corrected source control flow. Correct source-wrapper arity classification for open_port2/port_connect2/port_control3 and the earlier generic no-control-callback comment;host asserts wrong arities remain unclassified. No inherited fixture,expectation,compiler policy or exclusion changes.

Part205 prototype: altered source-directed port of OTP-29.1.1/ad058237 bif.c monitor(port,...),link/unlink/send_exit_signal_bif and io.c erts_deliver_port_exit/port monitor termination. Reuse actual port table,shared process monitor/reference/alias ownership,mailbox admission/priority and death cleanup;no new dependency. Port monitors accept local identity/name/local name tuple;remote port/name rejects badarg. DOWN reports port kind and original target,custom tags/priority/alias modes preserved;demonitor and observer death use actual shared entries. Explicit exit/exit_signal normal also closes non-owner's port;kill becomes killed. Owner/link death normal-drop remains separate. Priority port links and dead local link failures follow pinned source. Actual monitored_by client entries and absence of driver process monitors are queryable. Native ABI/fd/stdio/physical scheduling/busy/distribution/resource/full upstream/performance remain incomplete.

Part207 prototype: altered source-directed adaptation of OTP-29.1.1/ad058237 Unix sys_drivers.c output and Windows sys.c output,with io.c call_driver_output. Packet1/2 output exceeding255/65535 is accepted as a driver command then closes the actual port with einval;it is not invalid iodata/badarg and must notify real links/monitors. The corrected backend emits original-shaped EXIT/DOWN and removes the resource;no OS bytes are written. Reuse current real port cleanup/notifications,SDK immutable bytes and independent protocol width.12 source cases cover inclusive maxima,one-byte overflow through command2/3 and Port ! command,empty packets and malformed iodata preserving a live port. Incoming managed16MiB packet/line policies are separate and unchanged. No new package/framework/production BEAM;native ABI/fd/busy/full scheduling/platform resources/performance remain unfinished.
Part209: source-directed adaptation of OTP-29.1.1/ad058237 erl_bif_port.c option parser, erl_driver.h watermark range, Unix sys_drivers.c outputv/ready_output hysteresis. Reuse SDK Process pipes and async writes; no package or actor framework. A real byte queue replaces semaphore ownership as the busy condition; accepted writes account bytes, high watermark suspends admissions, drain below low or empty wakes senders, nosuspend declines busy admission. Packet headers contribute to queued bytes. busy_limits_port accepts disabled and positive native-size pairs with low clamped to high; atom line selects512; parallelism selects queued worker versus immediate async write initiation. Completed task tracking is pruned. Physical OS partial-write queue measurements and BEAM scheduling remain distinct from managed pending async bytes; msgq watermarks/native fd/argv0/shared stderr/options remain subsequent work, not silently accepted.

Part211 prototype: reuse actual port table,shared byte queue,SDK async continuations and owned process cancellation for distinct process-to-port message pressure. Reference erl_port_task.c and bif.c;Apache-2.0 Ericsson source attribution in consolidated OTP-PORTS.md. No package/actor framework. Preserve generated module sources/hashes;Execution's async send branch and registered send wrappers await pressure while synchronous host-only helper retains enqueue behavior. Ports have real message bytes rather than synthetic result flags. Physical BEAM scheduling/memory and OS fd/argv0/shared stderr remain separate scope.

Part213 prototype: reuse SDK Process for ordinary ports and typed owned native children only for descriptor/argv0/overlapped options;no package/framework/production BEAM. ProcessStartInfo cannot share child stdout/stderr or replace argv0. Use actual Windows handles/CreateProcessW and Linux64 GNU libc posix_spawn file actions,with cancellation-safe nonblocking parent pipes and owned child reaping. Original sys.c/sys_drivers.c direction/stdio/exit/error differences are preserved rather than merging independent readers. System API/ABI documentation consulted: https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw ; https://learn.microsoft.com/en-us/windows/win32/procthread/creating-processes ; https://raw.githubusercontent.com/bminor/glibc/master/posix/spawn.h ; https://man7.org/linux/man-pages/man3/posix_spawn.3.html . No glibc implementation copied or redistributed;system dynamic interface use only. All remaining option spellings now have concrete parsing/backend behavior in this prototype;full native driver/fd formats/platform/resource/errno/signal/performance scope remains partial. Sandbox named-pipe connection denial is recorded separately from repaired compile/quoting/search/backend defects.

Part215: fd ports borrow existing OS resources rather than launching Process. Audit pinned erl_bif_port.c descriptor/small-integer/name/settings parsing and sys/unix/sys_drivers.c fd_start/fd_stop/control plus sys/win32/sys.c translate_fd/fd_start/threaded_reader. Reuse Stream/Task, SafeFileHandle, runtime ownership/monitors, line/packet/pressure queues and generated module compiler; no package or production BEAM. Linux readiness uses libc poll with zero timeout and cancellation-aware delays, blocking writes use workers as the original async fd writer does, and close does not close caller descriptors or alter flags. Windows handles0/1/2 translate to standard handles, other values to handles; ReadFile/WriteFile use worker threads with CancelSynchronousIo, THREAD_TERMINATE access and cancellation retries to cover the pre-call race. Per-read CR/CRLF translation follows the Windows source, separately from Unix byte-preserving reads. System interfaces consulted: https://man7.org/linux/man-pages/man2/poll.2.html ; https://learn.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-cancelsynchronousio . No system implementation code copied/bundled. Concurrent external descriptor close/reuse/read, TTY reopening/flags/window-size control, native ABI and complete resource/scheduling contracts remain explicit limitations; selected OS pipe/file and inherited descriptor tests do not prove those contracts.

Part216: direct original comparison corrected fd metadata/settings: erl_bif_info.c/erl_port.h return the os_pid tuple with undefined in singleton and default info; erl_bif_port.c rejects exit_status for non-spawn drivers. Reuse the same borrowed OS streams and lifecycle;no resource identity/byte normalization or new dependency. The independently expected done outcomes and all prior4033 sources/generated hashes are preserved;five new Part215 source assertions and five independent IO templates are corrected explicitly. The oracle reporter now checkpoints every64 accumulated cases plus every new failure,final completion and caught infrastructure abort,avoiding a full growing JSON rewrite after each success while retaining every final row and unmodified comparison logic. Hard process termination can lose up to63 recent progress rows;it cannot produce a completed/successful report. Unit regression and the existing unavailable-oracle full-pipeline check guard final/failure/abort saves. This accelerates development reporting;it is not an Erlang execution benchmark.

Part218: source-guided spawn_opt/2,3,4,5 selected creation contracts. Audit erlang.erl forwarding/tuple-function guards and local/remote crasher behavior,bif.c spawn_opt_4 option/type failures,and erl_process.c erts_parse_spawn_opts/erl_create_process monitor/link installation. Reuse real ProcessRuntime process table/group leader/source callbacks,Monitor/alias/tag/priority delivery infrastructure and error-info capture. SpawnWithOptions holds the existing runtime gate through creation,link flags and monitor registration,so child termination cannot precede monitor installation. No actor framework/package/production BEAM. Link/monitor option sequences overwrite flags exactly as inspected;tuple callbacks intentionally create a child which fails badfun through apply/2,as the baseline does. Connected distribution and non-link/monitor spawn options (CPU priority,heap/GC/message-queue allocation hints,async_dist/scheduler) remain unfinished rather than being silently accepted as effective. No physical scheduling/resource/full module compatibility claim.

Part220: source-guided hibernate/0,3 and delay_trap/2 adapt OTP-29.1.1 erlang.erl, bif.c and erl_hl_timer.c. Reuse the existing runtime mailbox, cancellation, module registry and immutable terms; no new package or production BEAM dependency. Hibernate/3 discards the old language catch/after stack and resumes its MFA after a message without consuming it; hibernate/0 retains continuation. Delay uses selective receive and original unsigned32 timer limit, with segmented CLR waits. CLR cannot reproduce per-process BEAM heap shrinking or physical scheduling; those contracts remain partial.

Part221: original5033/5034 exposed one hibernate256 mismatch. Reinspect bif.c courtesy loop and beam_common.c apply export-stub resolution:MAX_ARG255 bounds courtesy scanning,not wake apply registers. Remove incorrect wake cap and let existing module registry resolve missing arity256 as undef;rename owner constant MaximumCourtesyArity. Preserve independent done outcome,explicitly correct only that new220 source expectation and add255/256 dormant-before-undef checks. No dependency/exclusion change;initialraw5033/5034+fd8/8 retained.

Part223: source-guided pure format_cpu_topology/1 adapts pinned erlang.erl cput_i2e consecutive grouping/order/unspecified levels/logical leaf/error contracts. Actual garbage_collect/0,1,2 adapts erlang.erl,bif.c,erl_process.c options and system-task/result protocols,using real global CLR System.GC major/minor and existing owner admission/termination. Distinct request identity preserves duplicate equal async IDs. Original exported garbage_collect_message_area/0 is nif_error(undefined),with no BIF;preserve its error without dummy collection. Inspect/reuse existing mailbox,process/runtime,immutable term/registry and .NET libraries;no external package/framework/production BEAM. CLR global heap differs from BEAM private per-process heap/root scanning/reductions/dirty tasks/max_heap/scheduler/resource semantics;do not claim those. See OTP-GARBAGE-COLLECTION.md for source audit.


Process-info prototype: before implementation audit pinned erl_bif_info.c pi_1_keys/process_info_bif/pi_args/process_info_aux/current_function/current_stacktrace and erlang.erl declarations; inspect .NET ProcessContext AsyncLocal frames, Mailbox pending signals/waits, ProcessRuntime dictionaries/monitors/suspension/death. Reuse existing immutable terms, queues, reference correlation and Task cancellation; no package/actor/production BEAM. Actual managed observable fields are appropriate; BEAM heap/reduction/resource default vector is unavailable, so process_info/1 remains pending. Source/notice/limitations: docs/OTP-PROCESS-INFO.md.

Sequence tracing prototype (2026-10-13): inspected existing C# ProcessRuntime,Mailbox,ProcessContext,ErlangOperatorValues,spawn/monitor/alias helpers and ErlangClockState as reuse candidates. Reuse immutable Terms,actual contexts,shared runtime gate,selective mailbox matching,reference ownership and real wall/monotonic/unique clocks;no actor framework,external tracing package or production BEAM. Native source control flow cited in OTP-PORTS.md defines token and default tracer protocol. Mailbox carries opaque token snapshots plus a separate preserve sentinel;the receive hook runs after releasing mailbox lock to avoid inversion with runtime-gate sends,and existing saved-call callbacks remain separate. Trace delivery is an actual message to the configured alive local PID,with atomic tracer switching,dead-tracer clearing and self suppression. Synchronous spawn descriptors expose the actual Erlang callback/MFA/options/args and share the monitor ref;only actual matching messages update receive clocks. Preserve all inherited compiler policies/contracts except root's separately evidenced hibernate256 correction and two appended boundary cases. Port/NIF tracer support,connected distribution,async spawn reply integration,all exit/DOWN/control flows,sensitive/reset operations and native scheduler/resources remain unimplemented rather than producing fake events or unused-option success. Existing system_flag/system_info dispatch must be composed with other agents at integration;these registrations implement only sequential_tracer. Owner license draft remains absent from archive/prototype/packages;source notice banners are not introduced.

Async spawn-request/sequential-trace integration adapts pinned OTP-29.1.1 erl_process.c erts_send_local_spawn_reply and local spawn token handling,bif.c send_error,under retained Ericsson AB Apache-2.0 notices. See OTP-SPAWN-REQUEST-SEQUENCE-INTEGRATION.md. Actual child reply tokens travel with pending success messages;parent clocks change at actual matching receipt. Error replies carry no token. Existing immutable terms and process/mailbox/monitor/alias lifecycle reused;no dependency or physical scheduler/distribution claim.

Integrated spawn/native/compiler slice reuse audit: inspect pinned erlang.erl,bif.c,erl_process.c,dist.c for spawn_request;erl_record.c/h,records.erl,erl_bif_op.c,external.c/h,utils.c,erl_term.h,erl_term_hashing.c for native immutable values/ETF/order/hash;erl_parse.yrl,erl_expand_records.erl,erl_eval.erl,erl_lint.erl,erl_internal.erl for syntax/types/defaults and diagnostics. Reuse existing .NET BigInteger,immutable term tables,actual process/mailbox/monitor/alias/module lifecycle,compiled metadata,Roslyn code generation and parser/token owner types;no dependency/actor framework/production BEAM. Exact record headers/notices appear in native-records-prototype.md,native-record-syntax-prototype.md,native-record-completion-prototype.md and OTP-PORTS.md. Two numeric foundation hash assertions are SDK-derived pending original proof;no independent-native claim or exclusion. Partial per-process heaps,complete lint/warnings/code staging,connected distribution and full native resources remain gaps.


Async error-token source correction: pinned erl_message.c erts_queue_message uses am_undefined,not NIL;matched receive in emu/msg_instrs.tab preserves the prior sequence token. This is the actual child-null error path from erl_process.c. Adapt preserved-token mailbox sentinel accordingly;one unpublished fixture label assertion/name is corrected without changing its independent done outcome. Prior successful local assertions are retained as historical evidence;no published4186 contract or exclusion changes.


System-reply sequential token integration adapts pinned OTP-29.1.1 erl_process.c system-task replies,erl_proc_sig_queue.c process-info/RPC/suspension/DOWN replies,erl_hl_timer.c timer messages and erl_message.c/am_undefined receive semantics in msg_instrs.tab. Retained Ericsson AB/contributor Apache-2.0 notices/full license apply. Reuse actual recipient lookup/mailboxes/correlation/priority/timers with preserved-token sentinel;ordinary sends and EXIT remain distinct.20 appended source contracts and precise producer audit in sequence-system-message-audit.md;full validation/original comparison pending. No new dependency/native heap/scheduler claim.


Part 227 repair adapts pinned OTP-29.1.1 erlang.erl spawn_request overload redispatch and bif.c/dist.c terminal request validation; erl_record.c local/qualified field access (Copyright Ericsson AB 2024-2026), emu/ops.tab atom-ID dispatch (Ericsson AB 1997-2026), beam_core_to_ssa.erl field lowering (Ericsson AB 2023-2026), beam_ssa_codegen.erl construction/update lowering (Ericsson AB 2018-2026), and beam_validator.erl constructor-only underscore rejection (Ericsson AB 2004-2026). Retain the complete Apache-2.0 license and existing notices. Reuse actual managed lifecycle, module/list/error metadata, immutable native fields and compiler semantics; no new dependency or production BEAM. Three explicit published source corrections retain valid independent done outcomes; the exact rejected constructor source is preserved as a measured diagnostic. No exclusions or harness weakening. See spawn-request-wrapper-repair-audit.md, native-anonymous-access-repair-audit.md and quoted-underscore-validator-repair.md.

Part229 raw-oracle failure diagnostics reuse the existing process capture and report checkpoint. Retain exact native stdout/stderr, exit code and fixture/stage before ETF decoding; decoder, independent outcomes, exclusions and failure exit code remain unchanged. This is development tooling, not a production BEAM dependency or evidence of native compatibility. Existing source/license decisions remain applicable; no new package.

## Combined match-spec/process tracing integration
Reuse the frozen12-file managed match compiler/evaluator once, production tracing callbacks and actual runtime/immutable terms/mailbox/process-info infrastructure. No package or production BEAM. Source from OTP-29.1.1 erl_db_util.c, erl_bif_trace.c, erl_trace.c and signal queue code defines real matching, event flags and test-only fake destructive mode. Production RunMode is Production; caller/mutation effects without real adapters are rejected. Admission-trace suppression is independent of seq preserve sentinel, and callbacks execute outside mailbox lock. Controls/admitted count/snapshots, GC/request death fanouts, logical process-info frames and async spawn token protocol remain inherited.

Process display adapts selected OTP-29.1.1 erlang.erl5477-5505 (public error/RPC wrapper), erl_bif_info.c4117-4155 (type-first/self/owner validation), erl_proc_sig_queue.c3368-3431,3564-3616 (real owner RPC/reference/undefined-token reply), and erl_process.c14896-14999 (current-first diagnostic intent). Copyright Ericsson AB 1996-2026. All Rights Reserved. applies to erlang.erl/erl_process.c; Copyright Ericsson AB 1999-2026. All Rights Reserved. applies to erl_bif_info.c; Copyright Ericsson AB 2018-2026. All Rights Reserved. applies to erl_proc_sig_queue.c. SPDX-License-Identifier: Apache-2.0. Existing retained license/notices apply. Native stack/address/register formatting is not copied or fabricated; the real managed logical Erlang frame chain is printed to actual standard error, before self/owner success. Type/pid/death/original argument metadata and preserved-token reply are selected native contracts. No dependency/source banner; physical BEAM stack, NIF tracers, sensitive-process flag and scheduling/resource costs remain explicit gaps.

Linked EXIT sequence-token propagation adapts pinned OTP-29.1.1 erl_process.c:14080-14081 (death link producer), erl_proc_sig_queue.c:1627-1865 (serial advancement, immutable token copy and live-target send event),4633-4782 (trap/control admission), bif.c:1763-1764 (explicit EXIT passes NIL), erl_trace.c seq_trace_update_serial and emu/msg_instrs.tab:161-179. Retained Ericsson AB/contributor Apache-2.0 notices and full license apply. Existing immutable record/term ownership, process gate, pending mailbox and matched receive protocol are reused; no dependency or source-file banner is added. Explicit exit/2/3 and exit_signal keep NIL clearing rather than fabricated token propagation.24 independent native-ready module fixtures and64 asynchronous linked-death host snapshots; original comparison remains root-owned. Ports/distribution, NIF tracers, physical scheduler/heap and general process-trace events remain bounded gaps.

Statistics prototype source attribution: altered C# adaptation of OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 erts/emulator/beam/erl_bif_info.c statistics_1 and erl_time_sup.c elapsed/previous-call accounting (Copyright Ericsson AB1999-2026), plus erts/preloaded/src/erlang.erl declarations (Ericsson AB1996-2026). Retained Apache-2.0 notices/full OTP-LICENSE.txt apply. Actual BCL monotonic uptime and ambient process USER CPU replace selected native time queries; physical scheduler/reduction/GC/allocator/IO statistics remain explicitly unsupported. No source banners/private license/new package/production BEAM. See OTP-STATISTICS.md and statistics-branch-design.md for scope and source distinctions.

Integrated CPU topology adapts pinned erlang.erl cput_e2i/set_cpu_topology (Copyright Ericsson AB1996-2026), erl_cpu_topology.c validation/sorting/set/get (Ericsson AB2010-2025), bif.c dispatch (Ericsson AB1996-2026) and erl_bif_info.c query (Ericsson AB1999-2026). Apache-2.0 full license/notices retained. See cpu-topology-state-prototype.md and CPU-TOPOLOGY-COMPOSITION.md for selected logical configuration scope and physical affinity gaps.

Integrated system monitor adapts pinned queue threshold/hysteresis and real notification delivery: erlang.erl Ericsson AB1996-2026; erl_bif_trace.c/erl_trace.c Ericsson AB1999-2026; erl_proc_sig_queue.h Ericsson AB2018-2025; erl_proc_sig_queue.c Ericsson AB2018-2026. Altered C# adaptation under Apache-2.0; full terms in OTP-LICENSE.txt. Real ordinary mailbox admission/removal and preserved system-reply sentinel are reused; unsupported physical/resource events are not fabricated.

Repaired all-feature composition reuses existing immutable terms, actual runtime gates/mailbox/monitor/alias lifecycle, owner RPC, standard streams and one shared12-file match-spec engine. No external dependency/actor framework/production BEAM or private LICENSE.local.txt. Six joint source cases and one actual host audit the real composed protocols; no physical BEAM display formatting or native scheduling/resource completeness claim. See platform-repaired-composition.md.

Bounded system_profile adaptation: pinned OTP-29.1.1 erl_bif_trace.c3709-3850 and erl_trace.c406-445/2550-2564, Copyright Ericsson AB1999-2026, Apache-2.0, retained licenses/OTP-LICENSE.txt. Actual per-VM controls/timestamp-only PID/port ownership/previous state/death cleanup; recognized physical enabled effects explicit notsup, no fake profiling events. Existing immutable terms/live lifecycle/shared gate reused, no new .NET dependency. Detailed audit/scope/tests in system-profile-prototype.md. No original proof or whole-module claim.


2026-10-14 priority permission candidate: reuse existing immutable terms, Runtime gate, invisible Mailbox pending controls, real alias/monitor/port identities and .NET dictionaries/tasks. No external actor library or duplicate permission cache: neither would provide native signal ordering. Pinned OTP branches guide newly written C# local LINK/UNLINK/ACK admission; distribution wire ids, physical BEAM queue layout and process_info/1 remain pending. Source freeze only; build/reference execution unrun.

Internal match-spec flatmap entry ordering reuses canonical AtomNameRegistry and exact TermOrder comparison. A ConditionalWeakTable caches immutable sorted read-only entries, avoiding repeated evaluation sorting; no new external dependency. Controlled atom creation is tested independently, without resetting native/managed tables. Native HAMT ordering and absolute index identity are outside this bounded slice.
Halt lifecycle foundation adapts selected immediate nonreturn/VM status intent from pinned OTP-29.1.1 erlang.erl3357-3502 and bif.c4309-4410 (Copyright Ericsson AB1996-2026, All Rights Reserved; SPDX-License-Identifier: Apache-2.0). Retained full license/notices apply. Altered CLR implementation is logical-runtime scoped, uses real gate/cancellation/module/mailbox/timer callback lifecycle and preserves embedding host/independent runtimes. No erlang halt exports yet; live ports/flush/options/subprocess/dump/abort remain pending. Reuse .NET Tasks/Timer.DisposeAsync and existing compiler/runtime/port probe; no package/dependency/source banner/private LICENSE.local.txt. See halt-lifecycle-foundation.md and parent repaired-halt-runtime-design.md.

Halt live-port prerequisite, 2026-10-14: numeric managed host selection and flush/deadline coordination adapt the pinned OTP-29.1.1 bif.c halt_2, big.c term_to_Uint_mask, erl_process.c erts_halt and erl_init.c halt timeout control flow. The library reports logical runtime outcomes and joins its actual owned port tasks; it does not invoke native process-wide exit. Erlang halt exports, abort/core dump, Unicode slogan parsing, connected distribution and native physical scheduling remain unimplemented in this prerequisite.

Actual bounded BEAM host execution reuses existing immutable terms, serialization, ProcessContext cooperative checkpoints and preloaded CoreModules registry; it requires no native BEAM production dependency, dynamic compiler, actor framework or simulated heap. Own bytes/read-only decoded models avoid caller mutation. BCL BigInteger/ZLibStream/BinaryPrimitives implement actual supported data transformations. ModuleRegistry publication and truthful generated metadata stay deferred to a separate transaction; InputSha256 is provenance, never native module MD5.

Actual bounded BEAM receive reuses Mailbox.ReceiveAsync for atomic exact-node consumption, controls/priority, tracing/token callbacks and segmented cancellation/suspension waits. No async selector under mailbox lock. Static valid timeout/pure matcher/zero frames are explicit bounded support; dynamic lazy validation and general save-pointer scheduling remain future cohesive APIs. No native runtime/package dependency.

## Based integer scanner prerequisite, 2026-10-14

No new dependency is introduced. Existing System.Numerics.BigInteger holds the selected based integer value; OTP-29.1.1 erl_scan source guides radix/digit/separator/error semantics with existing third-party attribution retained. Independent source fixtures and scanner error terms are declared separately from production conversion/diagnostics. Based floats, full scanner continuation and physical native resource limits are not promoted as complete. See OTP-BASED-INTEGER-SCANNER.md.

Captured closure host extension: immutable captures and known-target call_fun2 execute the verified original239 closure; original six/five provenance uses two retained batches. Runtime publication, generic fun execution, on_load, native function ETF and full frame/lease parity remain outside this subset. Source notices: Ericsson AB beam_file.c2020-2026, erl_fun.h2000-2026, erl_term.h2000-2026, beam_asm.erl1996-2026, beam_dict.erl1998-2026, jit/x86/instr_fun.cpp2021-2026, jit/asm_load.c2020-2026, emu/ops.tab1997-2026, Apache-2.0.

## Dedicated halt publication composition, 2026-10-14

Reuse the existing runtime gate, owned lifecycle/ports/cancellation, real Unicode io_request/io_reply mailbox/monitor protocol, immutable terms, compiler and OTP adapters. Optional runner installs three halt and two IO bindings; default484 Core/OTP bindings remain unchanged. No package dependency is added. Source/license attribution for altered OTP-29.1.1 selected halt/IO/dump-producer behavior is appended to OTP-PORTS.md with full retained OTP-LICENSE.txt.

A separate executable owns OS exit/abort and actual MDMP/ELF dump endpoints, preserving the reusable library and independent runtimes. Linux authorizes only its owned createdump child PID and resets permission, without host-wide security changes. A separate manual workflow builds the exact pinned original and executes predeclared fresh-process contracts, without ETF encoding or ordinary exclusions/normalizers. All24 raw strict contracts precede original observation;22 comparable contracts and2 physical format gaps have separate counts, with format-equivalence always false. Parent source-build verification and actual original execution remain necessary. See OTP-HALT-NATIVE-TRACK.md.

Mailbox callback isolation reuses BCL linked-list node identity, immutable entry snapshots, TaskCompletionSource signals and existing runtime-controlled admission. No actor package, spin timeout substitute, reflection mutation or native production dependency. Separate precedence/mutation epochs validate actual removal atomically; source/performance/concurrency gates remain independently required.



Halt original247 repair adapts pinned OTP29.1.1 big.c term_to_Uint32 (ETERM8 nonnegative native small with32-bit cast), erl_term.h small bounds, beam_common.c BIF formatter metadata, and erl_process.c/erl_init.c halt timer ordering into owned C# semantics. Reuse is source-guided adaptation, with existing OTP notices retained and no new dependency. The64-bit native small bound is2^59-1; native physical timer/run-queue races and BEAM crash-dump format remain explicit differences. Failed original24 evidence is immutable; corrected28 contracts require a fresh original run. Default Core remains484.


Part249 source-only default halt registration candidate: reuse the already adapted pinned OTP29.1.1 halt wrapper/BIF protocol through actual existing C# logical-VM/subprocess behavior. No new dependency/source adaptation is introduced. Planned default registration487 requires genuine repaired28 native proof and full exact-head gates; failed24724 remains immutable. Three typed owned-child MFA tests are distinct from ordinary returned-Term contracts; physical dump format equality remains unsupported.



Host prepared BEAM publication reuses existing C# ExternalTermFormat, immutable terms, native-significant-chunk BeamChecksumReader and ModuleRegistry image leases/rollback under its gate. IncrementalHash is actual .NET BCL MD5 for the native ordered input, not a whole-file fingerprint. Whole-batch preflight precedes registry mutation and purge-state changes are excluded. No production Erlang/BEAM dependency or extra package. On-load/native records/lambda resources and Erlang magic-reference wire restoration remain unsupported.


Prepared metadata first-term parsing reuses actual ExternalTermFormat.DecodeOne and immutable terms, with no extra native dependency. Used/trailing byte provenance stays distinct from native MD5 and whole-file source provenance. Existing descriptor default-version policy remains unchanged. Non-BIF function table order is preserved; native global export-index history requires separate real observations.

On-load HOST candidate remains pure managed code with existing BCL/runtime facilities; actual callback execution, registry tickets and Y locals do not add a package or Erlang NIF ABI. Source-only draft, old prepared/reader/oracle evidence retained; no default export or native reference capability claim.



Owned reference resource foundation is source-guided adaptation of pinned OTP29.1.1 external.c3644/4845-4854, erl_bif_unique.c198-291/295 onward and bif.c4792-4798 live magic-reference serialization/text lookup, with beam_bif_load.c loader-resource validation guiding the typed prepared payload. It introduces no native dependency, source banners or loading MFAs. Per-runtime weak lookup/live strong terms and typed leases are real managed ownership; exact3word/local creation0, physical native destructor/ABI, broader reference encodings and whole loading semantics remain explicit gaps. Source-only unbuilt foundation; existing notices retained.

## Native loader contrast exporter source proposal (2026-10-14)

Standalone tools/Compiler/Erlang.LoaderContrasts adapts control-flow guidance from pinned OTP-29.1.1 beam_bif_load.c (Ericsson AB1999-2026), beam_load.c (Ericsson AB1996-2026), external.c (Ericsson AB1996-2026), erl_bif_unique.h (Ericsson AB2014-2025), erlang.erl and code_server.erl (Ericsson AB1998-2026), under Apache-2.0. Original notices and pinned source provenance are consolidated here; no source banners or private license are copied. Ten independent source-declared loader contrasts (eleven disposable VMs) are unobserved until a future authorized original workflow. No runtime, registered MFA, corpus, exclusion or physical compatibility promotion is made.

Part265: inspect pinned sys_core_fold.erl simplify_apply/get_fixed_args and genuine257 direct-vs260 generic call disassembly before routing a literal loader target. Reuse existing compiler AST, argument evaluator, module dispatch and loader callback/code-lease implementation; no new package or native production dependency. Named syntax arities preserve distinct argument counts/indexes. This narrow route does not port general constant propagation or no_auto_import policy and does not establish full native stack parity. Existing C#/.NET licensing and notices remain unchanged.

Part266: reuse existing ErlangIoData flattening and BeamZlibMemberBoundary RFC1951 scanner, extracting its unchanged lexical DEFLATE body behind the unchanged RFC1950 header/adler contract. Inspect pinned erlang.erl prepare_loading/load_module and zlib.erl gunzip/reset with zlib_nif inflateEnd EOS/queue validation. Reuse BCL GZipStream for actual gzip CRC/ISIZE/FHCRC validation after strict physical-member preflight. Actual valid/invalid checksum and optional-header controls pass; no package or external production dependency, license/banner change or native allocation/performance claim. New semantic gzip header and resource-name constants retain distinct roles; value expansion matches previous passed tokens. Broader local boundaries are separate from the sixteen original observations.

Part267: reuse existing ProcessRuntime, ProcessContext cancellation/completion, system mailbox delivery and immutable BitString snapshots for an explicit HOST-only distribution queue. Inspect pinned dist.c controller/get_size/notification and gen_tcp_dist example before designing ownership; the unexecuted two-node probe is not evidence. Use BCL locked Queue/Dictionary and RunContinuationsAsynchronously-aware completion retirement; no actor framework, native production dependency or package addition. A separate bounded static protocol Atom reuses the genuine dist_data meaning and preserves source-domain ownership; 64 actual notifications share its object. Resource kinds, wire codec/authentication, node migration and atomic runtime ownership remain prerequisites. Existing licensing/notices unchanged.

## Part283 original TT8 tooling reuse (2026-10-15)

Reuse the eight unchanged published signal collector process-capture helpers as compile-time C# source links, with exact source/assembly bindings. Use BCL process, hashing and JSON facilities; no new package or production BEAM dependency. Inspect pinned OTP29.1.1 dist.c and seq_trace token delivery; preserve the original eight-scenario probe and plan. Existing third-party notices remain; no source banners, private license publication or public NuGet upload. Capture structural completion separately from native semantic/LP64/optimized-build attestation.

Part284 (2026-10-15): source-guided dist.c SEND_SENDER22/23; reuse existing C# ETF/admission/mailbox/helper primitives, no package/native production dependency. Preserve pinned source provenance and existing notices; public license remains undecided/private file excluded. Actual Werror cleanup correction changes ignored catch syntax only; no original outcome normalization.

Part285 (2026-10-15): adapt pinned dist.c/erl_proc_sig_queue incoming payload exit order using existing C# ETF/mailbox/epoch primitives plus BCL AsyncLocal/TCS. Actual source eligibility and deferred constructor work replace eager parsing; no external dependency or production BEAM. HOST constructor nested foreign-owner wait cycle is an explicit private boundary. Native resource accounting/outbound protocols remain incomplete; existing notices/private license policy unchanged.

Part286 (2026-10-15): standalone original payload11 collector reuses eight byte-exact existing BCL process-capture helpers and pinned OTP development compiler; production runtime unchanged, no new package or production BEAM. Canonical trapped trace contracts reuse public nonmutating info queries and native284 observations. No source license banners/private license publication.

Part287 (2026-10-15): deferred incoming monitor28 reuses actual285 ordered owner drain/epoch machinery; independent original monitor3 collector reuses exact BCL capture helpers and pinned OTP development compiler, no new package or production BEAM. Live watcher reason decode precedes monitor-reference lookup per pinned sources; physical/native results remain separate. No source license banners/private license publication.

Part288 (2026-10-15): lazy capacity observer reuses BCL TaskCompletionSource under the existing queue gate; no new dependency, queue budget or production BEAM. Native payload14 and CI287 evidence independently archived; original286 raw-hash refusal remains separate. Async all-writer admission/NIF proposals remain source-only. No source license banner/private license publication.

Part289 (2026-10-15): register existing managed input adapter and preserve target exception frames across apply dispatch; no added dependency/production BEAM/source license banner/private license publication. All-writer async pressure and real NIF ABI remain separate unfinished prerequisites.

Part290: standalone trusted fixture host uses existing runtime and BCL NativeMemory/NativeLibrary; no new dependency, generic NIF registration, license banner, public package release or LICENSE.local.txt publication. Pinned OTP headers and attribution remain consolidated.

Part294: standalone C# pressure observer reuses13 existing source files and BCL; no new NuGet dependency. Erlang original-VM test input and workflow Python build-provenance fragment are test-infrastructure exceptions, not production runtime implementations. No solution/public package/license-local publication.

## Part302 — original-only observation tooling reuse

Reuse the existing repository-owned process_capture.execute implementation (exact collector/function provenance retained), published SHA-bound original13 receipt helpers and metadata decoder baseline. The passive diagnostic decoder extension keeps an explicit diff/provenance, strict term/byte/depth budgets and raw fun spans; it never evaluates Erlang terms. Python standard-library orchestration, original Erlang fixtures and the passive reviewer are test/oracle infrastructure allowed by work.md section2.1, not a new production-language or package dependency. No external actor/ETF implementation, package or native ABI dependency is imported. Reviewed original source/license evidence remains in the pinned OTP checkout/register and the byte-preserved part302 source-proposal manifests; no file banners or LICENSE.local.txt are published. Bounded local35 decoder/report controls validate this reuse only; the new nine original observations and any SDK comparison remain unproved.

Part304: reuse the existing passive Python ETF decoder, receipt/process helper and byte-preserved original302 archive. Python remains test/oracle infrastructure under work.md2.1, not a production dependency. The schema-only function-stack correction is source-guided by pinned erlang.erl stacktrace(), validated by16 malformed/valid controls and independent same-raw9 review; no third-party parser/runtime code or new package is imported. Existing source/license records remain referenced, with no source banners or LICENSE.local.txt publication.

## Part357 runtime and native adapters

Reuse existing BCL/compiler projects; add no package. Pinned OTP contracts and CLR adaptation boundaries are recorded in [part357 source attribution](validation/part-357/source-attribution.md). Keep existing packaged notices byte-exact to the canonical local package inputs; no source license headers or LICENSE.local.txt are added.

## Part359 fixed Windows callbacks and original-driven monitor semantics

Reuse existing BCL/guest ownership/compiler projects without another package. [Source and adaptation record](validation/part-359/source-attribution.md) records pinned OTP contracts, LLP64 limits and selected native scope. Leave six canonical package resources exact, source headers absent and LICENSE.local.txt unpublished.

## Part360 text, binary and LP64 native adapters

Reuse existing BCL and guest ownership/compiler projects without a new dependency. [Source and adaptation record](validation/part-360/source-attribution.md) distinguishes managed Linux support, Windows owned native acquisition and pending original comparison. Preserve six canonical package resources and leave LICENSE.local.txt unpublished.

## Part364 — source-directed generated frames and private observers

Review candidate .NET facilities before this component: retain the existing C# compiler/interpreter and ProcessContext frame scopes, immutable term/function identity model, BCL Stack/Dictionary/List and TaskCompletionSource, and repository-owned bounded process/capture/ETF tools. No third-party actor/BEAM/ETF implementation is imported and no package is added. The development oracle is the unchanged OTP-29.1.1 source/BEAM build; Python and Erlang fixtures remain test infrastructure. Compiler lowering follows pinned v3_core/sys_core_fold/beam_ssa_type behavior only where a conservative proof exists; no fixture-specific stack rewriting is accepted. Actual local callback code fingerprints and initialization PIDs belong to their respective runtime and are observed independently. Existing source/license records and notices are preserved; no file banners or LICENSE.local.txt publication. Private implementation/source-directed reviews and exact observation evidence remain distinct from integration/full-contract acceptance.


Part365 — Integrate the existing source-directed proplists family and generic frame/continuation corrections. Reuse BCL collections, current term comparison/exact key semantics, process reductions, source compiler and established original-only GitHubCI tools. No new external dependency, production BEAM engine, public diagnostics or generated replacement is introduced. The prior unchanged-Erlang reuse gate and its unsupported moduledoc observation remain in the family planning document; this integration retains the reviewed C# adaptation. Preserve third-party notices and keep the local owner-license draft uncommitted.

## Part368 — Complete queue exports

Retain the part367 immutable {Rear,Front} representation. System.Collections.Generic.Queue and ImmutableQueue do not expose exact native tuple shapes, clause-specific improper-list behavior, end aliases or helper/callback frames. LINQ filtering/folding does not preserve OTP callback evaluation order, numeric-versus-exact equality or errors after a lazy branch. Port the pinned algorithms into cooperative C# operations, reusing current term/function/continuation infrastructure and native allocation-path reverse snapshots. Existing original-only GitHub CI tooling remains development test infrastructure; no new package or production BEAM dependency. Preserve all third-party notices and the uncommitted owner-license draft.

The independent original comparison also requires a conservative existing-compiler correction: pinned sys_core_fold.erl simplify_apply1010–1026 folds known apply/3 argument lists to ordinary calls. Reuse Expr.Call evaluation and its existing source/tail/loader handling for literal module/function plus a syntactically proper list; preserve SourceLine and left-to-right argument effects. Do not introduce fixture-aware stack filtering or a second dispatch engine. Runtime apply/2 wrong-arity errors use the existing actual source-frame capture and exact FunctionTerm/argument values, following pinned beam_common.c1667–1674; proper-list validation still precedes the arity check. These dependencies require prior-contract and full final-source regression checks.

Part370 — Reuse the current compiler tail-transfer loop, ProcessContext source frames, immutable terms and bounded oracle capture. Pinned beam_common.c apply/save-trace and emu/instrs.tab i_apply_last validate apply/3 before discarding its caller; error/1,2,3, exit/1 and throw/1 deliberately retain that source continuation. Implement a conservative resolved-tail transfer for fully valid dynamic apply targets, retaining current dispatch for invalid argument lists and exception BIFs. No framework substitution, third-party package or production BEAM engine. Preserve exact-vs-numeric FUN identity and existing explicit-stack comparisons.


## Part449 — incoming distribution/restart validation

Reuse BCL TcpListener/TcpClient, cryptographic RNG/MD5/fixed-time comparison for the documented distribution cookie protocol, bounded existing process/capture helpers, current ETF/immutable reference model and authenticated native driver package. Existing original Erlang/BEAM libraries remain unchanged. No external dependency or production handshake backend is imported: the new accepting socket/EPMD adapter is explicit evidence infrastructure exercising actual runtime queues. Read exact pinned dist_util/erl_epmd/net_kernel and native reference/DDLL routines; use assigned EPMD creation and actual original process restart. Preserve third-party notices and the uncommitted owner-license draft. Do not equate this scoped fresh incoming handshake with standard managed net_kernel boot or all status/race/TLS profiles.


## Part450 — pending collision, alive and owned fatal peer evidence

Reuse the existing BCL/ETF/native adapters and the repository-owned Halt437 Toolhelp process-entry/PInvoke layout for a test-only owned-family snapshot. Retain handles/start times before Process.Kill(entireProcessTree:true), then join every captured process; actual Python creator independently verifies the designated nonzero exit. This is not a Windows Job or future-child containment claim. No package, production Erlang engine or C# net_kernel replacement is added. Actual original net_kernel chooses both lexical pending branches and drives alive/wait_pending; the explicit host adapter validates statuses and uses existing production controller/queue primitives. Preserve original Erlang libraries, third-party notices and uncommitted owner-license draft.


## Part451 — original discovery and duplicate candidate evidence

Part451 scoped local acceptance: genuine check_pending discovery takeover in both lexical orders and already_pending silent refusal pass424/424 observations (376paired+48aux;368whole typed/raw+56own-reference protocols). Eight actual checker PIDs exit remarked and original connect waiters return true;24 actual kernel reply-trace groups verify ok_pending/up_pending/already_pending and recipients. Sixteen extra candidates close with zero status bytes, preserving full original up_pending rows/first pending owner and whole managed live snapshots; first false and valid true continuations retain real data/DDLL behavior.24 authenticated channels settle;8 designated fatal peer exits/8 captured family handles,16 normal oracle/replacement exits and2 private EPMDs settle. Final RuntimeA261E976/package1FE86D4E passes590/590 affected,retained36/native8,boot79/cleanup and one new concurrent adoption/queued-delivery group. No production algorithm/source-guard change:1621/default708/full118,API/full deltas0/0,catalog8827. Part450 mergeddf3cccb1; merge CI39960005038 green8826/packages. Original-only configured discovery barrier and explicit evidence adapter are not full managed net_kernel boot or default-DNS proof. Evidence:docs/validation/part-451/ddll-check-pending-and-already-pending. LICENSE.local.txt excluded.

Discovery is an explicit original-only kernel epmd_module callback barrier after normal boot; all unheld lookups delegate to erl_epmd. The actual kernel owns sys_dist/checker/acceptors; no direct state insertion or new public CheckPending enum. first alive candidate/old channel survive extra zero-byte EOF/reset and proceed false or valid true. Existing raw monitor/ref/frame/native cleanup is retained. The source/test/dependency/scheduler future plan remains unchanged and unimplemented by this slice.


## Part452 — late discovery, old timers and deferred adoption

Part452 scoped acceptance: 24/24 delayed-discovery/timer/owner scenarios across depths0/1/2/8 and both lexical orders;1272 observations=1128 paired+144 auxiliary,1104 whole typed/raw+168 own-reference protocols. Sixteen real queued late discovery success/error results and8 naturally fired old-checker timers after actual alive handoff preserve the new owner and actual data.72 authenticated channels,48 already_pending refusals,24 expected fatal old peers,48 normal oracle/replacement VMs,32 captured killed-family handles and5 normal private EPMDs settle. Accepted matrix is a five-batch composite; one readiness sharing failure is preserved and replaced by a complete fresh scenario. Runtime18AE9106/package6B4CE923 unchanged by final test guards. Affected591 passed before two stronger non-vacuity assertions; retain590 unchanged cases plus final strengthened case1/1. Native8/retained36 and boot79/cleanup pass. Exact452 full CI/packages tracked in PR, catalog8828. Default708/full118/source1621 unchanged, API/full deltas0/0. Parent451 PR12 remains open at green d4430b63,CI39963573865 passes8827/packages. Original-only configured discovery/scheduling and explicit managed queue adapter are not full managed net_kernel boot or default setup-time proof. Evidence:docs/validation/part-452/ddll-late-discovery-and-owner-races. LICENSE.local.txt excluded.

Actual original accept_pending precedes genuine setup_check success/error in the observed suspended-kernel mailbox; received messages retain exact checker/TimerRef/Res. The old send_after timer fires naturally after a real owner/controller/connection replacement; no synthetic kernel messages or direct sys_dist writes. Original-only instrumentation and explicit managed adapters remain separate. No production algorithm change was needed. Future source/test/dependency/scheduler architecture queue is unchanged.


## Part453 — original Unicode traps and named boot

Part453: Parts451/452 merged into main b161ea9b; exact merge CI39978960949 passes8828/8828 and both packages. Thirteen merged branches archived/verified and deleted locally/remotely; main and active ordsets worktree branch preserved. Original Unicode re ucompile/urun dispatch closes the observed named-kernel prerequisite:210 full observations,162 source-raw rows plus48 authenticated own-resource bijections. Four actual mid-conversion cancellation/owner-close profiles and substitute-binding refusals pass. Bootstrap filesystem recognition now retains exact current original-image canonical primitives. Original standard -sname boot with dist_listen=false/no_epmd passes144 full terms/stacks:138 strict source-raw rows and6 state-map-order-only rows; both whole-file strict byte mismatches remain explicit. Original repeat verifies all6427 provider files before/after. No TCP listener/EPMD/full net_kernel compatibility claim. Catalog8833 (+5 groups), default708/full118 unchanged, source guards1623 (+2); API/full deltas0/0. Final affected/native/boot gates and exact publication-head CI are recorded separately. LICENSE.local.txt excluded.

Read pinned erl_bif_re.c: Unicode compile traps every non-bitstring pattern to original ucompile/2; compiled-Unicode run traps every non-bitstring subject to urun/3. Keep conversion/error construction in original re.erl/unicode.erl; refuse missing or non-BEAM wrapper owners. Original erts_internal71 plus original erts_code_purger are early dependencies; reuse existing internal-port/atomics/ETS inspection providers. Do not equate source ownership with native body identity: filesystem admission checks current image, exact captured primitive and exported wrapper. Preserve initial registration behavior and refusal of unrelated stand-ins. Unchanged original inet_drv.c and registry driver remain candidates for subsequent real transport implementation; no handwritten pure net_kernel replacement. Existing architecture/scheduler queue is unchanged.


## Part454 — native raw messages and actual-sender order

Part454 scoped acceptance: genuine native raw port bang/send/nosuspend routes actual caller separately from envelope From; shared sender-dependency scheduler covers command/control/call/close/info/exit. Registered-name pre-admission retry resolves the original name again; info is captured inside its queue entry; one incremental drain fence replaces per-admission aggregate construction. Original65 raw protocol profiles plus15 actual busy_port-monitored alias races compare entire outcomes/stacks under explicit5/6 role bijections, with physical cleanup. Unchanged original registry driver and win32reg read-only6 compare whole typed/raw. Original BEAM tuple_size receive guards including f0/no-fail run16 values in one permanent group. Final runtime55D918E9; catalog8834 (+1), default708/full118 unchanged, guards1627 (+4), API/full deltas0/0. Final affected/native/boot receipts are separate. Full native TCP/UDP child ports, listener/EPMD/original net_kernel peer startup, admitted WAIT_BUSY after selector races and general binary-context receive/try remain open. LICENSE.local.txt excluded.

The private driver_create_port extension must adopt supplied driver data without Start, retain exact creator image/entry/DDLL ownership, reserve/adopt/rollback real parent port identities and reciprocal owner links, and fence child callbacks/stop until creator initialization returns. Public WinDyn slots remain unchanged. No fake internal-PID tag arithmetic, missing-service nominal success or handwritten pure net_kernel replacement is admitted.

## Part472 — reuse original network application libraries

Reuse unchanged pinned OTP29.1.1 inets9.8,ftp1.2.6,tftp1.3,eldap1.3.1 sources and official BEAM images through the existing .NET interpreter. No external managed network/LDAP rewrite is introduced. Compare actual instructions/imports first, declare dbg and compose original calendar/io_lib/io_lib_format/inet_parse dependencies explicitly. Preserve Apache notices in the original HTTP/FTP/TFTP inputs and the complete MIT notice in eldap.erl; retain generated ELDAPv3.hrl release identity and the original ASN.1 schema. Existing runtime/native boundaries remain unchanged; finite parser/codec/helper equality does not certify live services, transfers or authentication.

## Part473 — original Diameter/SNMP and support assets

Reuse unchanged pinned OTP29.1.1 Diameter2.7.2/SNMP5.20.5 source/image identities through the existing .NET interpreter, retaining original Apache notices and package licenses. No managed protocol rewrite or native-provider placeholder is introduced. Preserve all137 images, including eight Diameter implementation images outside its application descriptor, six Diameter dictionaries, MIB sources/compiled data and launcher sources. Hash-bind release sources and ten relocated SNMP compiler inputs against pinned Git, identifying generated outputs separately. Original inet/inet_parse and stdlib dependencies close portable address/type paths. Keep PCRE-backed original re and raw Counter64 ASN.1 BER obligations explicit:18 reference-only profiles are not managed acceptance. Prepared TLV checks do not certify raw BER. Support readers never run launchers or install priv paths.

## Part474 — original Megaco images

Reuse all original Megaco4.9.2 images and source inputs unchanged, executing selected original timer/configuration/termination-ID paths through the shared .NET interpreter. Compose original lists explicitly, preserve upstream source notices and license, and distinguish16 generated release files. No native scanner/BER substitute, handwritten protocol implementation, backend fallback or automatic application startup is introduced. Full gateway/controller services, transport and native providers remain separate obligations.

## Part475 — original Common Test/Dialyzer and real bootstrap dependencies

Keep all original pure algorithms unchanged. Include both property-extension images outside Common Test ebin, original source/support bytes and notices. Compose original formatter, preprocessor/scanner/features, Core/set helpers and a real minimal original init/file_server startup with explicit existing native ETS/persistent-term/bootstrap-filesystem providers. Do not mock init arguments, seed private feature state or normalize error stacks. Cold consumer disambiguates its authored module-list constants from imported types. Whole-app import audit still names debugger i/dbg_iserver; full runner/analyzer/PLT/distributed/GUI closure remains separate. Inventory43 doc/example images separately from primary application packaging.

## Part476 — original OS monitoring/ODBC libraries

Reuse unchanged pinned original os_mon and odbc rather than translating their pure parameters,df parser or callbacks into C#. Preserve original C sources as source data and both package licenses;explicitly exclude producer nteventlog/win32sysinfo executables. Reuse existing genuine io:printable_range native binding through the original io image;no hand-written pure fallback or application startup. No external .NET monitoring/database dependency is introduced for image packaging. Live collection,drivers,connection lifecycle and full applications need separate acceptance. Keep complete identity-bearing error arguments and anonymous tail-apply discrepancy visible;do not call a reduced direct-MFA corpus full module compatibility.

## Part477 — dynamic apply is native VM control transfer

Follow pinned OTP emu/ops.tab,instrs.tab and beam_common.c apply/fixed_apply/apply_bif_error_adjustment. Reuse current code-image,canonical no-return,async-context and owner-lease infrastructure. Do not add an actor library or translate pure OTP algorithms. Unwrap only actual canonical apply registrations;defer caller transfer until real argument validation,retain special no-return and source-load-NIF behavior,and preserve selected host bindings. Bind native function-apply error metadata to the original preloaded erlang source/image. Compare entire observations rather than stripping caller/comprehension frames.

## Part478 — remaining original GUI/tool libraries

Reuse unchanged pinned debugger, et, observer, reltool and wx source/images under their preserved upstream notices and existing OTP package license. Reuse the public library facade and explicit selection infrastructure. No new .NET GUI dependency or handwritten pure-module alternative is introduced. Original pixmap/header/source support stays data. Exclude producer wx DLLs and PDBs; they do not establish a portable native wx provider. Compare finite pure class/helper and process-dictionary behavior with whole original values and stacks. Live GUI, debugger, tracing and release-building application acceptance remains separate.

## Part479 — non-GC floating stores

Reuse pinned OTP emu/float_instrs.tab:fstore and the existing initialized-FP/destination/frame ownership checks. fstore performs PUT_DOUBLE and heap-pointer advancement using reserved space;it does not invoke GC. Keep actual GC root checks,arithmetic exception edges and handler guards unchanged. Execute original reltool_fgraph instead of translating force layout into C#. Preserve original header/notices;existing math native primitives and original lists/math wrappers remain explicit. No new external dependency or native GUI provider is introduced.

## Part480 closed receive-loop dependency policy

Do not translate original debugger loops into C#. Extend only the closed-wait lexical proof for already validated make_fun3,select_tuple_arity,update_record and put_tuple2. Existing typed validation remains authoritative;ordinary labels remain function-local,local tail targets are followed,unknown tails/returning arms/handlers still refuse,and a real wait remains required. Reuse existing public Debugger/Stdlib readers and cold consumer;no new external package or native GUI driver is introduced.

## Part481 original binary context and finite record proof

Reuse pinned beam_validator.erl match-context and typed-register semantics. Expand only transparent origin transport and conservative no-return CFG continuations;do not port original SSL/Parsetools algorithms. Nil/atom/literal writes remove their target origin and cannot authenticate contexts;duplicate active argument uses remain ordinary-only. Newly proven no-return swap helpers route through existing typed validation. Reject the broad swap-validation experiment that refused previous original modules. No new dependency/native TLS provider or compatibility promotion.

## Part482 total comparison BIF dependency policy

Reuse the existing BeamInfallibleBifContract and ErlangOperatorValues instead of importing a second .NET comparator or rewriting original crashdump code. Pinned beam_validator skips exception state for BIFs proven cannot_fail;beam_call_types delegates totality to erl_bifs:is_safe,which explicitly lists all eight comparison operators. Canonical fixed BIF execution reuses runtime-aware equality semantics;ordinary source/import/native binding rules remain distinct. No new package,provider or native GUI dependency is added. Frame source reads,destination bounds,real GC roots and handlers remain checked. Original bytes/notices and the uncommitted local license are preserved.


## Part484 — Cooperative native receive guards

Retain the original OTP29.1.1 megaco_trans_sender algorithm. Reuse existing ErlangListLength.ReadAsync and ErlangOperatorValues.Binary with ProcessContext; no new .NET actor/collection dependency. The synchronous match-spec walker cannot preserve cooperative guard execution. Pinned original i_length/length trap and receive save-pointer sources plus a real priority trace require preserving an already executing guard result across priority insertion. Keep the old callback protocol and scope host-unlink revalidation separately. Source audit: [Part484](validation/part-484/cooperative-receive-length/source-review.json). No new dependency/license selection; LICENSE.local.txt remains excluded.

## Part558 — compound comparison contracts for ordsets

Continue the existing C# alternative using Erlang.Terms and the repository ETF codec, with no added package. Inspect pinned OTP29.1.1 utils.c compound comparison, flat-map numeric/exact phases and HAMT minimum-key comparison before production changes. A fresh original matrix of unloaded local functions and maps across the32-entry threshold reproduces wrong C# map ordering; .NET canonical dictionary storage is not the arithmetic comparison view. Preserve exact map identity and hashing while correcting the comparison dependency, and retain separate runtime HAMT-layout acceptance. The independent term comparison tool references only the existing Terms/Serialization projects while shared runtime files are in an unrelated merge conflict. This does not establish post-merge runtime acceptance.

## Part557 — ordsets C# verification and native reverse source frames

Retain the existing handwritten C# alternative and pinned OTP29.1.1 original Erlang/BEAM alternative. Inspect `lib/stdlib/src/ordsets.erl` and `lib/stdlib/src/lists.erl` at the pinned tag; preserve Apache-2.0 notices and unchanged original assets. The installed .NET `SortedSet<T>`/`HashSet<T>` APIs do not reproduce the source merge's malformed-list error paths, numeric representative selection, helper source PCs, or callback protocol, so no replacement collection dependency is introduced. Reuse the existing typed source trampoline, native-binding identity check, source-frame scopes, actor ownership and ETF codec. The production change retains the native reverse caller PC; it does not translate new OTP algorithms or alter ordering/merge results. The new test fixture and Python subprocess driver use existing repository and standard-library facilities only. Whole-domain comparator/helper closure remains pending after finite exact oracle equality; see [ordsets compatibility](ORDSETS_COMPATIBILITY.md).

## Part520 — Complete math registration and backend choice

The current user explicitly requests a managed math alternative while preserving original Erlang selection. Reuse existing MathNative (System.Math and truncating remainder), Integer.TryToDouble, Erlang error bindings, and MathHostSpecialFunctions (system CRT/libm erf/erfc). No new NuGet dependency or approximation implementation is introduced. Candidate System.Math is the installed .NET10 standard library (dotnet/runtime, MIT); this part registers existing delegates, without copying third-party implementation text. Pinned OTP29.1.1 lib/stdlib/src/math.erl and erts/emulator/beam/erl_math.c (Apache-2.0) establish the26 declarations,24 native stubs, two literal constants, argument conversion order and nonfinite-result errors. Original assets/notices remain unchanged. Fresh official Windows OTP oracle output matches retained files byte for byte. Finite same-platform evidence does not prove all floating-point inputs or other CRT/libm implementations; no100% promotion is made.

Source consultation: .NET10 Math internal-call declarations and CoreCLR native wrappers were also inspected at https://github.com/dotnet/runtime/blob/v10.0.0/src/coreclr/System.Private.CoreLib/src/System/Math.CoreCLR.cs and https://github.com/dotnet/runtime/blob/v10.0.0/src/coreclr/classlibnative/float/floatdouble.cpp . They forward mathematical operations to the host functions and contain platform/compiler-specific rounding safeguards. These versioned upstream sources support the reuse decision; they do not certify the installed JIT/CRT or replace actual cross-platform oracle comparisons.

## Math compatibility — 2026-11-03

The expanded genuine OTP29.1.1 Windows corpus exposed that System.Math.Log2 and the pinned official OTP build are different algorithms. Follow erl_math.c's !HAVE_LOG2 expression (log(x) / 0.6931471805599453) on Windows and retain the direct log2 path on Unix. Reuse existing System.Math/CRT/libm primitives, Integer.TryToDouble, native error bindings and explicit library selection; no new production dependency or copied numerical algorithm is introduced. Original math.erl and math.beam remain unchanged with existing Apache-2.0 notices. The versioned dotnet/runtime host wrappers were consulted under MIT; glibc asin/underflow source was read for audit only and no LGPL code was copied. Physical VM layout, resource ceilings and timing remain separate from the26 mathematical MFA contracts. Source/provenance, old-runtime2785 RED and both new502772-case GREEN comparisons are recorded in validation/math-compatibility-20261103/. Cross-platform/final-publication receipts remain required before universal promotion.

## Part559 — ordsets shared comparison and physical map preservation

Inspect pinned OTP29.1.1 `erts/emulator/beam/utils.c` (compound comparison, flat-map key/value phases, HASHMAP_PHASE1/2 and address-seeded quicksort), `erl_map.c` (single-key put, flatmap merge, collision insertion and hash-key comparison), `erl_term_hashing.c` (local-fun internal hash), and x86 JIT `instr_map.cpp` (single dynamic put calls erts_maps_put). Keep the Apache-2.0 upstream notices and original Erlang/BEAM assets unchanged. System.Collections sorted/dictionary APIs are MIT-licensed platform facilities already in use; they supply storage, not OTP physical collision or equality semantics. No new dependency is suitable or introduced. Reuse the existing immutable native map graph, runtime-owned hash retention, physical-copy plan and iterative comparison scopes. Translate the two-phase HAMT comparison protocol into C# rather than invoking OTP as a fallback. Retain canonical dictionary order for storage/ETF; arithmetic ordering and physical layout have distinct purposes. Default registration/backend choice stays unchanged.

The adversarial oracle replays the same pool/0 BEAM in both runtimes and transports only scalar expectations. Transporting pathological maps through deterministic ETF can be invalid in original OTP and would erase the physical construction contract. A separate repeated original diagnostic demonstrates address-seeded bulk ordering of distinct local fun keys whose comparison is zero. The permanent pool preserves these keys and tests deterministic incremental HAMT construction rather than asserting one address-dependent permutation. Capture and repeat verify all6427 admitted provider files before and after execution. This evidence does not close every dependent lists helper or establish cross-platform/final-head acceptance.


## Part560 — string source-stack parity and explicit dual backend

Pinned OTP29.1.1 string.erl and lists.erl are the owning algorithm/error-location sources; the unchanged string_SUITE.erl is retained under its Apache-2.0 notice and exact upstream blob/hash. The user's explicit string-port request authorizes the manual C# alternative while preserving original Erlang/BEAM. No policy change for other pure modules is inferred. BCL String APIs and Rune classification are not substitutes for OTP chardata, graphemes, case mapping or normalization; reuse the pinned unicode/unicode_util images on .NET. Reuse the existing source-call execution frames, tail-call scopes, native lists traversal and bit-syntax UTF-8 encoder/error-info capture. Collections, BigInteger, Rune UTF-8 decoding and immutable binary slices are existing platform facilities; no production dependency is added. The oracle is development-only and production never falls back to original string or an external VM.

Compiled binary patterns are actual native resources. Compare their complete stack-containing terms only after live binary:match queries establish the admitted one-to-one resource binding; retain raw stacks and reject modified frames/bindings/queries. Expose both backends through the existing startup/manifest/MSBuild mechanism, require explicit Unicode/list dependencies and record actual identities. Retain the default native-only registration profile and distinguish it from the71 selectable exports. Whole ERTS resource/timing and unmeasured platform equivalence are outside this finite acceptance.

The complete regression also exposed imported logical flat maps with atom keys retaining a different runtime's atom-index order. Reuse canonical name-keyed pairs for exact equality only when both maps are ordinary logical flat maps with exclusively atom keys; names identify atoms across these runtime tables. Preserve native physical comparison and all other key shapes. The existing imported-copy regression and all twelve independently captured term-contract shards validate this narrow correction; a rejected broad logical-map shortcut is retained as failed evidence.

## Part561 — Windows thread CPU observation under resource pressure

Inspect the existing Process.Threads-based provider and Microsoft documentation for [OpenThread](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openthread) and [GetThreadTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getthreadtimes). Query the exact Windows thread with THREAD_QUERY_LIMITED_INFORMATION and retain its real user time in100ns ticks through an owned SafeFileHandle. This removes the observed failing whole-process diagnostics enumeration without replacing measured CPU time with wall time or swallowing OutOfMemory. No third-party code/package is copied or introduced; use the existing OS/BCL facilities. Keep the Unix provider and nested ownership accounting. The preceding fatal Roslyn stack overflow is not assumed to have the same cause; its exact interrupted fixture first passes unchanged in a fresh process. Preserve that distinction in evidence.

## Part562 — All71 string source contracts

Review all119 public clauses of pinned `lib/stdlib/src/string.erl`, its reached
helpers and native `erts/emulator/beam/bif.c` float parser plus Windows
`erts/emulator/sys/win32/sys_float.c`. Preserve the existing Apache-2.0 source,
notices and original images. Continue the user-requested C# alternative beside
original Erlang; no broader pure-module port policy change is inferred.
Existing BCL string/search/set APIs cannot reproduce the observed lazy malformed
chardata behavior, native compiled-pattern identity, list-membership failures or
source helper frames. Reuse actual selected `binary:compile_pattern`,
`binary:match`, `lists:member`, pinned Unicode modules, the existing ETF function
factory and source continuations. Keep Rune only for bounded UTF8 scalar decode
and BigInteger for exact integer arithmetic. No package or alternate Unicode,
normalization, number-parser or collection dependency is introduced.

The real 4096-digit timeout justifies removing repeated prefix traversals rather
than increasing a worker budget. Source-level dispatch must retain conversion
error tuples, original separator-list shape and `cp_prefix` continuation equality.
The independent oracle generators and per-API summary use existing Erlang,
PowerShell and Python standard libraries only. Finite same-platform comparisons
do not certify all IEEE conversions, other operating systems or physical ERTS
resource ceilings. See [Part562](validation/part-562/README.md).

## Unicode C# alternative — 2026-11-04

Audit pinned OTP-29.1.1 lib/stdlib/src/unicode.erl and the existing UnicodeNative/UnicodeNativeTraversal BIFs. System.Text.Encoding and String.Normalize use platform Unicode data and replacement/error contracts that differ from OTP; do not use them as library implementations. Reuse Rune solely for strict bounded scalar decoding/encoding and BinaryPrimitives for endian words (existing .NET BCL, MIT). Port ml_map and cbv behavior directly from the Apache-2.0 OTP source, preserving notices. Reuse the explicitly selected pinned unicode_util module for normalization and classification, matching the established string dependency boundary. No new package or Unicode database. Retain original Erlang/BEAM without mutation and require explicit dependencies for the new managed selection. This user-requested module port does not change the policy for other pure OTP modules.


## Timer managed alternative — 2026-11-04

Inspect OTP-29.1.1 timer.erl (Apache-2.0), its complete public/server export surface and existing runtime timers, ETS, spawn/monitor and selected gen_server/supervisor facilities before implementation. System.Threading.Timer (BCL, MIT) alone cannot preserve Erlang cancellation references, selective receive, process ownership, signal ordering or supervised restart. Reuse the existing Erlang runtime primitives rather than introducing a timer/actor package. Translate library dispatch, server callbacks and interval workers directly from the pinned source, retaining original source/BEAM unchanged and preserving upstream notices. The explicit user request authorizes this module's C# alternative; it does not change the repository policy for other modules. Existing timer primitives remain shared native capabilities. Finite timing observations will not be described as universal scheduling/resource equivalence.

## gen_event managed alternative — 2026-11-04

Inspect pinned OTP-29.1.1 gen_event.erl, gen.erl, sys.erl and gen_event_SUITE.erl before implementation. Akka.NET/Orleans and CLR events do not implement the OTP wire protocol, catch results, supervised-handler links, alias requests or system state callbacks. Reuse existing runtime primitives and explicitly selected OTP gen/proc_lib/sys/logger/io_lib dependencies; port the complete gen_event dispatch, handler transitions, system and formatting exports in C#. No new package. Retain Apache-2.0 attribution and original Erlang/BEAM unchanged. The current user explicitly authorizes this managed module alternative; the general pure-module policy is unchanged. Finite checks cannot establish universal OTP equivalence.

## File managed alternative — 2026-11-04

Inspect all exports/clauses of pinned OTP-29.1.1 lib/kernel/src/file.erl, file.hrl and file_int.hrl (Apache-2.0, Ericsson AB). The user explicitly authorizes this module's C# port with preserved original selection. BCL File/Directory/FileStream (MIT runtime) cannot replace the distributed file server, process I/O, module-dispatched descriptors, raw/RAM drivers or Erlang parser/evaluator semantics. Reuse the selected OTP dependencies and existing .NET runtime primitives; translate file library algorithms/protocol dispatch into Erlang.Otp.Kernel C#. Use BCL BigInteger for unbounded lengths and BinaryPrimitives for the unsigned big-endian indirect header. No new package, backend fallback or original-source mutation. Both alternatives require the same supporting OTP modules and native capabilities. Finite focused checks do not establish universal OS/resource/full-stack equivalence.

## ETS managed alternative — 2026-11-04

Inspect pinned OTP-29.1.1 lib/stdlib/src/ets.erl (Apache-2.0, Ericsson AB 1996-2026) and the existing C# ETS native providers. ConcurrentDictionary/DataTable and .NET persistence APIs do not preserve ordered_set numeric key equality, bag semantics, process ownership, match specs, scheduler census or the disk_log format. Reuse the existing native runtime storage, continuation, fixation, transfer and match-spec implementations, retaining unchanged Erlang source/BEAM. Port the ETS library control flow to C#, calling selected disk_log, dets, qlc, erl_eval, ms_transform and I/O modules at the same dependency boundary as OTP. Use existing BCL collections only for temporary lists; no new package. The explicit user instruction authorizes this module-specific port. Finite comparisons are not a universal compatibility certificate.

## code managed alternative — 2026-11-04

Inspect pinned OTP-29.1.1 code.erl, code_server.erl, eep48.hrl and existing BEAM loader, reference-resource, filesystem and interpreter-native bindings. System.Reflection/AssemblyLoadContext and BCL filesystem enumeration do not reproduce code_server path/cache/sticky state, prepared-code references, Erlang on_load handling or purge. Retain the explicitly selected original code_server and OTP helper modules as dependencies and port code's public interface, preparation workers, metadata/docs logic and status processing into C#. Reuse the existing OTP process/monitor and native loader APIs; introduce no package. Preserve Apache-2.0 notices and original Erlang/BEAM unchanged. Native coverage/debug retain the runtime's existing non-BEAMASM capability profile; a Windows JIT reference does not certify those features. This user explicitly authorizes this module's C# port alongside Erlang.

## DETS managed alternative — 2026-11-04

Inspect pinned OTP-29.1.1 dets.erl, dets_server.erl, dets_sup.erl,
dets_utils.erl, dets_v9.erl and dets.hrl (Apache-2.0, Ericsson AB).
The current user explicitly authorizes this module's C# port and preserving
original Erlang, with implementation before testing and focused-only checks.
BCL FileStream/BinaryPrimitives/MD5 provide byte I/O, endian words and the
format's required checksum (existing .NET BCL, MIT); they do not implement DETS
linear hashing, repair, owner lifetimes, continuations or bchunk. SQLite,
LiteDB and embedded key/value stores use different file formats and protocols
and are not suitable substitutes. No third-party storage package is introduced.
Reuse existing immutable terms, exact equality/order, arbitrary-precision Integer,
ETF encode/decode factories, canonical OTP hashing and match-spec machinery.
Retain upstream notices and all original source/BEAM files unchanged. The C#
file/object/option code is an internal, unregistered draft; no completed library,
backend selection or compatibility acceptance is claimed. See
[implementation checkpoint](validation/dets-port-20261104/README.md).

ETS terminal dependency — the connected pinned ets.erl re_search path uses re:run/3 with exactly [{capture,none}]. Reuse the already admitted native PCRE2 engine, its Unicode trap, cancellation/resumption and finally-based match-data cleanup. Add only the requested no-capture return mode (match/nomatch); no .NET regex substitution, new native dependency or general regex-option acceptance is introduced. Original re.erl remains unchanged.

## DETS connected raw file and buffer prerequisites — 2026-11-04

The first real kernel-backed DETS observer fails in unchanged file/prim_file before reaching DETS: del_file_nif is absent from the existing four-operation builtin image. Inspect pinned prim_file.erl, prim_buffer.erl, prim_file_nif.c and win_prim_file.c. Extend the existing image-reservation/declared-NIF loader and runtime-owned weak reference resources for the raw seek/read/write/pread/pwrite/truncate/sync/indirect-read operations and buffer primitives needed by the original DETS backend. Do not replace file_server with a test stub or weaken module-owner admission. Reuse BCL FileStream/SafeFileHandle and existing Windows path/error marshalling; retain Apache-2.0 source notices. These are shared runtime primitives for both backends, not an Erlang library backend fallback. Keep file/buffer lifecycle and focused error/liveness tests in this DETS acceptance scope. No third-party package.

## Part578 — timer semantic constants and scoped publication

Reuse existing runtime module/BIF/operator/error/boolean catalogs. Give timer API, request, reference, action/result, child specification, time conversion, closure and source-location values distinct semantic owners; equal literals alone are not a reuse criterion. Validator bootstrap composes existing public ETS primitives and does not require the concurrent ETS module port. No new library/package/license change. Preserve pinned OTP29.1.1 Apache-2.0 assets and reference suites.

### ETS Part569 final dependency evidence

The final ETS acceptance reuses the concurrently supplied native prim_file/prim_buffer writable resource closure and unchanged original disk_log/file/DETS helpers;75 real filesystem/DETS/corruption scenarios pass independently for each backend. Both exported dumps are read by official OTP. Preserve the initial missing del_file_nif failures as historical evidence. The library-only init actor handles original get_argument requests with no command-line flags, rather than replacing DETS startup or disk persistence. No new third-party package or private persistence format is introduced. Frozen filtered-build dependencies and the two concurrently changed shared selection sources are disclosed in docs/validation/ets-port-20261104/README.md; this is scoped evidence, not independent certification of every native filesystem contract.

## DETS RAM-file driver dependency — 2026-11-04

The expanded original-DETS observer reaches ram_file:open and exposes the absent builtin ram_file_drv: the generic spawn path attempts an OS process and returns eacces. Inspect pinned lib/kernel/src/ram_file.erl and erts/emulator/drivers/common/ram_file_drv.c (Apache-2.0, Ericsson AB). Implement the actual binary driver command/response protocol on BCL MemoryStream, preserving the unchanged Erlang wrapper. Admit the builtin name only for the explicit spawn command in the registered bootstrap-filesystem profile. Reuse RuntimePorts identities, ownership, links, monitors, exit signals, statistics, ordered commands and cleanup; do not introduce an independent port registry or replace the ram_file module. The driver has no external OS process and introduces no package. Compression opcodes are commented out in the pinned driver; unknown commands preserve its no-response behavior. This is connected DETS runtime scope, not certification of all generic port options.

## DETS completion and magic-reference lifetime — 2026-11-04

Retain original dets_server/dets_sup as selected Erlang support modules, like the original code_server dependency of the code alternative. The C# library owns the complete dets dispatcher/table worker/v9 algorithms; it never delegates storage to original dets/dets_v9/dets_utils. Reuse canonical phash2/phash and existing match-spec execution; no new package or private storage format. Preserve original assets and Apache-2.0 notices.

Unchanged dets_SUITE:repair_continuation exposes retained compiled-match resources after guest garbage_collect. Inspect register liveness, function scope retention and the existing weak resource registry. Clear dead caller-save registers and completed argument/result temporaries. Keep actual guest-heap collection first and add a CLR interop sweep only when the registry contains legacy compiled-match/file/buffer payloads. Do not reject all decoded references: retained local and remote programs must remain valid. The permanent four-mode oracle test plus connected literal-root/compiled-ETS/internal-GC checks cover this distinction. No actor framework or alternative GC package is introduced.

## sys alternative — 2026-11-05

The explicit user request authorizes a manual C# alternative to pinned OTP-29.1.1 sys. Inspect the complete upstream sys.erl and sys_SUITE plus the existing SysTermination, module-selection, generated-frame, gen:call/reply and process receive facilities. Reuse immutable runtime terms, selective receive, existing gen/proc_lib, file and formatting modules on .NET; do not introduce an actor framework, another transport or third-party package. No candidate package supplies this runtime's terms/process/stack contract; the existing runtime is the suitable integration. Retain original source/BEAM and Ericsson Apache-2.0 attribution. Implement all explicit exports before running focused sys and connected behavior checks; finite checks do not establish universal compatibility.

## application alternative — 2026-11-05

The explicit user request authorizes a complete manual C# alternative to OTP-29.1.1 application. Inspect pinned lib/kernel/src/application.erl (including serial/concurrent dependency traversal, rollback and behaviour_info), application_controller/master/dist_ac boundaries and the existing explicit startup selection and source-frame machinery. Existing .NET actor libraries do not supply Erlang terms, request identifiers, group leaders or these controller protocols; reuse this runtime and the original controller/master/distribution/gen_server modules on .NET. No external package or ERTS dependency in production. Preserve original Erlang/BEAM bytes and Ericsson Apache-2.0 notices. Implement the entire module before focused application/connected checks. Universal compatibility requires more than registered exports or finite observations.
## supervisor alternative — 2026-11-05

The user's current request explicitly authorizes the complete manual C# supervisor alternative beside unchanged OTP-29.1.1 Erlang/BEAM. Inspect pinned supervisor.erl, supervisor_SUITE and existing Supervisor/SimpleSupervisor, GenServer, selective receive, gen/proc_lib/sys and runtime library selection. Reuse the repository's immutable terms and process/BIF implementation; actor frameworks (including Akka.NET, Apache-2.0) do not provide Erlang links, monitors, exact child IDs, source stacks or sys state contracts and are not adopted. No package dependency is added. Preserve Ericsson Apache-2.0 attribution. Complete the implementation before executing focused validation; no broad regression or canonical-readiness refresh is authorized by this task.

## io complete library alternative — 2026-11-05

Inspect pinned OTP-29.1.1 io.erl, the runtime I/O protocol, explicit startup selection and existing managed ports. The user explicitly requests the C# alternative, overriding the general pure-module policy for io. Reuse immutable terms, selective receive, monitor/demonitor, traced sends and the shared printable_range primitive. Keep io_lib, erl_scan, erl_parse, unicode and net_kernel as explicit ordinary library dependencies. BCL TextWriter alone cannot implement Erlang request/reply or parsing semantics; no external .NET I/O package is adopted. Preserve original Erlang/BEAM and Apache-2.0 notices. No package/license change or performance claim.

## erts_internal selectable alternative — 2026-11-05

The explicit user instruction authorizes the C# library alternative to OTP-29.1.1 erts_internal, retaining original Erlang/BEAM. Inspect the pinned upstream module and the existing native stub capture, selective mailbox receive, source frames, startup selection and statistics trap admission. Reuse the actual runtime primitive delegates for the 71 native signatures, and port the 27 Erlang library signatures. BCL tasks/channels alone and external actor libraries cannot supply Erlang selective receive, process signals, ETS privileges or exact term/stack semantics; no package is adopted. Preserve Ericsson Apache-2.0 notices. The authenticated original image is used for native signature/delegate capture, not as an implementation of the selected C# library bodies. Complete implementation before focused tests; no full regression or global readiness promotion.

## erpc alternative — 2026-11-05

The explicit user request authorizes a complete manual C# alternative to pinned OTP-29.1.1 erpc. Inspect lib/kernel/src/erpc.erl and the existing partial Erpc/ErpcTimeout/ErpcStack implementation, spawn_request, abandonment, monitors and immutable request maps. Existing .NET RPC/actor frameworks cannot preserve these runtime terms, monitor ordering, exception classes and selective receive contracts; reuse this runtime and BCL only. No external dependency or production ERTS. Retain original Erlang/BEAM and Ericsson Apache-2.0 notices. Implement all explicit exports before focused erpc/connected validation. Finite observations cannot certify universal compatibility.

## error_logger alternative — 2026-11-05

The explicit request authorizes a complete C# alternative to pinned OTP29.1.1 error_logger. Inspect lib/kernel/src/error_logger.erl, logger_internal.hrl, the existing WarningMapNative, immutable terms and execution frames. System.Diagnostics/ILogger do not implement Erlang event-manager, callback, metadata or supervision contracts; reuse this runtime and original logger/gen_event/supervisor/application/io_lib dependencies on .NET. No external package. Preserve original Erlang/BEAM, Ericsson notices and Apache-2.0 attribution. Implement every explicit export before focused comparisons; API coverage alone cannot establish universal compatibility.


## gen_server alternative — 2026-11-05

The explicit user request authorizes a complete C# alternative to pinned OTP-29.1.1 gen_server alongside unchanged original Erlang/BEAM. Inspect the complete pinned source, gen_server_SUITE, existing host GenServer/ErlangGenServer, sibling managed state-machine engines, immutable terms, selective receive, alias/monitor/timer BIFs and gen/proc_lib/sys contracts. The host facade omits the full Erlang module contract and is retained separately. Reuse this runtime and existing original gen/proc_lib/sys/logger/formatting dependencies on .NET. BCL Channels and actor libraries cannot directly supply Erlang callback return, selective receive, link, monitor or source-frame semantics; no external package is adopted. Preserve Ericsson Apache-2.0 notices. Implement every export and the engine before focused validation. Finite acceptance is separate from universal compatibility; do not run the full regression or regenerate global readiness from a partial report.

## logger alternative — 2026-11-05

The explicit module-port request authorizes a selectable manual C# alternative to OTP-29.1.1 logger. Inspect pinned logger.erl/logger_internal.hrl and the existing partial logger host implementation, module selection, immutable terms, process dictionary and source-frame facilities. Existing Microsoft.Extensions.Logging and Serilog use different levels, event/configuration models and callback/failure contracts; neither substitutes for the OTP API. Reuse this runtime and the unchanged Erlang logger_server/logger_config/logger_backend/logger_proxy, handlers, formatter, filters, application and io dependencies on .NET, as file/code ports reuse their OTP servers. C# owns logger API control flow, message/metadata assembly, reports, pretty printing and environment initialization; it never calls original logger. No new package or external ERTS production dependency. Preserve Ericsson Apache-2.0 notices. Implement all 77 explicit exports before running focused checks; retain the default host subset and original Erlang assets. Dependency modules are not claimed as C# ports. Finite observations are not proof of universal compatibility.

## rpc alternative — 2026-11-05

The explicit rpc port request authorizes the C# library alternative beside unchanged OTP-29.1.1 rpc.erl/BEAM. Inspect the pinned complete rpc source, its rex server, observer, C-node stdout and erpc dependency boundaries. Reuse the runtime's immutable terms, selective receive, links/monitors, group leaders, stored-function recipes and explicit module selection. Existing BCL tasks or actor packages do not supply Erlang mailbox/monitor/stack semantics; no new package is adopted. Keep gen_server, erpc, maps/lists, ETS, persistent_term, net_kernel and unicode as ordinary independently selected dependencies, like upstream rpc itself. Preserve Ericsson Apache-2.0 attribution. Implement all 35 exports before focused comparisons; API counts are not a universal compatibility claim.

error_logger focused closure follow-up: reuse RuntimeFunctionEntries.External for logger_filters and register empty-environment IRuntimeStoredFunctionRecipe callbacks for report formatting/TTY stop. Raw host delegates cannot be stored in guest ETS. The C# callback uses its real assembly identity, never a borrowed upstream BEAM hash; ETF restoration rebinds the same managed code. Keep original calendar/raw_file_io/file_io_server dependencies and original Logger/event-manager/application services. Add explicit caller source frames for non-tail delegation, including nested invalid format depth. No new package or replacement logging/file service.


## Part591 — sys focused acceptance and shared tail-fun admission

Retain actual gen/proc_lib/list, IO/formatting/file helpers and native file/buffer resources. The five unchanged sys_SUITE testcase bodies use original gen_server and original sys_sp1/sys_sp2; separate observations exercise trace/print effects and real file bytes. Start the actual file_server callback through gen_server's non-master service setup. Supply original net_kernel/raw_file_io/epp/scanner/feature helpers rather than replacing their behavior; restore authenticated erl_features state through native persistent_term. No new package, transport, storage format or production ERTS dependency.

The original sys:print_event/2 witness proves that the existing shared failed-fun capture duplicated a tail-call frame. Detect the existing syntactic deallocate/return tail protocol before fun admission fails, preserving continuation duplication only for non-tail calls. Authentic BEAM tail/non-tail badfun/badarity controls pass against the pinned VM on both sys backends and six error-depth profiles. This is a connected interpreter correction, not a blanket stack trim or relaxation of expected results. Runtime source identity and native prerequisites remain separate from universal semantics. Preserve Apache-2.0 sys/reference notices and all failed discovery receipts. [Final evidence](validation/part-591/README.md).

## Multi-scheduling blocker closure — 2026-11-05

Inspect OTP29.1.1 bif.c, erl_bif_info.c and erl_process.c scheduler suspension, blocker lists and process-exit cleanup. Reuse the existing actual normal actor barrier/dispatcher and dirty CPU/shared-IO queue owners. BCL ReaderWriterLockSlim/SemaphoreSlim alone cannot model Erlang scheduler binding, dirty work quiescence, nested per-process blockers and owner death; no external package or actor framework is adopted. SemaphoreSlim serializes changes only; it does not stand in for physical scheduler suspension. Preserve the original ERTS library and Apache-2.0 notices. Implement first, then compare only multi-scheduling, erts_internal and connected scheduler/lifetime contracts. Finite acceptance is separate from universal OTP compatibility.


## Part593 — native node/dist reference census — 2026-11-05

Inspect pinned erl_bif_info.c tuple admission and helper trap, erl_node_tables.c root walk/result builder, erl_process.c signed all-versus-normal block state, and node_container_SUITE/erts_test_utils reference invariants. Reuse the actual normal actor barrier, physical MSO external-node leases, ETS stored identity/read leases, persistent arena identifier roots, mailbox recipient operation custody, timer payloads, distribution controller/link/monitor owners and existing match-spec ASTs. BCL GC reflection/heap enumeration and a third-party actor framework cannot supply guest ownership; no package is adopted. The diagnostic builds its counts from real retained roots and leaves host DTO lifetime distinct from guest process custody. Compressed wire bodies are separate from external key headers. Preserve native/reference Apache-2.0 notices and the original Erlang alternative. [Focused source/evidence](validation/part-593/README.md); no whole-module100% certification or full regression.
