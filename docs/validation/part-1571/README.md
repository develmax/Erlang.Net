# Part 1571 — Return tuples directly from protected branches

A terminal try with a direct call and simple tuple results can return from each branch with a frame containing only the exception token. A following tuple constructor that consumes the try result once can be emitted in both branches. The compiler reuses the tuple planner, preserves the X1 reason until its last read, and uses X2 and X1 for unmatched rethrow.

The fixture frame shrank from three Y slots to one. Its instructions match OTP after normalizing source filenames. Four execution outcomes and three focused compiler tests passed. [Fixture](../part-1568/beam_unused_catch_stack.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. Only prim_inet changed. Six functions gained an instruction match after label normalization and none lost one. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code sizes](loader-and-code.txt).
