# Compatibility overview

Part 1522 matches OTP on 83 fixture observations and on the complete fixture image. OTP loads both images in all 979 differing pairs. [Evidence](validation/part-1522/README.md).

Part 1495 checks fields retained across an inner call and a tuple argument built from the retained value. The pinned OTP VM and both C# builds agree on the tested returns. [Evidence](validation/part-1495/probe.txt).

Part 1494 checks that a folded identity case returns the original selector and that an outer field survives the inner selector call. The pinned OTP VM and both C# builds agree on the tested values. [Evidence](validation/part-1494/probe.txt).

Part 1493 checks a nested call selector that uses an outer tuple field and a saved input. Both C# builds agree with the pinned OTP VM on the inner return values and outer mismatch. [Evidence](validation/part-1493/probe.txt).

Part 1492 checks a nested call in a tagged case arm. The pinned OTP VM and both C# builds agree on conversion, forwarding the saved argument, unmatched tuples and `badarg`. [Evidence](validation/part-1492/probe.txt).

Part 1491 checks a saved-selector case followed by later expressions. The pinned OTP VM and both C# builds agree on the tested values, guard fallback and original selector in `case_clause`. [Evidence](validation/part-1491/probe.txt).

Part 1490 checks register-resident fields in saved tuple case arms, including a bound integer key, an exact mismatch with a float, a local call and retention of the original tuple. The pinned OTP VM agrees with both C# builds on the tested values. [Evidence](validation/part-1490/probe.txt).

Part 1489 checks saved-selector dispatch after a call, guard failure followed by a repeated tag, and the original tuple in `case_clause`. The pinned OTP VM agrees with both C# builds on the tested values and error. [Evidence](validation/part-1489/probe.txt).

Part 1488 checks tuple arity and tag dispatch with secondary patterns, repeated keys, and guard fallback. The pinned OTP VM agrees with both C# builds on the tested values. [Evidence](validation/part-1488/probe.txt).

Part 1487 checks selection by tuple tag, branch-specific fields, a wrong tuple arity, and a failed guard followed by a repeated tag. The pinned OTP VM agrees with both C# builds on the tested values. [Evidence](validation/part-1487/probe.txt).

Part 1486 checks shared tuple field selection, exact integer versus float matching, and a failed guard followed by another clause with the same key. The pinned OTP VM agrees with both C# builds on the tested values. [Evidence](validation/part-1486/probe.txt).

Part 1485 checks exact literal selection, guarded fallback, and bitstring constants. The pinned OTP VM agrees with both C# builds on the tested fixture and on fifteen `ssh_bits:mpint/1` values and errors. [Evidence](validation/part-1485/README.md).

Part 1484 checks both tuple alias orientations, guarded fallback, and exact matching of integer literals against floats. The pinned OTP VM agrees with both C# builds on the tested values. [Evidence](validation/part-1484/probe.txt).

Part 1483 compares tuple-pattern dispatch with a nested call and a fallback that returns the original input. The pinned OTP VM agrees with both C# builds on successful matches, wrong arity, and wrong tag. [Evidence](validation/part-1483/probe.txt).

Part 1482 checks clause selection through combined type predicates, exact equality, and fallback. The pinned OTP VM agrees with both C# builds on the tested results, including integer versus float exact comparison. [Evidence](validation/part-1482/probe.txt).

Part 1481 compares measured tuples and binary split results with the pinned OTP VM. The tested successful results, bypass branch, and `badarg` agree. Five focused .NET checks pass. [Evidence](validation/part-1481/README.md).

Part 1480 checks measured call results, byte_size badarg and failed arithmetic guards. The pinned OTP VM agrees with C# on the tested values and failures. Five focused tests pass. [Evidence](validation/part-1480/README.md).

Part 1479 checks integer, float and atom operands in complementary size guards and function_clause when neither comparison succeeds. The pinned OTP VM agrees with both C# images on the tested cases. Five focused tests pass. [Evidence](validation/part-1479/README.md).

Part 1478 checks Unicode atom conversion, an overridden unqualified conversion, and success and case_clause in a one-arm result match. The erl_tracer image matches the Windows OTP 29.1.1 release byte for byte. Five focused tests pass. [Evidence](validation/part-1478/README.md).

Part 1477 compares normal returns, tail errors, unknown tags and wrong tuple arities in a terminal tagged case. Both compilers preserve the original tuple in case_clause. Five focused tests pass. [Evidence](validation/part-1477/README.md).

Part 1476 checks tuple fields used in a constructed error argument and multiple call arguments that read an enclosing parameter and a tuple alias. The pinned OTP VM agrees with C# on the tested values and errors. Five focused tests pass. [Evidence](validation/part-1476/README.md).

Part 1475 checks nested selectors on the register case path, including an argument that survives both calls and a terminal branch error. The pinned OTP VM agrees on the tested results, and all five focused .NET tests pass. [Evidence](validation/part-1475/README.md).

Part 1474 covers a guarded unary static command followed by a wx object overload. The wxDisplay image matches the Windows OTP 29.1.1 release byte for byte in an independent source compile and the permanent regression check. Five focused tests pass. [Evidence](validation/part-1474/README.md).

Part 1473 checks guard acceptance and failure, preservation of the selector through a call and case_clause. The reference VM agrees on the tested behavior for the three fixture images. All five focused .NET tests pass. [Evidence](validation/part-1473/README.md).

Part 1472 tests direct and bound case selectors, a guard that keeps the original binding, and case_clause. All five focused tests pass. OTP accepts both images of each differing release pair. [Evidence](validation/part-1472/README.md).

Part 1471 checks GC and ordinary BIFs with X and Y inputs, return values and badarg behavior. OTP accepts both images of all 983 differing release pairs. [Evidence](validation/part-1471/README.md).

Part 1470 checks copied, computed and tuple list heads against the corrected entry path. OTP accepts both images of all 983 differing release pairs. [Evidence](validation/part-1470/README.md).

Part 1469 checks nested entry matching, guard failure, repeated bindings and original arguments in function_clause. Three focused cases pass and OTP accepts both images of every differing release pair. [Evidence](validation/part-1469/README.md).

Part 1468 checks proper-list and bit-operand inference, improper-tail arguments and native fallback boundaries. Three focused cases pass. OTP accepts both images in each differing release pair. [Evidence](validation/part-1468/README.md).

Part 1467 checks frameless arithmetic, typed BIF operands, improper-tail arguments and the original arithmetic error. Three focused tests pass. OTP accepts both images in each differing pair. [Evidence](validation/part-1467/README.md).

Part 1466 checks tuple dispatch, exact integer keys, closed closure fallback and function_clause argument preservation. Three focused tests pass. OTP accepts both images in each differing pair. [Evidence](validation/part-1466/README.md).

Part 1465 checks incoming initialization and preserves projection copies if any continuation reads the slot. Three focused tests pass. OTP accepts both images in each differing pair. [Evidence](validation/part-1465/README.md).

Part 1464 proves stack slot initialization and liveness before retargeting a projection across direct calls. All three focused tests pass. OTP accepts both images in each differing pair. [Evidence](validation/part-1464/README.md).

Part 1463 preserves argument copies with live source registers and keeps Y-slot writes through calls. Three focused tests pass. OTP accepts both images of all 983 differing pairs. [Evidence](validation/part-1463/README.md).

Part 1462 checks shared tuple projections, constant fallbacks, duplicate tags and function_clause arguments. Both loaders accept all differing release images. [Evidence](validation/part-1462/README.md).

Part 1461 verifies that source no_inline suppresses automatic inlining regardless of option order. The release-profile experiment over-inlined compiler modules and was withdrawn. Both loaders accepted all differing images from the current build. [Evidence](validation/part-1461/README.md).

Part 1460 checks single- and multi-clause fold callbacks, captured records and callback errors. Both HTTP images agree on the tested results and module stack frames. Both loaders accept all 983 differing images. [Evidence](validation/part-1460/README.md).

Part 1459 verifies record dispatch, sentinel handling, function_clause and the top badrecord location. The HTTP callback frame still differs. Both loaders accept all 983 differing images. [Evidence](validation/part-1459/README.md).

Part 1458 checks clause precedence, default-key elimination, terminal if and guarded wildcard cases. DNS release behavior matches for the tested opcodes. Both loaders accept all 983 differing images. [Evidence](validation/part-1458/README.md).

Part 1457 checks guarded identity, case_clause, function_clause and constant-error fallbacks against OTP. The DNS and Diameter release images agree on the tested inputs. Both loaders accept all 983 differing images. [Evidence](validation/part-1457/README.md).

Part 1456 checks shared local tail targets and fallback argument forwarding. The original and generated Megaco parsers agree on valid and incomplete messages in OTP. Both loaders accept all 983 differing images. [Evidence](validation/part-1456/README.md).

Part 1455 checks guarded, unconditional and tail-call fallback paths plus mixed atom results against OTP. All 1,366 sources compile and both loaders accept all 983 differing images. [Evidence](validation/part-1455/README.md).

Part 1454 verifies binary construction and nested tuple fallbacks against OTP. Both loaders accept all 983 differing release images. [Evidence](validation/part-1454/README.md).

Part 1453 verifies boolean and non-boolean filter results, negation, predicate exceptions and improper lists against OTP. Both loaders accept all 984 differing release images. [Evidence](validation/part-1453/README.md).

Part 1452 checks nested and block mappers, existing parent bindings, repeated arguments and mapper error order. All 1,366 release images were generated. Both loaders accept all 984 differing images. [Evidence](validation/part-1452/README.md).

Part 1451 verifies tagged and untagged tuple projection, skipping nonmatching heads and bad_generator for improper tails. Both loaders accept the 984 differing release images. [Evidence](validation/part-1451/README.md).

Part 1450 checks captured values, list order, malformed generators, mapper failures and element-level try/catch against OTP. All 1,366 release images were generated, and both loaders accept the 984 differing images. [Evidence](validation/part-1450/README.md).

