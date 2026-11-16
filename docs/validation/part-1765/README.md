# Part 1765

Lower a single relaxed tuple generator over a variable to a recursive list comprehension helper. A matching head contributes to the result. A nonmatching head tail-calls the helper on the remaining list. Preserve the improper input error. For a tuple projection with distinct fields, save the head and captures over the recursive call. Reuse the original tuple if all output fields match in order. Read projected fields afterward when the output differs.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 968 differing pairs.

191 new OTP list comprehension helper identities and 0 lost, across 116 changed modules with helper differences. Normalized helper instructions gained 5 matches and lost 0.

Compared 120 changed images. Across all functions, 57 instruction matches were gained and 0 were lost after normalization. The sum of absolute Code-size differences decreased by 320 bytes.

A generated comprehension helper returns a proper list on success. Preserve that fact during caller analysis after lowering. This restored normalized instruction parity for asn1ct_check:make_constr_set_vs_1/1, including its typed tail test and empty exit.

The pinned OTP runtime agreed with the previous and new compiler on 11 fixture assertions. The assertions cover skipping, repeated exact bindings, nested patterns, capture and tuple order, observable effects and invalid list tails. Two new fixture helpers match OTP after normalizing labels and paths. All 11 targeted and all seven BEAM harness cases passed. The previous compiler fails the new regression.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1764. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1765-tuple-list-comprehensions-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1765-tuple-list-comprehensions-final-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Lower variable and tuple generators whose source is a call with no filters. The group covers 226 source functions in 126 modules. The most common first mismatch in new helpers is 97 modules 154 helpers {get_list,get_list}.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [First differences in new helpers](new-helper-first.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Helper callers](helper-callers.txt) · [ASN.1 instruction comparison](asn1-proper-list.txt) · [Fixture](beam_tuple_list_comprehension.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
