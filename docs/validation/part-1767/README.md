# Part 1767

Lower a single filter after a relaxed variable or tuple list generator. An unsuccessful guard skips the element. An ordinary expression filter evaluates once per element and raises bad_filter on a non-Boolean result. A simple guard on an identity projection skips the frame allocation when it fails. A Boolean call filter with a tuple projection reuses the existing filter frame.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 968 differing pairs.

156 new OTP list comprehension helper identities and 0 lost, across 109 changed modules with helper differences. Normalized helper instructions gained 3 matches and lost 0.

Compared 113 changed images. Across all functions, 42 instruction matches were gained and 0 were lost after normalization. The sum of absolute Code-size differences increased by 1500 bytes.

968 differing images, 0 with the same Code payload and 0 with all chunk payloads equal. 0 differing images have identical instruction bytes after removing the Code header.

The pinned OTP runtime agreed with the previous and new compiler on 14 fixture assertions. They cover guard failure, captured values, a false result, a non-Boolean result, exceptional filters, and malformed generator input. All four parent functions and two recursive helpers match after normalizing labels and paths. The two tuple guard helpers still differ. All 11 targeted and all seven BEAM harness cases passed. The previous compiler fails the new regression.

The compiler block passed 3721 of 3830 tests. The same 108 failed with part 1766. The original string Jaro test also timed out in this broad run. The timeout reproduced with the previous build in a separate invocation. The full runtime suite and readiness regeneration were outside this run. The scoped layout check reported the same four files at both revisions.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1767-filtered-list-generators-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1767-filtered-list-generators-final-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Lower functions with two independent comprehensions and reproduce the helper ordinals assigned in the Core and SSA passes. The ranked group contains 159 functions across 119 modules.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [Differences by BEAM chunk](chunk-rank.txt) · [Filter rank](filter-rank.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Independent comprehensions](multiple-list-comprehensions.txt) · [Helper callers](helper-callers.txt) · [Fixture](beam_filtered_list_generator.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt) · [Isolated Jaro rerun](jaro-isolated.txt)