Part 1449 verifies multiple checked fields, first badrecord selection and reuse of successful shape checks. All 1,366 release images were generated, and both loaders accept the 984 differing images. [Evidence](validation/part-1449/README.md).

Part 1448 verifies direct field extraction, successful calls and badrecord in the OTP fixture and the .NET runtime. All 1,366 release images were generated. Both loaders accept the 984 differing images. [Evidence](validation/part-1448/README.md).

Part 1447 verifies unused fields, repeated and prebound equality, guards, captures and exported bindings against OTP. Both runtimes accept all 984 differing release images. [Evidence](validation/part-1447/README.md).

Part 1433 verifies register fields passed to constructors and local calls. The original selector remains available for case_clause. [Evidence](validation/part-1433/README.md).

Part 1432 verifies bound-field and literal alternatives, whole-tuple aliases, and the original case_clause failure value on OTP and .NET. [Evidence](validation/part-1432/README.md).

Part 1431 verifies construction into a reused Y register and preserves the original call result when the case reads it. The fixture runs on OTP and .NET. [Evidence](validation/part-1431/README.md).

Part 1430 verifies a leading nil fixture against the complete OTP image. Its first-match behavior and function-clause failures agree on OTP. [Evidence](validation/part-1430/README.md).

Part 1429 verifies a frame-free tagged list entry and nil construction. The fixture matches the entire pinned OTP image and preserves failure on an improper list. [Evidence](validation/part-1429/README.md).

Part 1428 compares handler resume and tuple-arity dispatch on OTP. The .NET validator rejects the existing increasing slot order in the nested fixture. [Evidence](validation/part-1428/README.md).

Part 1427 verifies caught computed throw reasons, observable stacks and reason producer errors with the pinned OTP runtime. All 984 differing generated images are accepted by the OTP loader. [Evidence](validation/part-1427/README.md).

Part 1426 verifies empty-stack raising for caught local throws and preserves stack traces for observing handlers, bound mismatches, escaped functions, cleanup and reason dispatch. The deterministic fixture matches OTP byte for byte. [Evidence](validation/part-1426/README.md).

Part 1425 verifies one- and two-cell literal prefixes, nested tuple construction after a call, and ordinary fallback for a partial or unmatched prefix. The deterministic fixture matches OTP byte for byte. [Evidence](validation/part-1425/README.md).

Part 1424 verifies shared list dispatch, computed cons arguments, and a literal prefix added to a call result only on the returning branch. The deterministic fixture matches OTP byte for byte and preserves an improper suffix when the producer returns one. [Evidence](validation/part-1424/README.md).

Part 1423 verifies shared scalar and tagged tuple dispatch on a record field, list rebuilding with copied fields, and badrecord on an invalid second element. The deterministic fixture matches OTP byte for byte. [Evidence](validation/part-1423/README.md).

Part 1422 verifies direct list extraction into the call frame, preservation of the first call result across the selector, both case branches, and failures before and during the calls. The deterministic fixture matches OTP byte for byte. [Evidence](validation/part-1422/README.md).

Part 1421 verifies a shared list test and extraction for record-field dispatch and cons tails, with separate frames for the call sequences. The deterministic fixture matches OTP byte for byte. [Evidence](validation/part-1421/README.md).

Part 1420 verifies frame-free list entry, proven record updates, original record preservation, and badrecord for unknown shapes. [Evidence](validation/part-1420/README.md).

Part 1419 verifies shared guarded list extraction, prefix lookahead, numeric equality and tuple-result cons tail calls. xmerl_b64Bin_scan matches the Windows OTP release byte for byte. [Evidence](validation/part-1419/README.md).

Part 1418 verifies dynamic tail calls, tagged and atom case branches, constructed exits and register-only tuple returns. xmerl_xpath_lib matches the Windows OTP image byte for byte. [Evidence](validation/part-1418/README.md).

Part 1417 verifies deferred tuple projection, stack slot reuse, known and checked shapes, and original badmatch values. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1417/README.md).

Part 1416 verifies tagged and untagged tuple fields live across calls, successive checked matches, argument reordering, and preservation of the failed value. The complete deterministic fixture matches OTP. [Evidence](validation/part-1416/README.md).

Part 1415 verifies multiple fields in tagged and untagged call-result tuples, argument reordering, and an exact local return shape. The complete deterministic fixture matches OTP and includes consecutive tuple matches that differed with the previous compiler. [Evidence](validation/part-1415/README.md).

Part 1414 verifies consecutive untagged tuple matches before a tail call, tuple return, or call-based case. The deterministic fixture matches the complete OTP image and preserves badmatch for wrong types and arities. [Evidence](validation/part-1414/README.md).

Part 1413 verifies case projection, register-resident fields, caught selector calls and badmatch. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1413/README.md).

Part 1412 verifies checked call sequences, direct field projection and out-of-line badmatch blocks. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1412/README.md).

Part 1411 verifies known and unknown tuple shapes, register argument construction and nested tail arguments. The deterministic fixture matches the complete OTP image and both badmatch cases. [Evidence](validation/part-1411/README.md).

Part 1410 verifies stack and register closure destinations, captured call results and tail calls. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1410/README.md).

Part 1409 verifies nested and sibling tuple-field calls, including the order of two dictionary writes. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1409/README.md).

Part 1408 verifies tuple returns that combine call results, saved arguments and closures. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1408/README.md).

Part 1407 verifies a captured list filter followed by empty and nonempty list branches. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1407/README.md).

Part 1406 verifies register-only process dictionary read sequences with tuple keys and tail argument moves. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1406/README.md).

Part 1405 verifies register dispatch on tuple widths and record tags before frame allocation. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1405/README.md).

Part 1404 verifies colour conversions following option mapping and mixed record and tuple checks before guarded forwarding. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1404/README.md).

Part 1403 verifies nested tuple reconstruction in wx options and commands, including multiple tuple arguments and optional replies. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1403/README.md).

Part 1402 verifies paired colour guards and argument ordering for leading, middle and trailing colour conversions. The deterministic fixture matches the complete OTP image and its values and failures agree under the queue stub. [Evidence](validation/part-1402/README.md).

Part 1401 verifies guarded forwarding before wx object overloads and middle text conversion before options mapping. The deterministic fixture matches the complete OTP image, and its guard failures, protected class fallback and missing environment agree with OTP. [Evidence](validation/part-1401/README.md).

Part 1400 verifies append in a computed call argument, local and external tail calls, live inputs and badarg. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1400/README.md).

Part 1399 verifies dynamic module and function operands, fixed and variable argument lists, and call resolution after a bound call result. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1399/README.md).

Part 1398 verifies size and arithmetic tail operands, error behavior and external call resolution. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1398/README.md).

Part 1397 verifies immediate nested-call forwarding, register and literal captures, and a mixed caller path that retains dynamic input. The deterministic fixture matches the complete OTP image. [Evidence](validation/part-1397/README.md).

Part 1396 verifies nested call operands, tuple conversion errors, closure captures and tail returns. The deterministic regression fixture matches the full OTP image. [Evidence](validation/part-1396/README.md).

Part 1395 verifies nested ordinary calls before a terminal case, selector arguments that share a previous result and source order for effects. The permanent fixture matches the complete OTP image. [Evidence](validation/part-1395/README.md).

Part 1394 verifies grouped tuple arity dispatch and a final fallback. The focused oracle covers incorrect tags and arities, repeated bindings, guard failure, aliases and tail calls. The permanent fixture matches the complete OTP image. [Evidence](validation/part-1394/README.md).

Part 1393 verifies separate frames for nonempty-list branches with tuple and guarded nil fallbacks. The focused oracle checks call-prefix values, empty lists, callback arity and function-clause failure. [Evidence](validation/part-1393/README.md).

Part 1392 verifies reuse of a dying stack slot after argument construction. Both normalized instructions and the complete deterministic regression image match OTP 29.1.1. [Evidence](validation/part-1392/README.md).

Part 1391 verifies case-arm tuples built from two calls, including a result kept through another call and a result both passed forward and returned. The permanent fixture matches its full deterministic OTP 29.1.1 image. [Evidence](validation/part-1391/README.md).

Part 1390 verifies straight call sequences in case arms with preserved stack values, a selector result in X0 and a constructed tail-call argument. List dispatch can extract saved head and tail values directly into the selector frame. The permanent fixture matches its complete OTP 29.1.1 image. [Evidence](validation/part-1390/README.md).

Part 1389 verifies list entry with negated type guards and a following call-based case, plus call-based cases after singleton dispatch. The tests cover both frame-free guard paths and the allocated case path, improper list fallback and whole-image equality for the deterministic fixture. [Evidence](validation/part-1389/README.md).

Part 1388 verifies shared list extraction for singleton and open cons patterns. The public fixture checks empty, singleton, longer and improper lists. A private fixture exercises proper-list inference through a comprehension binding and a forwarding wrapper. [Evidence](validation/part-1388/README.md).

Part 1387 verifies list dispatch into ordinary call sequences, including a head and tail retained across calls, a result saved across an intervening call, the empty-list branch and rejection of a non-list input. The deterministic fixture matches its complete OTP 29.1.1 image. [Evidence](validation/part-1387/README.md).

Part 1386 verifies list dispatch into nested element-based cases, including a constant empty-list return, an exact bound-variable comparison, a recursive tail and errors on invalid tuples and lists. The deterministic fixture matches its complete OTP 29.1.1 image. [Evidence](validation/part-1386/README.md).

Part 1385 verifies saved call results in straight ordinary call sequences, including expired argument slot reuse, overlapping result lifetimes and an argument that reads the same result before it is needed again. The complete deterministic fixture matches OTP 29.1.1. [Evidence](validation/part-1385/README.md).

Part 1384 verifies straight sequences of ordinary calls with bound and discarded results, including a constructed argument using both a saved input and the previous X0 result. A permanent deterministic OTP fixture compares the complete image. [Evidence](validation/part-1384/README.md).

