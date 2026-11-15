# Part 1772

Extend the BEAM entry frame pass for guarded tagged tuples. Move the tag tests and proven tuple projections into X registers and allocate on each path that needs a frame. Copy a projected field to its original Y slot after allocation. The pass checks register availability and rejects any route from a framed section back into the frameless prefix.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 399 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 967 differing pairs.

The first disassembled difference changed from a tagged-tuple test versus allocation for 109 functions in 66 modules.

Compared 334 changed images. Across all functions, 10 instruction matches were gained and 0 were lost after normalization. The sum of absolute Code-size differences increased by 2788 bytes.

967 differing images, 0 with the same Code payload and 0 with all chunk payloads equal. 0 differing images have identical instruction bytes after removing the Code header.

The fixture passed 11 assertions on the previous compiler, the new compiler and OTP. The previous nested tuple entry allocates its frame before the tag test. The new and OTP entry test the tag first and the default returns without a frame. The two framed branches still use two slots in C# and one in OTP. All 11 targeted tests and all seven BEAM cases passed. The previous compiler failed the new frame placement regression.

The compiler block passed 3721 of 3830 tests. The same 108 failed with part 1771. The original string Jaro test also timed out and had reproduced on an earlier compiler build. The full runtime suite and readiness regeneration remain pending.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1772-tagged-entry-frames-safe-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1772-tagged-entry-frames-safe-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Compact live Y slots separately for each branch frame. A larger allocation in C# is the first difference for 8244 functions across 856 modules.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [Differences by BEAM chunk](chunk-rank.txt) · [First instruction differences](first.txt) · [Tagged entry transitions](tagged-changes.txt) · [Filter rank](filter-rank.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Independent comprehensions](multiple-list-comprehensions.txt) · [Tagged entry groups](tagged-entry.txt) · [Helper callers](helper-callers.txt) · [Fixture](beam_entry_frame_probe.erl) · [Fixture execution](fixture-behavior.txt) · [Nested tuple instructions](nested-instructions.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
