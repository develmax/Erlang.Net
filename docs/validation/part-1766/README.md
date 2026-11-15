# Part 1766

Lower a single relaxed variable or tuple generator over a statically resolved call. Evaluate the source expression in the parent and pass the result as the first argument of the recursive helper. Preserve the improper input error when the result may be improper. For selected standard calls that return proper lists, return nil from the non-cons arm. Reuse the shared recursive list construction sequence for identity projections.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 968 differing pairs.

171 new OTP list comprehension helper identities and 0 lost, across 97 changed modules with helper differences. Normalized helper instructions gained 25 matches and lost 0.

Compared 98 changed images. Across all functions, 96 instruction matches were gained and 0 were lost after normalization. The sum of absolute Code-size differences decreased by 1601 bytes.

968 differing images, 0 with the same Code payload and 0 with all chunk payloads equal. 0 differing images have identical instruction bytes after removing the Code header.

The pinned OTP runtime agreed with the previous and new compiler on 13 fixture assertions. The assertions cover exactly one source call, empty and improper input, a source exception, captured values and use in a tuple. All four new fixture helpers and their parents match after normalizing labels and paths. All 11 targeted and all seven BEAM harness cases passed. The previous compiler fails the new regression.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1765. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1766-call-list-generators-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1766-call-list-generators-final-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Lower one filter after a relaxed variable or tuple generator over a variable. The group covers 182 source functions in 113 modules, with 77 guard filters and 105 Boolean expression filters.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [Differences by BEAM chunk](chunk-rank.txt) · [Filter rank](filter-rank.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Helper callers](helper-callers.txt) · [Fixture](beam_call_list_generator.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