Part 1383 verifies pure dynamic map construction in returns and tail calls, including shared nested values, source-order bindings and live registers at the map heap check. One additional installed release image matches byte for byte. [Evidence](validation/part-1383/README.md).

Part 1382 verifies pure tuple and list construction sequences and constant results following nil entry. Two additional installed release images match byte for byte. [Evidence](validation/part-1382/README.md).

Part 1381 verifies aliased list entry and nested cons call arguments, including a later clause that needs the original tuple head and the heap test before list extraction. [Evidence](validation/part-1381/README.md).

Part 1380 verifies direct register arithmetic tail calls, constructed fallback results and nonnegative counter bounds forwarded across private calls. [Evidence](validation/part-1380/README.md).

Part 1379 verifies arithmetic arguments for recursive calls inside list case arms, including frame-free tails, retained list heads, floats, badarith and overlapping argument moves. [Evidence](validation/part-1379/README.md).

Part 1378 verifies two-arm comparison cases after list dispatch, including a checked record-field selector, recursive fallback, frame-free returns and the badrecord source line. [Evidence](validation/part-1378/README.md).

Part 1377 verifies list dispatch with aliased tuple arguments and if bodies. The oracle covers recursive matches, improper lists and function_clause when a tuple fails despite a proven cons input. [Evidence](validation/part-1377/README.md).

Part 1376 verifies register forwarding into a local or external call inside a returned tuple, including an argument constructed from the first result and fields that remain live after the second call. [Evidence](validation/part-1376/README.md).

Part 1375 verifies delayed tuple-field extraction around arithmetic on a proven local call result. Integer and float arithmetic, preservation of tuple fields, guard failures and badmatch on unproven results are covered. [Evidence](validation/part-1375/README.md).

Part 1374 verifies entry-register list dispatch with terminal control-transfer fallbacks. The oracle covers repeated bindings, proper and improper lists, throws, a nested exit reason and a local function named throw. [Evidence](validation/part-1374/README.md).

Part 1373 verifies branch-local frames for literal clauses with sequence and try bodies. The fixture exercises guard failure, exception handling and slot trimming across calls. [Evidence](validation/part-1373/README.md).

Part 1372 verifies nonempty and empty list clauses, proper-tail analysis and frame placement. The fixture covers improper tails, repeated bindings and first call results forwarded to a terminal call. [Evidence](validation/part-1372/README.md).

Part 1371 verifies literal clause dispatch in registers, branch-local frames and arguments for control-transfer calls. The fixture exercises matching and unmatched keys, live constructor inputs, and exit and throw terms. [Evidence](validation/part-1371/README.md).

Part 1370 verifies stack slot reuse for calls nested in tuple and list arguments. The fixture checks inputs still needed at the terminal call, order of evaluation and exception paths. [Evidence](validation/part-1370/README.md).

Part 1369 verifies register construction for nested tuples, records and lists as call operands. The fixture verifies evaluation order and exception short-circuiting with a computed list. [Evidence](validation/part-1369/README.md).

Part 1368 verifies local and external terminal calls with a first computed call operand and later constructors. The test covers argument liveness, evaluation order and exception short-circuiting. [Evidence](validation/part-1368/README.md).

Part 1367 verifies ordinary calls with multiple computed operands, trailing literals and variables, and exceptions during argument evaluation. The OTP loader accepts all differing release images from this build. [Evidence](validation/part-1367/README.md).

Part 1366 verifies discarding, forwarding and retaining a tagged payload before a terminal case. The case fixture distinguishes badmatch from case_clause and verifies that the selector call is skipped on a failed match. [Evidence](validation/part-1366/README.md).

Part 1365 verifies tagged payloads used after an intervening call, repeated arguments, live entry arguments, and stack relocation before an X0 reload. The failure path keeps the original unmatched value. [Evidence](validation/part-1365/README.md).

Part 1364 verifies named and discarded tagged payloads in consecutive calls, preservation of live entry arguments and the full value of a terminal match. The existing equality check handles a payload variable bound before the match. [Evidence](validation/part-1364/README.md).

Part 1363 verifies tagged call-result matches, payload projection, preservation of earlier values and original badmatch terms. The wx fixture covers both a terminal queue call and a queue followed by a reply. [Evidence](validation/part-1363/README.md).

Part 1362 verifies guards before text conversion, mapped option arguments, class and record overloads, command replies and missing-environment errors. The wx fixture also checks a reference already bound in the function head. [Evidence](validation/part-1362/README.md).

Part 1361 verifies constant propagation in private case branches, including mixed call sites, escaping local function references and the no_module_opt option. [Evidence](validation/part-1361/README.md).

Part 1360 verifies direct argument projection from entry tuples and records, guarded selector fallback, preserved function_clause and closure shadowing in private caller proofs. [Evidence](validation/part-1360/README.md).

Part 1359 verifies tuple and literal cases with no catch-all, nested shape checks, deferred projection and the original value in case_clause. [Evidence](validation/part-1359/README.md).

Part 1358 verifies computed tuple results after a case, including repeated operands, slot relocation, field projection and failure paths. [Evidence](validation/part-1358/README.md).

Part 1357 verifies exact literal case matching, unexpected-selector errors, and recursive inference of two possible selector values. [Evidence](validation/part-1357/README.md).

Part 1356 verifies case selectors after calls, reuse of a stack slot, rounding types, and preservation of function_clause and badarg. [Evidence](validation/part-1356/README.md).

Part 1355 verifies call effects and order, local function references, literal and computed tuple returns, and failure on rebinding. [Evidence](validation/part-1355/README.md).

Part 1354 verifies direct reuse of a bound call result in a returned tuple, including repeated operands and fallback clauses. [Evidence](validation/part-1354/README.md).

Part 1353 verifies linear call forwarding, singleton argument construction and liveness across the first call in a list branch. [Evidence](validation/part-1353/README.md).

Part 1352 verifies proven boolean cases with cons arguments, delayed tuple field reads and failure handling for unproven selectors. [Evidence](validation/part-1352/README.md).

Part 1351 verifies closure captures when a list tail reuses the argument register and compares a branch-local case with OTP. [Evidence](validation/part-1351/README.md).

Part 1350 verifies separate frames for complex list and tuple clauses and compares a bound-call sequence with OTP. [Evidence](validation/part-1350/README.md).

Part 1349 verifies tuple guard failures with the original function argument and delayed tuple field reads in OTP. [Evidence](validation/part-1349/README.md).

Part 1348 verifies tuple arity dispatch for list heads, preservation of fallback inputs and singleton append behavior in OTP. [Evidence](validation/part-1348/README.md).

Part 1347 verifies literal list head dispatch and fallback argument preservation against OTP. The focused probe covers improper list tails, a tail call and a conversion error. [Evidence](validation/part-1347/README.md).

Part 1346 verifies tuple and record tail arguments containing calls, including monitor and ETS table results. The OTP VM checks cover successful calls and conversion failure. [Evidence](validation/part-1346/README.md).

Part 1345 matches the complete wxGridCellBoolEditor release image. A focused OTP VM check with a stub verifies the mapped arguments, queue result and reply behavior. [Evidence](validation/part-1345/README.md).

Part 1344 verifies external list and integer conversion calls, invalid arguments, and local call resolution with auto-import disabled. [Evidence](validation/part-1344/README.md).

Part 1343 verifies nested call chains in separate clause frames, tuple conversion and cons extraction with the list in either argument position. [Evidence](validation/part-1343/README.md).

Part 1342 verifies shared mixed cons extraction, literal dispatch and nil identity fallthrough against OTP. Two escaping functions match decoded instructions after label normalization. [Evidence](validation/part-1342/README.md).

Part 1341 verifies computed cons heads, tail calls, tuple construction, evaluation order and exception short-circuiting against OTP. [Evidence](validation/part-1341/README.md).

Part 1340 verifies exact equality across list pattern bindings and other arguments, including tagged tuple aliases, repeated tails and improper-list fallthrough. [Evidence](validation/part-1340/README.md).

Part 1339 verifies nested cons entry tests, repeated tuple variables, literal fields and remainder guard fallthrough against OTP. [Evidence](validation/part-1339/README.md).

Part 1338 verifies branch-local cons reconstruction against OTP, including tagged and guarded branches, duplicate tuple fields and improper lists. [Evidence](validation/part-1338/README.md).

Part 1337 matches the Windows OTP 29.1.1 error_handler BEAM and verifies registered-process branches, explicit remote call resolution and conversion errors in a focused OTP VM check. [Evidence](validation/part-1337/README.md).

Part 1268 verifies receive tuple arity dispatch, mailbox order, guard fallthrough and timeouts. [Evidence](validation/part-1268/README.md).

Part 1267 verifies mapped queue arguments, closed atom dispatch and constant NIF errors. [Evidence](validation/part-1267/README.md).

Part 1266 verifies Unicode conversion frames and both tuple arity alternatives, including rejection of non-tuples. [Evidence](validation/part-1266/README.md).

Part 1265 verifies type alternatives and tuple tail forwarding, including nonfirst tuple inputs and a four-register rotation. [Evidence](validation/part-1265/README.md).

Part 1264 verifies NUL-terminated Unicode comprehension output, length argument placement and improper-list errors. [Evidence](validation/part-1264/README.md).

Part 1263 verifies exact integer alternative guards, tuple arity tests and tuple tail argument forwarding. [Evidence](validation/part-1263/README.md).

Part 1262 verifies direct calls through a constant module atom with guards, local tail calls and error propagation. [Evidence](validation/part-1262/README.md).

Part 1261 verifies direct field slot initialization and unused binding analysis, including closure captures. [Evidence](validation/part-1261/README.md).

Part 1260 verifies early tagged list head checks and direct field extraction, including repeated matches and nil fallback. [Evidence](validation/part-1260/README.md).

