# Part 1437 — Skip unused register case projections

The register case emitter plans field extractions from the accepted branch's variable uses. Fresh unused bindings become wildcards in the checked pattern. A field that repeats an earlier binding still takes the existing matching path. An alias uses x0 when the branch needs the original tuple. After all tests, a single live field can replace the selector in x0 for a returned value or constructor.

The fixture covers returning a whole alias, returning a construction that uses a single extracted field, forwarding a field into a call, a repeated-field match and rejection with the original selector. The OTP oracle checks execution and the absence of unused projections. In compile_proc/3, the input_file_error branch now goes directly from its label to test_heap and put_tuple2, matching that part of the OTP branch.

The implementation reuses LocalFunctionEntries.Walk for expression operands and the existing register construction plan for heap liveness. The OTP reference runs ssa_opt_live and moves get_tuple_element to its use blocks in beam_ssa_opt.erl.

- [Fixture instructions](probe.txt), [release function instructions](compile-proc.txt), and [focused .NET tests](focused.json)

All 1,366 Windows OTP 29.1.1 release images were generated. The strict comparison reports 382 Equal, including ELDAPv3 generated from ASN.1 in C#, and 984 Different. Both runtimes accepted all 984 differing images. The emitter changed 283 images and 613 functions. The aggregate absolute Code size gap fell by 1,408 bytes. The normalized function equality count and the strict image count did not change.

The opcode scan previously found 38 functions in 35 images with a field extraction followed immediately by a heap check that drops the extracted register and a tuple construction. It finds zero after the change. A separate scan identified 93 functions in 38 images where OTP uses short-circuit guard tests and the C# code has the corresponding value-producing BIFs, including wxLocale:new/1 and wxFont:new/1.

- [Corpus](otp-corpus.json), [delta](summary.txt), [normalized functions](normalized-functions.txt), [projection scan before](../part-1436/dead-projections.txt) and [after](dead-projections-after.txt)
- [Short-circuit guard scan](short-circuit-guards.txt) and [wx instructions](wx-near-instructions.txt)
