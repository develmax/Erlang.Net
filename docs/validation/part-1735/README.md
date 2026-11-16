# Part 1735

Remove a stack-to-X copy before an eligible branch predicate or selector when the tested operand can use the Y slot and the X value is dead on all continuations. The proof accounts for implicit reads by calls, allocation, heap tests and return. Keep exception-handler functions outside this transformation. Preserve the compact type annotation and any intervening line instruction. The reference selector lowering is in OTP beam_ssa_codegen.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 522 images and reduces the total absolute Code gap by 13948 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

4646 branch operands change from X to Y in 3090 functions across 522 modules with the same previous and current predicate sequence. 5 match the reference operand in functions with the same reference, previous and current predicate sequence.

The build and six targeted tests passed. The standalone probe uses the source of [part 1734](../part-1734/beam_branch_frame_3.erl) and checks direct Y1 selection plus both branch results and fallback in the OTP VM. The probe images still differ. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Branch operands](branch-operands.txt) · [Previous reloads](reloads-before.txt) · [Remaining reloads](reloads-after.txt) · [Probe](saved-selector-probe.txt) · [Result spills before branches](remaining-result-spills.txt)