Part 1259 verifies that general cons entry proof preserves deeper list checks and clause errors. [Evidence](validation/part-1259/README.md).

Part 1258 verifies cons-check elimination for proven private entries while retaining checks for unknown inputs, exports and function captures. [Evidence](validation/part-1258/README.md).

Part 1257 verifies shared cons extraction across guarded and unguarded clauses, including improper lists. [Evidence](validation/part-1257/README.md).

Part 1256 verifies guarded list clauses with a saved field on the call path and frame-free recursion on the tail path. [Evidence](validation/part-1256/README.md).

Part 1255 verifies guarded atom case tails with fresh branch bindings and unchanged failure reasons. [Evidence](validation/part-1255/README.md).

Part 1254 verifies computed tail argument order, error precedence, and shared dispatch labels against OTP. [Evidence](validation/part-1254/README.md).

Part 1253 verifies direct local tail calls, argument order, and unmatched selectors in terminal atom cases. [Evidence](validation/part-1253/README.md).

Part 1252 checks identity case folding and preservation of bound fallback constraints against OTP. [Evidence](validation/part-1252/README.md).

Part 1251 checks the stack location of an unmatched atom case against OTP when a branch contains a call. [Evidence](validation/part-1251/README.md).

Part 1250 checks both matched branches and the original unmatched value in an atom case against OTP. [Evidence](validation/part-1250/README.md).

Part 1249 matches the complete snmpa_error image from Windows OTP 29.1.1. [Evidence](validation/part-1249/README.md).

Part 1248 verifies caught singleton-list dispatch against Windows OTP 29.1.1 for snmpa_error. [Evidence](validation/part-1248/README.md).

Part 1235 verifies register-only list scans with repeated bindings and reordered tail-call arguments. OTP prepared all 1,009 differing images for loading. [Evidence](validation/part-1235/README.md).

Part 1195 matches 51 additional debug chunks and preserves all release-image comparison statuses. [Evidence](validation/part-1195/README.md).

Part 1194 adds a byte-identical ct_master_status release image. OTP prepared all 1,006 differing images for loading. [Evidence](validation/part-1194/README.md).

Part 1193 adds a byte-identical constant-bound tuple fixture and preserves all release comparison statuses. [Evidence](validation/part-1193/README.md).

Part 1191 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1191/README.md).

Part 1190 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1190/README.md).

Part 1189 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1189/README.md).

Part 1188 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1188/README.md).

Part 1187 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1187/README.md).

Part 1186 adds a byte-identical guarded tuple-return fixture and preserves all release-image comparison statuses. [Evidence](validation/part-1186/README.md).

Part 1185 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1185/README.md).

Part 1184 preserves all Windows OTP release images and adds an exact per-clause frame fixture. [Evidence](validation/part-1184/README.md).

Part 1183 preserves all release-image comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1183/README.md).

Part 1182 adds a byte-identical guarded dispatch fixture and preserves all release-image comparison statuses. [Evidence](validation/part-1182/README.md).

Part 1181 adds a byte-identical guarded tuple fixture and preserves all release-image comparison statuses. [Evidence](validation/part-1181/README.md).

Part 1180 adds a byte-identical tuple fallback fixture and preserves all release-image comparison statuses. [Evidence](validation/part-1180/README.md).

Part 1179 adds a byte-identical nested tuple fixture and preserves all release-image comparison statuses. [Evidence](validation/part-1179/README.md).

Part 1178 adds an exact OTP tuple entry fixture and preserves all release-image comparison statuses. [Evidence](validation/part-1178/README.md).

Part 1177 preserved all 1,366 comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1177/README.md).

Part 1176 preserved all 1,366 comparison statuses. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1176/README.md).

Part 1175 preserved the status of all 1,366 Windows OTP images. OTP prepared all 1,007 differing images for loading. [Evidence](validation/part-1175/README.md).

Part 1173 adds byte-identical wxGridSizer and wxFlexGridSizer. OTP prepared all 1007 differing images for loading. [Evidence](validation/part-1173/README.md).

Part 1172 adds byte-identical wxFontPickerCtrl. OTP prepared all 1009 differing images for loading. [Evidence](validation/part-1172/README.md).

Part 1080 preserves entry bindings across later calls and initializes the remaining frame before the body continues. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1080/README.md).

Part 1079 preserves list literal failures and alternative-clause selection. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1079/README.md).

Part 1078 preserves named tails and original arguments on failure. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1078/README.md).

Part 1077 preserves exact list lengths and original arguments on pattern failure. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1077/README.md).

Part 1076 preserves guard fallback and original failure arguments with selective cons extraction. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1076/README.md).

Part 1075 preserves guard fallback, repeated bindings and original failure arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1075/README.md).

Part 1074 preserves original arguments on nested cons pattern failure. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1074/README.md).

Part 1073 preserves wildcard and named-unused list patterns, including improper lists. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1073/README.md).

Part 1072 preserves list recursion, wildcard bindings and function_clause arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1072/README.md).

Part 1071 preserves empty-list returns and non-list function_clause arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1071/README.md).

Part 1070 preserves improper list forwarding and case_clause on non-lists. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1070/README.md).

Part 1069 preserves tail call arguments and original function_clause arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1069/README.md).

Part 1068 preserves argument restoration for tuple branches sharing a failure exit. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1068/README.md).

Part 1067 preserves original arguments on failure before the frame exists. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1067/README.md).

Part 1066 preserves clause order inside each tuple arity and restores function_clause arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1066/README.md).

Part 1065 preserves mixed-arity clause selection and function_clause arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1065/README.md).

Part 1064 preserves tuple aliases, clause tags and function_clause arguments. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1064/README.md).

Part 1063 preserves arguments through BIF evaluation and register cycles. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1063/README.md).

Part 1062 emits frameless tuples built from parameters, literals and supported BIFs. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1062/README.md).

Part 1061 preserves tuple field evaluation order. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1061/README.md).

Part 1060 preserves multi-argument evaluation order. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1060/README.md).

Part 1059 preserves bound matches, closure captures and function_clause argument recovery. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1059/README.md).

Part 1058 preserves local and remote resolution of process dictionary and PID calls. The corpus has 348 Equal, 1015 Different, 3 SourceRejected. [Evidence](validation/part-1058/README.md).

Part 1057 adds two exact Diameter supervisor images. OTP prepares all 1015 differing images. [Evidence](validation/part-1057/README.md).

Part 1056 adds two exact SNMP Windows images. All 1017 differing generated files prepare in OTP. [Evidence](validation/part-1056/README.md).

Part 1055 preserves all 344 exact images and OTP preparation for all 1019 differing images. [Evidence](validation/part-1055/README.md).

Part 1054 preserves all 344 exact images and OTP preparation for all 1019 differing images. [Evidence](validation/part-1054/README.md).

Part 1053 adds et to the exact Windows release fixtures. All 1019 differing generated images prepare in OTP. [Evidence](validation/part-1053/README.md).

Part 1052 adds diameter_transport to the exact Windows release fixtures. All 1020 differing generated files prepare in OTP. [Evidence](validation/part-1052/README.md).

Part 1051 preserves all 342 exact images and OTP preparation for all differing images. [Evidence](validation/part-1051/README.md).

Part 1050 preserves all 342 exact release images and OTP preparation for all 1021 differing images. [Evidence](validation/part-1050/README.md).

Part 1049 preserves 342 exact images and OTP preparation for all 1021 differing images. [Evidence](validation/part-1049/README.md).

Part 1048 preserves all 342 exact images and OTP preparation for all 1021 differing images. [Evidence](validation/part-1048/README.md).

Part 1047 preserves the 342 exact images and OTP preparation for all differing images. [Evidence](validation/part-1047/README.md).

Part 1046 retains all 342 exact images and OTP preparation for all 1021 differing images. [Evidence](validation/part-1046/README.md).

Part 1045 retains all 342 exact images and OTP preparation for all 1021 differing images. [Evidence](validation/part-1045/README.md).

Part 1044 retains all exact images and OTP preparation for all differing generated images. [Evidence](validation/part-1044/README.md).

Part 1042 retains all exact release matches and OTP loader preparation for all differing images. [Evidence](validation/part-1042/README.md).

Part 1041 removes the remaining oversized-frame loader failures. OTP 29.1.1 prepares all 1021 differing generated images. [Evidence](validation/part-1041/README.md).

Part 1040 restores OTP preparation for 973 differing images. Five remaining loader failures have oversized frames. All 342 exact release images remain equal. [Evidence](validation/part-1040/README.md).

Part 1039 retains all 342 exact images. OTP prepares 43 of the 1021 differing generated images and returns badfile for 978. [Evidence](validation/part-1039/README.md).

Part 1038 retains all 342 exact Windows OTP 29.1.1 images. In 1002 differing modules, at least one matched function has a larger generated first stack frame. [Evidence](validation/part-1038/README.md).

Part 1037 retains 342 exact Windows OTP 29.1.1 release images, including ELDAPv3. There are 1021 differing images and three rejected sources. [Evidence](validation/part-1037/README.md).

Part 1035 adds the exact alarm_handler release image. [Evidence](validation/part-1035/README.md).

Part 1034 aligns four decoded alarm_handler functions. The whole image still differs. [Evidence](validation/part-1034/README.md).

Part 1033 adds exact release images for httpd_custom_api and megaco_user. [Evidence](validation/part-1033/README.md).

Part 1032 adds the exact ftp release image. [Evidence](validation/part-1032/README.md).

Part 1031 adds the exact ssh_sftpd_file release image. [Evidence](validation/part-1031/README.md).

Part 1030 adds the exact dbg_idb release image. [Evidence](validation/part-1030/README.md).

Part 1029 adds the exact xmerl_xml release image. [Evidence](validation/part-1029/README.md).

Part 1028 adds the exact diameter_transport_sup release image. [Evidence](validation/part-1028/README.md).

