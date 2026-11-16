# Part 915 bitstring, reference and map key guards

The general emitter uses the BEAM type tests for positive bitstring and reference guards and the existing unary BIF path when the predicate result is used as a value. A map key guard uses bif2 with the guard failure label, then the enclosing guard tests the result. An invalid map takes the failure edge even for a negated guard.

The pinned Windows OTP compiler and runtime produced the reference matrix and disassembly for the regression fixture. The same matrix checks the C# generated image through the managed BEAM loader and runtime. [OTP reference](reference.txt).

All three selected compiler checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run produced 221 Equal, 754 Different, 105 SourceRejected and 286 Unsupported across 1,366 modules. Twenty modules moved from Unsupported to Different, with no other status changes. Of the 39 modules previously blocked at these guard calls, 19 revealed subsequent blockers. [Corpus report](otp-corpus.json).
