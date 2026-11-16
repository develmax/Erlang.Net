# Part 1764

Extract a recursive BEAM helper for a list comprehension with one relaxed variable generator, a variable input, a statically resolved call as its output and no filters. Substitute the helper call in the parent expression. Pass free values after the input list and preserve the order of incoming arguments. Use the shared recursive list builder for nested call arguments and multiple captures. For a local two-argument mapper with the capture first, use the OTP register order and preserve the result across the recursive call by trimming the stack.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 398 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 968 differing pairs.

435 new OTP list comprehension helper identities and 0 lost, across 159 changed modules with helper differences. Normalized helper instructions gained 243 matches and lost 0.

Across 161 changed images, 355 functions gained instruction matches and one lost its match after normalization. The sum of absolute Code-size differences decreased by 4886 bytes.

The lost match was asn1ct_check:make_constr_set_vs_1/1. The emitted nil test on the list tail lost the operand type annotation and the empty-list exit gained a nil test. [Instruction comparison](asn1-difference.txt).

All 11 targeted tests and seven BEAM harness cases passed. The new regression fails with part 1763. The pinned OTP VM agreed on eleven fixture checks using the pinned OTP, prior and current images. Three new fixture helpers match the release disassembly after normalizing local labels and source paths. The first stack frame for an improper list now identifies the same helper as OTP. After formatting, the compiler rebuild preserved metadata except MVID and all method bodies in the tested IL.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1763. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1764-recursive-list-comprehensions-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1764-recursive-list-comprehensions-final-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Both list comprehension helpers and their parents for alias and negation substitution in proplists match OTP instructions after normalization. The proplists image still differs.

The source rankings count parents with a single list comprehension and a missing release helper. Remaining groups ranked by module count follow.

- 95 modules 136 source functions {list,var,var,call,0,true}
- 69 modules 87 source functions {list,tuple,var,tuple,0,true}
- 68 modules 93 source functions {list,var,var,var,1,true}
- 62 modules 91 source functions {list,tuple,var,var,0,true}


Recursive lowering for relaxed tuple generators over a variable can address 254 source functions in 149 modules. OTP skips nonmatching heads with a tail call to the helper.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Residual call forms](residual-lc.txt) · [Helper callers](helper-callers.txt) · [proplists comparison](proplists.txt) · [Fixture](beam_recursive_list_comprehensions.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Exception stack frame](fixture-stack-frame.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler rebuild](compiler-rebuild.txt) · [Compiler block](compiler-block-summary.txt)