Part 1027 adds exact release images for megaco_tcp_accept_sup and ftp_sup. [Evidence](validation/part-1027/README.md).

Part 1026 aligns compile information for every generated Windows release image. Whole-image parity is verified for 332 images. [Evidence](validation/part-1026/README.md).

Part 1025 aligns compile information for the generated images in nine release applications. Their existing whole-image results are preserved. [Evidence](validation/part-1025/README.md).

Part 1024 adds the exact wxGraphicsContext release image. [Evidence](validation/part-1024/README.md).

Part 1023 aligns two decoded drawText instruction sequences. The wxGraphicsContext release image still differs. [Evidence](validation/part-1023/README.md).

Part 1022 adds exact release images for wxGrid and wxGraphicsPath. [Evidence](validation/part-1022/README.md).

Part 1021 adds the exact wxAuiManager release image. [Evidence](validation/part-1021/README.md).

Part 1020 aligns the decoded addPane/3 instructions after normalization. The full wxAuiManager image still differs. [Evidence](validation/part-1020/README.md).

Part 1019 adds the exact wxIconBundle release image. [Evidence](validation/part-1019/README.md).

Part 1018 aligns the decoded instructions of three wx overloads after label normalization. Their complete release images still differ. [Evidence](validation/part-1018/README.md).

Part 1017 adds the exact wxAuiNotebook release image. [Evidence](validation/part-1017/README.md).

Part 1016 adds exact release images for wxMenuItem and wxPrintPreview. [Evidence](validation/part-1016/README.md).

Part 1015 adds exact release images for wxBitmapButton, wxPreviewControlBar, wxSplashScreen and wxStaticBitmap. [Evidence](validation/part-1015/README.md).

Part 1014 adds exact release images for wxBookCtrlBase, wxChoicebook, wxListbook, wxToolbook and wxTreebook. [Evidence](validation/part-1014/README.md).

Part 1013 adds exact release images for persistent_term and megaco_tcp_connection_sup. [Evidence](validation/part-1013/README.md).

Part 1012 adds exact release images for wxAuiDockArt, wxColourData and wxTreeCtrl. [Evidence](validation/part-1012/README.md).

Part 1011 aligns CInf for the full Diameter application and preserves the six exact Diameter images. [Evidence](validation/part-1011/README.md).

Part 1010 adds the exact megaco_udp_sup release image and preserves the previous matches. [Evidence](validation/part-1010/README.md).

Part 968 adds a byte-identical constant append fixture and verifies binary receive behavior. [Evidence](validation/part-968/README.md).

Part 967 verifies expanded equiv metadata against OTP and preserves 223 whole-image matches. [Evidence](validation/part-967/README.md).

Part 966 verifies generator scope and fallback against OTP and preserves 223 whole-image matches. [Evidence](validation/part-966/README.md).

Part 965 verifies inlined closure capture and shadowing and preserves 223 whole-image matches. [Evidence](validation/part-965/README.md).

Part 964 verifies comprehension filter values and order against OTP and preserves 223 whole-image matches. [Evidence](validation/part-964/README.md).

Part 963 verifies dynamic binary extraction and strict failure against OTP and preserves 223 whole-image matches. [Evidence](validation/part-963/README.md).

Part 962 verifies nested branch outcomes against OTP and preserves all 223 whole-image matches. [Evidence](validation/part-962/README.md).

Part 961 verifies rounding and fallback behavior against OTP and preserves 223 whole-image matches. [Evidence](validation/part-961/README.md).

Part 960 verifies extrema and float guard behavior and preserves 223 whole-image matches. [Evidence](validation/part-960/README.md).

Part 959 verifies selective receive values and mailbox order against OTP and preserves 223 whole-image matches. [Evidence](validation/part-959/README.md).

Part 958 adds a byte-identical function-reference fixture and preserves all 223 release image matches. [Evidence](validation/part-958/README.md).

Part 957 verifies nested andalso debug forms and preserves all 223 whole-image matches. [Evidence](validation/part-957/README.md).

Part 956 preserves all 223 whole-image matches. Three newly generated eunit modules match Dbgi. [Evidence](validation/part-956/README.md).

Part 955 verifies direct and macro-expanded sigil forms against OTP and preserves all 223 whole-image matches. [Evidence](validation/part-955/README.md).

Part 954 verifies five comprehension function forms against OTP and preserves all 223 whole-image matches. [Evidence](validation/part-954/README.md).

Part 953 verifies map and catch function forms against OTP. The release typer Dbgi and CInf bytes match. [Evidence](validation/part-953/README.md).

Part 952 preserves all 223 prior whole-image matches and verifies both paths of the branch match probe. [Evidence](validation/part-952/README.md).

Part 951 verifies conditional, receive and timeout values against OTP. Whole-image matches remain at 223. [Evidence](validation/part-951/README.md).

Part 950 preserves all 223 previous whole-image matches and verifies success and function_clause for the nil-pattern fixture. [Evidence](validation/part-950/README.md).

Part 949 verifies both branches of a case binding probe in the managed runtime. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-949/README.md).

Part 948 adds a byte-identical diagnostic options fixture. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-948/README.md).

Part 947 adds a byte-identical automatic-inlining fixture. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-947/README.md).

Part 946 adds two byte-identical feature fixtures. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-946/README.md).

Part 945 adds a byte-identical deterministic sigil fixture. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-945/README.md).

Part 944 adds a byte-identical on_load fixture and validates the declaration and loader paths. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-944/README.md).

Part 943 verifies Diameter record creation, updates, retrieval and indexes. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-943/README.md).

Part 942 verifies emitted match specification terms against OTP. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-942/README.md).

Part 941 adds a deterministic external documentation fixture that matches OTP. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-941/README.md).

Part 940 verifies build macro expansion and conditional selection. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-940/README.md).

Part 939 adds a deterministic guard extraction fixture that matches OTP. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-939/README.md).

Part 938 verifies map and bit pattern bindings and caught errors after inlining. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-938/README.md).

Part 937 adds a deterministic NIF marker fixture that matches OTP. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-937/README.md).

Part 936 adds a deterministic equiv fixture that matches OTP byte for byte. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-936/README.md).

Part 935 verifies selective receive alternatives, map keys and retained messages. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-935/README.md).

Part 934 verifies zipped list generator values and failure payloads against OTP. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-934/README.md).

Part 933 verifies range and native record predicates in guards and expression values. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-933/README.md).

Part 932 verifies operator values, guard rejection and strict xor badarg. The Windows OTP corpus remains at 223 whole-image matches. [Evidence](validation/part-932/README.md).

Part 931 reaches 223 whole-image matches in the Windows OTP corpus. All 31 public_key ASN.1 modules match CInf, Dbgi and Docs. [Evidence](validation/part-931/README.md).

Part 930 admits 11 additional source modules. Whole-image parity remains at 221 modules. [Evidence](validation/part-930/README.md).

Part 929 admits 38 additional release sources. Whole-image parity remains at 221 modules. [Evidence](validation/part-929/README.md).

Part 928 checks guarded fold results and the first failure frame with the pinned lists dependency. Whole-image parity remains at 221 modules. [Evidence](validation/part-928/README.md).

Part 927 checks ordered inline matching and the function_clause failure frame against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-927/README.md).

Part 926 verifies computed receive guard failures and catch-all mailbox order against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-926/README.md).

Part 925 verifies receive filtering and retained mailbox order against pinned OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-925/README.md).

Part 924 verifies the complete Docs payload for included forms against pinned OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-924/README.md).

Part 923 checks targeted inline record construction and updates against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-923/README.md).

Part 922 checks targeted inline argument evaluation and local fun references against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-922/README.md).

Part 921 checks fixed-width binary generator traversal and strict failure reasons against pinned OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-921/README.md).

Part 920 verifies bit widths, filters and errors for binary comprehension output against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-920/README.md).

Part 919 verifies named closure capture, recursion and returned self values against pinned OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-919/README.md).

Part 918 verifies abs, bit_size, trunc and map_get guard outcomes against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-918/README.md).

Part 917 verifies maybe outcomes and map_size guard failures against OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-917/README.md).

Part 916 verifies map comprehension order, filters and generator errors against pinned OTP. Whole-image parity remains at 221 modules. [Evidence](validation/part-916/README.md).

Part 915 checks bitstring, reference and map key guards, including a negated map key guard with an invalid map. Whole-image parity remains at 221 modules. [Evidence](validation/part-915/README.md).

Part 914 verifies seven more complete Windows OTP 29.1.1 wx images. [Evidence](validation/part-914/README.md).

Part 913 verifies byte-for-byte rebuilds of wxCloseEvent, wxEvent, wxClipboard, wxCaret and wxAuiManagerEvent. [Evidence](validation/part-913/README.md).

Part894 verifies byte-for-byte rebuilds for release behaviours and generated wx event modules. [Evidence](validation/part-894/README.md).

Part893 checks fixed-width signed and little-endian segments, binary tails and failure branches. [Evidence](validation/part-893/README.md).

Part892 verifies the OTP Docs layout for a type declared in an included header. [Evidence](validation/part-892/README.md).

Part891 checks literal and bound map keys, nested generators, filters and strict mismatch. [Evidence](validation/part-891/README.md).

Part890 checks generated behaviour_info, callback documentation and source call binding directives against OTP. [Evidence](validation/part-890/README.md).

Part889 verifies direct unary BIF layouts and fallback for computed arguments and constructed results. [Evidence](validation/part-889/README.md).

Part888 checks direct node, port, float and byte size guard layouts and compound guard behavior. [Evidence](validation/part-888/README.md).

Part887 checks try after result preservation, exception propagation and cleanup failure. [Evidence](validation/part-887/README.md).

Part886 checks division, list subtraction, shifts and remainder against OTP. [Evidence](validation/part-886/README.md).

Part885 verifies head failure, number and map guard predicates against OTP. [Evidence](validation/part-885/README.md).

