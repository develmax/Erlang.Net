# Part 1769

Share Core ordinals for side-by-side list comprehensions and anonymous closures. Retain the SSA counter for recursive list helpers and emit those helpers at their closure boundaries. Reuse deferred call labels so both kinds of lifted definitions can appear in OTP order. Nested comprehensions inside an anonymous function need a separate scope.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 968 differing pairs.

277 new OTP list comprehension helper identities and 0 lost, across 44 changed modules with helper differences. Normalized helper instructions gained 11 matches and lost 0.

3 new OTP anonymous closure identities and 0 lost, across 3 changed modules with helper differences. Normalized helper instructions gained 1 matches and lost 0.

Compared 52 changed images. Across all functions, 13 instruction matches were gained and 0 were lost after normalization. The sum of absolute Code-size differences decreased by 3407 bytes.

968 differing images, 0 with the same Code payload and 0 with all chunk payloads equal. 0 differing images have identical instruction bytes after removing the Code header.

The pinned OTP runtime agreed with the previous and new compiler on 14 assertions, including closure captures and bad_generator. Six lifted function bodies gained instruction matches after normalization. The order matches for all 19 emitted definitions in the fixture. The OTP image additionally contains a recursive helper for a separate nested comprehension. All 11 targeted tests and all seven BEAM cases passed. The previous compiler failed the new regression.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1768. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1769-comprehensions-with-closures-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1769-comprehensions-with-closures-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Emit function_clause stubs for captured anonymous closures with a refutable head or guard. The default compiler options group has 291 missing SSA-generated stubs across 136 modules, each called from one anonymous function.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [Differences by BEAM chunk](chunk-rank.txt) · [Filter rank](filter-rank.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Independent comprehensions](multiple-list-comprehensions.txt) · [Remaining contexts](comprehension-contexts.txt) · [Closure differences](closure-delta.txt) · [Missing failure stubs](inlined-failures.txt) · [Failure stub probe](failure-stub-probe.txt) · [Helper callers](helper-callers.txt) · [Fixture](beam_closure_comprehension.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Helper order](helper-order.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
