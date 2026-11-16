# Part 1432 — Keep case selectors in x0 across pattern tests

The general case emitter now tests supported unguarded patterns on the result register. A tagged tuple test reads x0 and compares literal or bound fields through x1. Fresh field bindings use the existing frame rules. Each failure edge reaches the next clause with the original selector still in x0. If all clauses reject it, case_end uses that original value.

The focused oracle exercises a bound field comparison, a literal alternative, a whole-tuple alias and two kinds of case_clause failure. Both images pass on OTP. The C# fixture frame shrinks from seven slots to four. OTP uses two slots and shares the tuple arity dispatch.

asn1ct:compile_proc/3 now uses six slots, down from eight and compared with three in OTP. It still copies fresh fields into Y slots and emits a tagged test for each clause. OTP groups the tuple arities and loads accepted fields into call registers.

- [Oracle](check-case-register-selector.ps1), [previous compiler](previous-probe.txt), [current compiler](probe.txt), and [compile_proc/3](compile-proc.txt)

The final Windows OTP 29.1.1 run generates all 1,366 release images with zero source rejections or compiler errors. There are 382 Equal and 984 Different. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepts all 984 differing images for loading.

There are 658 changed images and 3,320 changed functions. The frame size moves closer to OTP in 2,358 functions, including 128 that reach it, and stays equally distant in 962. The aggregate Code size gap decreases by 71,465 bytes. Whole-image and normalized-function equality counts stay unchanged. Three selected .NET tests pass with the corpus compiler assembly.

- [Corpus](otp-corpus.json), [frame statistics](summary.txt), [instructions](normalized-functions.txt), and [loader comparison](loader-and-code.txt)
- [Focused tests](focused.json), [compiler assembly hashes](compiler-hash.txt), and [remaining allocation groups](sequence-endings.txt)