Part884 compares direct integer, binary, UTF and float construction layouts against OTP. [Evidence](validation/part-884/README.md).

Part883 checks length and function guards, including improper lists and invalid arities. [Evidence](validation/part-883/README.md).

Part882 covers map construction, association updates, exact updates and guard failure. [Evidence](validation/part-882/README.md).

Part881 checks element failures in guards, boolean negation and arithmetic multiplication against OTP. [Evidence](validation/part-881/README.md).

Part880 checks PID and tuple size guard behavior and verifies the direct layouts against OTP. [Evidence](validation/part-880/README.md).

Part879 checks guarded tuple record field access and verifies rejection and guard alternatives for malformed records. [Evidence](validation/part-879/README.md).

Part878 verifies direct BEAM function-value calls and a call with computed target and argument. [Evidence](validation/part-878/README.md).

Part877 checks selective receive with bound tuple fields, guards, mailbox retention and timeouts. [Evidence](validation/part-877/README.md).

Part876 verifies local reference creation and FunT row reuse against OTP. [Evidence](validation/part-876/README.md).

Part875 verifies subtraction and negation with direct registers and typed guard results. [Evidence](validation/part-875/README.md).

Part874 verifies numeric equality and inequality against OTP and matches the Windows xmerl_sgml release image. [Evidence](validation/part-874/README.md).

Part873 supports traditional record predicates with a constant tag and positive tuple arity. [Evidence](validation/part-873/README.md).

Part872 verifies inherited since, explicit overrides, authors, format, group and equiv metadata in the deterministic documentation image. [Evidence](validation/part-872/README.md).

Part871 matches deterministic imported calls and verifies their declaration forms against OTP. [Evidence](validation/part-871/README.md).

Part870 verifies catch results for normal return, throw, exit and error. The selected deterministic catch forms match OTP byte for byte. [Evidence](validation/part-870/README.md).

Part869 verifies direct, literal-prefix and tuple-result list concatenation against OTP. [Evidence](validation/part-869/README.md).

Part868 verifies runtime behavior for nested multi-clause closures. The deterministic closure image still differs in instructions and related chunks. [Evidence](validation/part-868/README.md).

Part867 verifies deterministic direct record construction and runtime computed record fields. Eleven additional wx modules match CInf, Dbgi and Docs. [Evidence](validation/part-867/README.md).

Part866 emits dynamic remote calls in value and tail positions. The complete Windows wxDataObject release image matches. [Evidence](validation/part-866/README.md).

Part865 verifies the complete Windows wxGBSizerItem, wxGraphicsBrush, wxGraphicsFont, wxGraphicsPen and wxPreviewCanvas images. [Evidence](validation/part-865/README.md).

Part864 matches the deterministic Unicode and multiline documentation fixture. Full release parity remains scoped to corpus entries marked Equal. [Evidence](validation/part-864/README.md).

Part863 matches the deterministic typed documentation fixture. Full release image parity remains limited to corpus entries marked Equal. [Evidence](validation/part-863/README.md).

Part862 verifies quoted Erlang escapes in the deterministic fixture. The Windows OTP corpus still reports 93 byte-identical release images. [Evidence](validation/part-862/README.md).

Part861 broadens source admission in the Windows OTP 29.1.1 corpus. Byte parity remains limited to the release images listed as Equal. [Evidence](validation/part-861/README.md).

Part860 verifies deterministic fixture parity for the output loop dispatch. The full inet_epmd_socket image remains unsupported. [Evidence](validation/part-860/README.md).

Part799 verifies dynamic detail reply dispatch in an isolated OTP fixture. The complete cdv_detail_wx image remains unsupported. [Evidence](validation/part-799/README.md).

Part798 reproduces the complete Windows OTP 29.1.1 cdv_term_cb image. [Evidence](validation/part-798/README.md).

Part797 moves cdv_term_cb from unsupported to a generated image with byte differences. [Evidence](validation/part-797/README.md).

Part796 verifies the binary, list and tuple expansion instructions in an isolated OTP fixture. The complete cdv_term_cb image remains unsupported. [Evidence](validation/part-796/README.md).

Part795 reproduces the complete Windows OTP 29.1.1 cdv_mem_cb image. [Evidence](validation/part-795/README.md).

Part794 moves cdv_mem_cb from unsupported to a generated image with byte differences. [Evidence](validation/part-794/README.md).

Part793 verifies memory field formatting and split, map and join lowering in a pinned fixture. The complete cdv_mem_cb image remains unsupported. [Evidence](validation/part-793/README.md).

Part792 verifies captured table projection in a pinned fixture. The complete cdv_mem_cb image remains unsupported. [Evidence](validation/part-792/README.md).

Part791 reproduces the complete Windows OTP 29.1.1 cdv_int_tab_cb image. [Evidence](validation/part-791/README.md).

Part790 reproduces the complete Windows OTP 29.1.1 cdv_bin_cb image. [Evidence](validation/part-790/README.md).

Part789 moves cdv_bin_cb from unsupported to a generated image with byte differences. Its three formatting closures match the pinned fixture. [Evidence](validation/part-789/README.md).

Part788 verifies recursive binary digit formatting in a pinned fixture. The complete cdv_bin_cb image still requires closure lowering. [Evidence](validation/part-788/README.md).

Part787 reproduces the complete Windows OTP 29.1.1 cdv_proc_cb image. [Evidence](validation/part-787/README.md).

Part786 reproduces the complete Windows OTP 29.1.1 cdv_sched_cb image. [Evidence](validation/part-786/README.md).

Part785 reproduces the complete Windows OTP 29.1.1 cdv_mod_cb image, including its list of local callbacks. [Evidence](validation/part-785/README.md).

Part784 reproduces the complete Windows OTP 29.1.1 cdv_ets_cb image, including the list title and guarded details page. [Evidence](validation/part-784/README.md).

Part783 reproduces the complete Windows OTP 29.1.1 cdv_port_cb image. The pinned release comparison covers the captured callback, nested reply tuples and list formatting. [Evidence](validation/part-783/README.md).

Part782 reproduces the complete Windows OTP 29.1.1 cdv_dist_cb image from its Erlang source. The corpus still includes source rejection and unsupported code generation. [Evidence](validation/part-782/README.md).

Part781 covers record-details cases and local-to-remote tuple forwarding in a pinned fixture. The cdv_dist_cb release image is still unsupported. [Evidence](validation/part-781/README.md).

Part780 matches the pinned cdv_timer_cb image, preserving macro-expanded arithmetic and record indexes in Dbgi. [Evidence](validation/part-780/README.md).

Part779 matches the pinned cdv_atom_cb release image, including character annotations and wx metadata. [Evidence](validation/part-779/README.md).

Part778 matches the pinned cdv_persistent_cb release image, including wx typed records and private type forms in Dbgi. [Evidence](validation/part-778/README.md).

Part777 matches the pinned ssl_admin_sup release image, including its child specs and guarded option defaults. [Evidence](validation/part-777/README.md).

Part776 matches the pinned inets_lib release image, including clock arithmetic and timestamp formatting. [Evidence](validation/part-776/README.md).

Part775 matches the pinned mnesia_sp release image, including both catch paths and their debug forms. [Evidence](validation/part-775/README.md).

Part774 matches the pinned sasl_report_tty_h release image, including the state-preserving fallback. [Evidence](validation/part-774/README.md).

Part773 matches the pinned prim_tty_sighandler release image, including its map patterns and typed source register. [Evidence](validation/part-773/README.md).

Part772 matches the pinned erl_signal_handler release image, including the empty state record and signal callback branches. [Evidence](validation/part-772/README.md).

Part771 matches the pinned ssl_app release image, including the logger setup literals and ordered calls. [Evidence](validation/part-771/README.md).

Part770 matches the pinned mnesia_checkpoint_sup release image, including the mixed call and tuple flow in init/1. [Evidence](validation/part-770/README.md).

Part769 matches the pinned Diameter config, watchdog, peer FSM and service supervisor release images, including their original debug forms. [Evidence](validation/part-769/README.md).

Part768 matches the pinned asn1ct_table release image, including the generated delete list helper. [Evidence](validation/part-768/README.md).

Part767 matches the pinned beam_opcodes release image, including the opcode and name dispatchers. [Evidence](validation/part-767/README.md).

Part766 matches the pinned erl_bifs release image for all three predicate dispatchers. [Evidence](validation/part-766/README.md).

Part765 matches the pinned erl_reply release image with both application functions aligned. [Evidence](validation/part-765/README.md).

Part764 matches the pinned SSH no-I/O release image, including Code, Attr, CInf, Dbgi and Docs. [Evidence](validation/part-764/README.md).

Part763 retains spec and type forms in release abstract code, with nominal declarations also retained in Attr. Type documentation entries remain open. [Evidence](validation/part-763/README.md).

Part762 preserves cons identity and the original badmatch location for a targeted inline stack reduction. [Evidence](validation/part-762/README.md).

Part761 supports targeted direct inlining for constant and argument-returning functions with pure arguments. [Evidence](validation/part-761/README.md).

Part760 follows pinned OTP file directive mapping for the tested standalone and release debug forms. [Evidence](validation/part-760/README.md).

Part759 matches the pinned fixture with independent, shared and mixed recursive closures in returned tuples. [Evidence](validation/part-759/README.md).

Part758 matches the pinned fixture for a tagged closure and a closure tuple retaining both parent arguments. [Evidence](validation/part-758/README.md).

Part757 matches OTP for the tested accumulator closure and captured offset. [Evidence](validation/part-757/README.md).

Part756 matches OTP for the supported named countdown closure with and without captures. [Evidence](validation/part-756/README.md).

Part755 emits multiple parent captures for supported returned tuple closures in OTP register order. [Evidence](validation/part-755/README.md).

Part754 emits and links simple local BEAM closures with zero or one free variable. [Evidence](validation/part-754/README.md).

