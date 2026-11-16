# Part 1770

Emit function_clause stubs for anonymous closures that capture values and can reject incoming arguments. The call to the stub passes the source-level arguments. Reuse register guard tests and atom selection to defer frame allocation for entry dispatch when every result can be returned in registers.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 968 differing pairs.

0 new OTP list comprehension helper identities and 0 lost, across 0 changed modules with helper differences. Normalized helper instructions gained 0 matches and lost 0.

253 new OTP function_clause stub identities and 0 lost, across 128 changed modules with helper differences. Normalized helper instructions gained 228 matches and lost 1.

Compared 138 changed images. Across all functions, 228 instruction matches were gained and 1 were lost after normalization. The sum of absolute Code-size differences increased by 6479 bytes.

The one lost instruction match is observer_procinfo init_stack_page/2. Late emission of the nested closure assigned suffix 0 to the failure at line 285 and suffix 1 to the failure at line 267. OTP assigns them in the opposite order. [Failure ordinal probe](failure-ordinal-probe.txt).

968 differing images, 0 with the same Code payload and 0 with all chunk payloads equal. 0 differing images have identical instruction bytes after removing the Code header.

The pinned OTP runtime agreed with the new compiler on 16 assertions. Seven failure assertions check name, argument list and line in the first stack frame. The fixture has six matching function_clause stubs and 24 matching definition identities in order. Five closure bodies and five failure stubs newly match after normalization. All 11 targeted tests and seven BEAM cases passed. The previous compiler failed the new regression.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1769. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1770-captured-closure-failures-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1770-captured-closure-failures-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Move shared tagged-tuple tests before frame allocation. The first instruction mismatch has this shape in 1687 functions across 363 modules. Start with handle_info/2 in logger_handler_watcher, where it is the only function whose disassembled instructions differ after label normalization.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [Differences by BEAM chunk](chunk-rank.txt) · [First instruction differences](first.txt) · [Tagged-tuple entry probe](tagged-tuple-entry-probe.txt) · [Filter rank](filter-rank.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Independent comprehensions](multiple-list-comprehensions.txt) · [Remaining contexts](comprehension-contexts.txt) · [Failure stub differences](stub-delta.txt) · [Remaining stub callers](failure-callers.txt) · [Missing failure stubs](inlined-failures.txt) · [Stack arguments](failure-arguments.txt) · [Helper callers](helper-callers.txt) · [Fixture](beam_closure_failures.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Helper order](helper-order.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