Part753 supports named fun recursion and lexical capture in source execution. General closure emission remains open. [Evidence](validation/part-753/README.md).

Part752 handles dynamic module, function and arity operands in external fun references. [Evidence](validation/part-752/README.md).

Part751 handles adjacent ordinary string literals in source and debug forms, including macro expansion and bit syntax. [Evidence](validation/part-751/README.md).

Part750 adds client_server to the release image regression set. The pinned Windows image is byte-identical. [Evidence](validation/part-750/README.md).

Part749 supports static external function literals, including the cleanup callback used by client_server:start/0. [Evidence](validation/part-749/README.md).

Part748 matches the client_server connect and option builder disassembly after label normalization and compares four stubbed connect scenarios. [Evidence](validation/part-748/README.md).

Part647 validates the signed decode result and badmatch/badarg paths while retaining LDAP BER checks. [Evidence](validation/part-647/README.md).

Part646 retains LDAP behavior while removing proven tuple projection calls and ordering select tables. [Evidence](validation/part-646/README.md).

Part645 matches the retained LDAP comparisons and exercises fixed-byte capture and failure. [Evidence](validation/part-645/README.md).

Part644 retains LDAP BER behavior and verifies unconditional fallback and prebound case errors. [Evidence](validation/part-644/README.md).

Part643 passes LDAP BER comparisons and managed arithmetic value, exception and guard tests. [Evidence](validation/part-643/README.md).

Part642 matches LDAP abstract form structure and preserves the retained BER and dispatcher checks. [Evidence](validation/part-642/README.md).

Part641 validates try/of success and error propagation and retains the LDAP BER and dispatcher comparisons. [Evidence](validation/part-641/README.md).

Part640 matches release LDAP record and file forms and all 297 same-source non-file forms. [Evidence](validation/part-640/README.md).

Part639 matches LDAP same-source debug forms and the installed release debug options. [Evidence](validation/part-639/README.md).

Part638 preserves LDAP BER and dispatcher results while changing argument register allocation. [Evidence](validation/part-638/README.md).

Part637 passes the focused compiler checks and the retained LDAP BER and dispatcher comparisons. [Evidence](validation/part-637/README.md).

Part636 matches the retained LDAP BER and dispatcher oracle and exercises wrong tag and tuple shape fallback. [Evidence](validation/part-636/README.md).

Part635 matches LDAP dispatcher forwarding and unknown-type reasons as well as the retained BER comparisons. [Evidence](validation/part-635/README.md).

Part634 matches selected LDAP calls, BER boundaries and info/0 on the generated release-profile image. [Evidence](validation/part-634/README.md).

Part633 matches 88 additional BER boundary calls. The full corpus remains 18/1366 byte-exact; megaco_config_misc now emits a differing image. [Evidence](validation/part-633/README.md).

Part632 matches 320 direct LDAP codec calls, 83 round trips and four protected-expression outcomes against OTP. Full corpus byte parity remains 18/1366. [Evidence](validation/part-632/README.md).

Part631 verifies 25 sequence codec calls against OTP. [Evidence](validation/part-631/README.md).

Part630 verifies 54 direct collection calls against OTP. [Evidence](validation/part-630/README.md).

Part629 verifies 89 direct LDAP calls against OTP. [Evidence](validation/part-629/README.md).

Part628 matches OTP on 180 direct LDAP codec calls. Release image parity remains open. [Evidence](validation/part-628/README.md).

Part627 verifies twelve LDAP values and error scenarios against the release. [Evidence](validation/part-627/README.md).

Part626 verifies nine LDAP range cases against the installed OTP release. Release BEAM byte parity remains open. [Evidence](validation/part-626/README.md).

Part625 verifies the 2,917-byte header, 111 release exports and selected runtime calls. [Evidence](validation/part-625/README.md).

Part623 verifies selected complete LDAP BER behavior. The release BEAM remains byte-different. [Evidence](validation/part-623/README.md).

Part622 verifies exact OTP bytes and eleven runtime paths for the list mapping fixture. [Evidence](validation/part-622/README.md).

Part621 verifies exact OTP bytes and eight runtime paths for the nested tag fixture. [Evidence](validation/part-621/README.md).

Part620 verifies exact OTP bytes and ten runtime paths for the field codec fixture. [Evidence](validation/part-620/README.md).

Part619 verifies exact OTP bytes and ten runtime paths for the isolated choice search fixture. [Evidence](validation/part-619/README.md).

Part618 verifies exact OTP bytes and 289 runtime outcomes for the isolated BER tag probe. [Evidence](validation/part-618/README.md).

Part617 verifies exact OTP output and runtime parity for the isolated signed integer probe. [Evidence](validation/part-617/README.md).

Part616 verifies the isolated LDAP protected call fixture against original OTP bytes and runtime behavior. [Evidence](validation/part-616/README.md).

Part615 verifies byte and runtime parity for the isolated LDAP call and tuple-dispatch fixture. [Evidence](validation/part-615/README.md).

Part614 verifies OTP byte and runtime parity for a fixture combining tagged tuples, a type guard and binary captures. [Evidence](validation/part-614/README.md).

Part613 verifies OTP byte and runtime parity for finite atom and integer dispatch, including function_clause. [Evidence](validation/part-613/README.md).

Part612 matches the OTP abstract spec and documentation payloads and the isolated generated LDAP schema BEAM. [Evidence](validation/part-612/README.md).

Part611 verifies full image parity for dets_sup, including source type annotations in the debug chunk. [Evidence](validation/part-611/README.md).

Part610 adds managed ELDAPv3 source generation and verifies runtime_tools release image parity. ELDAPv3 release image parity remains open. [Evidence](validation/part-610/README.md).

Part609 applies the admitted OTP stdlib include path and prepares ASN.1-derived Erlang source before testing the C# compiler. [Evidence](validation/part-609/README.md).

Part608 verifies complete Windows OTP 29.1.1 release image parity for mnesia_app, including multi-clause debug forms and Docs. [Evidence](validation/part-608/README.md).

Part607 adds an exact Windows release match for tftp_app, including typed tuple operands, case failure, chained calls and debug metadata. [Evidence](validation/part-607/README.md).

Part606 adds exact Windows release matches for odbc_app, runtime_tools_sup, disk_log_sup, and tls_connection_sup. The BEAM compiler covers the tested nil pattern, independent move ordering, and constant binding sequences. [Evidence](validation/part-606/README.md).

Part605 records eight byte-equal installed OTP 29.1.1 images on Windows x64. The release profiles record bootstrap compiler version 10.0.4 and selected module build inputs. [Evidence](validation/part-605/README.md).

Part604 matches the complete Docs fixture bytes on Windows x64 and adds focused BIF and constructor byte comparisons. Other platforms retain the existing ETF compression path. [Evidence](validation/part-604/README.md).

Part603 adds a deterministic whole-image comparison for supported dynamic remote calls. The two documentation images match every other chunk and the raw Docs ETF payload. Compressed Docs equality remains open. [Evidence](validation/part-603/README.md).

Part602 adds focused deterministic comparisons for literal dispatch, static external calls, and predefined macros. Typed record and constrained spec forms have source admission coverage. [Evidence](validation/part-602/README.md).

Part601 extends focused deterministic byte comparison to header line metadata, literal tests, register permutations, and local call frames. This covers the fixture shapes documented in the [validation report](validation/part-601/README.md).

Part600 extends the deterministic BEAM profile to macro-only headers, argument returns, and direct local tail calls. Focused byte equality uses OTP 29.1.1 erlc with deterministic output. [Evidence](validation/part-600/README.md).

Part599 adds focused OTP 29.1.1 whole-image controls for parameterized macros and expression conditionals. Windows release byte equality remains at zero of 1,366. [Evidence](validation/part-599/README.md).

Part598 covers the OTP macro-presence conditional subset and checks a deterministic BEAM image containing active and inactive nested branches. Windows release byte equality remains at zero of 1,366. [Evidence](validation/part-598/README.md).

Part597 extends exact deterministic BEAM fixture equality to six modules, including object macros and one warning-only compile directive. Typed forms are preserved for later compilation. Windows release regeneration remains at zero of 1,366. [Evidence](validation/part-597/README.md).

Part596 verifies selected OTP 29.1.1 triple-quoted string values and documentation form admission. The BEAM emission profile still excludes documentation-bearing modules and has zero regenerations for the Windows release corpus. [Evidence](validation/part-596/README.md).

Part595 matches the full BEAM bytes of four small deterministic OTP 29.1.1 fixtures. The installed Windows corpus still has no byte-equal regeneration. [Evidence](validation/part-595/README.md).

Part594 verifies compact BEAM operand bytes at 22 OTP-29.1.1 test points. The check covers the operand encoder only. [Evidence](validation/part-594/README.md).

sys Part591 implements all37 non-metadata exports in selectable C# alongside unchanged Erlang/BEAM. Both pass137/137 complete focused observations and52 error cases at depths0/1/2/3/8/32 on identical final binaries. Actual file output, original behaviors and five unchanged upstream cases pass. Full CT capture/distribution/platform/resource/metadata acceptance remains outside this finite evidence. [Selection](sys-backends.md), [receipt](validation/part-591/README.md).

Part589 [rpc alternatives](rpc-backends.md):35/35 explicit exports implemented, with unchanged original source/BEAM. Each backend matches241 focused observations and18 scenarios, plus13 distribution-packet checks. These finite Windows results do not certify universal100% compatibility; full upstream peer/EI/old-release, scheduler/resource/platform/metadata/release-boot acceptance remain separate. [Receipt](validation/rpc-port-20261105/README.md).

Part590 [error_logger alternatives](error-logger-backends.md) implement29/29 explicit signatures, retaining unchanged original Erlang/BEAM. Each backend matches143 complete observations and10 unchanged upstream testcase functions. These finite Windows results do not certify universal100% compatibility; distribution, resource/scheduling/platform/metadata and complete release startup acceptance remain open. Full regression and canonical readiness are unchanged. [Receipt](validation/error-logger-port-20261105/README.md).

Part588 [logger alternatives](logger-backends.md): all77 explicit exports have selectable C# and unchanged original Erlang implementations. Each backend passes155 focused observations, including5 unchanged upstream testcase functions, handler events, configuration effects and full tested error stacks. Common Logger servers/handlers/formatters remain original Erlang on .NET. Full regression/readiness and universal upstream/distribution/resource/platform acceptance remain separate. [Receipt](validation/logger-port-20261105/README.md).

io Part587 has a complete selectable C# library implementation and retained original Erlang/BEAM:53/53 exports, with52 library bodies and one shared native primitive. Both pass666/666 focused observations including full errors/stacks/messages and genuine callback/upstream cases. Full terminal/filesystem/old-peer/resource/platform and whole release startup acceptance remain separate; no100% classification or canonical-readiness promotion. [Scope](io-backends.md), [evidence](validation/io-port-20261105/README.md).

The selectable [gen_server alternative](gen-server-backends.md) implements all42 exports beside unchanged original Erlang. Each backend passes107 full API observations and70 scenarios, including four unchanged upstream tests. Whole-module100% compatibility and full upstream/platform/resource/distribution/Logger/metadata acceptance remain unverified; no broad regression/readiness refresh was run. [Evidence](validation/gen-server-port-20261105/README.md).

[application](application-backends.md): all39 non-module_info exports are selectable as C# or original Erlang/BEAM. Both match101 full-outcome observations and19 connected scenarios against OTP29.1.1, including five unchanged application_SUITE functions. Full upstream/distributed/platform/resource/metadata acceptance remains open; finite checks do not certify100% compatibility. [Part584](validation/part-584/README.md).

Part593 implements the native node/dist reference query behind erts_debug and both erts_internal backends. Actual owner/header/storage census, all-scheduler admission, signal/heap classification and retirement are covered by262/262 observations plus59/59 scenarios per backend,13 physical and43 connected groups. Original Erlang/BEAM and98 exports remain. Full regression/readiness were deliberately not run; broader upstream/distributed/resource/platform acceptance and universal compatibility remain unverified. [Receipt](validation/part-593/README.md).

Supervisor Part583 implements24 managed exports and preserves the original Erlang/BEAM alternative, including OTP record layouts, four strategies, retries, significant children, parallel shutdown, sys/code change and Logger callbacks. Both backends pass287/287 API observations and105/105 scenarios each, including19 unchanged upstream testcases; no universal100% or canonical-readiness promotion. [Scope and evidence](validation/part-583/README.md).

gen_event Part580: all44 non-metadata exports have explicit C#/original BEAM alternatives. Primary105/105 per backend passes; Managed depth3 is104/105 because an upstream zero-timeout assumption races an already queued response. Whole-module/scheduling compatibility remains partial. [Finite evidence](validation/part-580/README.md).

## Current state and sources

[Part579 gen_statem](validation/part-579/README.md) implements40 non-metadata exports and the complete C# engine beside unchanged original Erlang/BEAM. Each passes74 complete API outcomes/stacks and311 focused engine scenarios, including256 mode/event combinations and sys exactly-one termination. This finite Windows evidence does not certify100% OTP compatibility: full upstream suite, owned-heap/resource/platform/distribution/log-handler/metadata acceptance and global readiness remain open. See [backend scope](gen-statem-backends.md).

[Part578 timer](validation/part-578/README.md) exposes all36 library exports through explicitly selected C# and unchanged original Erlang alternatives. Both pass728 complete primary observations,11 scenarios,47 upstream testcase bodies and six backtrace-depth profiles. The clean scoped build uses no exclusions. Finite evidence does not certify universal timing/resources/distribution/platforms or refresh canonical readiness.

[Part569 ETS](validation/ets-port-20261104/README.md): Both backends pass 355 exact observations, 8 stateful scenarios, 12 terminal/regex scenarios and 75 real filesystem/DETS/corruption scenarios each (450 per backend). Official OTP also reads both exported dumps. Original Erlang is unchanged; 25 C# library exports compose 51 shared primitives. Full upstream/platform/resource/scheduling/metadata/startup-manifest acceptance remains open. No full regression or global readiness refresh ran.

[Part566 gen_event](validation/part-566/README.md) implements all 44 non-metadata signatures in C# beside unchanged original Erlang/BEAM. Each backend matches 105 focused observations, including ten unchanged upstream functions and complete error stacks. This is finite Windows acceptance; full gen_event_SUITE, distribution/resource/platform/metadata certification and canonical readiness remain separate.

[Part565 timer](validation/part-565/README.md) adds all36 managed exports beside
unchanged original Erlang/BEAM. Both alternatives pass728 primary observations,
47 non-benchmark upstream tests and depth0/1/2/3/8/32 stack profiles. Final
malformed-state checks preserve the public callback's case-clause behavior.
Finite Windows observations do not certify universal timing, distribution,
resource or platform equivalence; canonical readiness is not regenerated from
the focused reports.

The [file alternative](file-backends.md) covers all 69 exports and matches 408
focused OTP observations per backend, including complete captured stacks. Five
separate raw/normal/RAM/consult/compressed cycle gates fail in both alternatives;
missing native filesystem support prevents full compatibility. Original Erlang
is preserved and no file MFA is promoted from these finite checks. See
[Part568](validation/part-568/README.md).

The [code port](validation/code-port-20261104/README.md) implements all69 exports beside unchanged original Erlang. Each backend passes156 full-outcome observations and25 real-BEAM lifecycle results;17 connected groups pass. Existing non-BEAMASM coverage/debug, full release startup and whole-platform/resource acceptance remain separate. No universal100% classification is claimed.

[DETS](dets-backends.md): all64 explicit exports now have selectable C#/original Erlang implementations. Native v9 incremental storage, repair, ownership, callbacks/continuations, bchunk/QLC/ETS and connected raw-file/buffer/RAM primitives are implemented. Focused OTP acceptance and limitations are recorded in [the receipt](validation/dets-port-20261104/README.md); no full regression or canonical readiness refresh.

[Part563 Unicode](validation/part-563/README.md) implements the complete 22-export
managed alternative beside unchanged original Erlang. Each backend matches
66,458 primary observations (including complete exception stacks), a 490-case
permanent subset and 29 binary backing probes. Nineteen connected regression
groups and both MSBuild selections pass. This scoped run does not refresh global
readiness or certify universal platform/resource equivalence.

[Part562](validation/part-562/README.md) audits all71 string APIs/119 source clauses, repairs the newly reproduced search/lazy-prefix/numeric/Jaro gaps and retains both implementations. Each final backend passes37145 observations plus8522 upstream-inclusive cases and138 depth-profile observations. All9649 regression tests pass. No compatibility classification is promoted from finite counts.

The [string backend contract](string-backends.md) preserves both 71-export implementations and compares complete returned terms, exception reasons/stacks, binary backing and backtrace-depth profiles against OTP29.1.1. Its finite acceptance evidence and global regression gates are recorded separately.

[Part561](validation/part-561/README.md) adds a large original input and removes
quadratic C# binary-tail copying:8522 complete cases pass in each backend. The
Windows user-CPU provider uses direct per-thread queries with real measured time.
The complete local regression passes9647/9647 and readiness validation passes.
Whole-platform/resource equivalence and publication-head CI remain separate.

Current [ordsets verification](ORDSETS_COMPATIBILITY.md) covers all 21 APIs with exact outcomes/stacks/effects and repairs native reverse source frames. Universal acceptance remains open.

Compatibility is baseline-bound and tracked per command. A registered API, a prepared original image, a finite comparison and full contract acceptance are separate claims. Read each source with its revision and recorded limitations.

- [API compatibility index](API_COMPATIBILITY.md): command classifications, complete scopes, ordered evidence and hashes.
- [Module readiness](MODULE_READINESS.md) and [machine-readable readiness](module-readiness.json): generated registration/component snapshot.
- [Compatibility ledger](mfa-compatibility.json): authoritative command records. Rendering documentation does not refresh runtime or source-binding acceptance.
- [Known semantic boundaries](semantic-differences.md): limitations and their history.
- [Backend selection](RUNTIME_LIBRARY_SELECTION.md) and [math backends](math-backends.md): explicit alternatives and scoped comparisons.
- [Remaining work](remaining-work/INDEX.md) and the [active changelog](../CHANGELOG.md): later checkpoints, incomplete gates and delivery evidence.

The sources may represent different checkpoints while work is in progress. Resolve differences against their exact source/report revisions; do not combine stale counters into a new acceptance claim. This documentation split changes no API classification or runtime behavior.

## Baseline and acceptance

The pinned baseline is defined in [reference-baseline.json](../reference-baseline.json). Follow the [architecture decisions](decisions/index.md) and current [project rules](../AGENTS.md). Full compatibility needs the complete supported observable contract and authentic reference evidence; finite local regression or image availability alone is insufficient.

## Historical checkpoints

Every prior paragraph, result, failed attempt and unfinished scope is retained in [ordered compatibility history](history/compatibility/index.md). Old headings labelled current refer to their recorded checkpoint. Put new results in the active changelog and update the owning ledger/report instead of appending historical narratives here.

## erpc alternative — 2026-11-05

All33 explicit OTP29.1.1 exports have a selectable C# implementation and retained original Erlang/BEAM. Calls, casts, request collections, multicast/multicall, source-stack trimming and timeout abandonment share the existing runtime/distribution machinery. Both implementations match330 focused observations, including3 unchanged upstream tests;8 connected RPC/spawn/monitor checks pass. This is finite Windows evidence, not universal100% acceptance. Full regression and canonical readiness are deliberately unrun. See [backend selection](erpc-backends.md) and [immutable receipts](validation/erpc-port-20261105/README.md).
