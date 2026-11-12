# Part 1395 — Carry call sequence values into a terminal case

The saved-call planner tracks reads in terminal case branches alongside ordinary call arguments. It accepts a final selector call or a case on the last bound call result. Nested ordinary arguments are evaluated in source order when the other operands are already prepared. The selector can copy X0 to its argument register before swapping a dying Y slot into X0. The case reads the preserved inputs and results from their final stack slots.

The OTP oracle exercises both case outcomes, nested argument evaluation order and an intermediate result used by both a call and a branch. All three probe functions reach the OTP frame size. The options function matches normalized OTP instructions. A separate deterministic fixture with a simple list helper matches its complete OTP image. The selected compiler cases, whole-module check and earlier tuple dispatch oracle pass using the corpus compiler assembly.

All 1,366 Windows OTP 29.1.1 images generate. The strict comparison has 372 Equal, 994 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

Ten release images change. Six functions gain normalized instruction matches and none lose one. All nine changed peak frames reach the OTP size. In asn1ct:includes/2 the peak frame falls from five slots to two and the normalized instructions match. Nine Code size gaps narrow by a net 177 bytes, with none wider.

The largest frame group still covers 1,798 functions across 558 modules. The bound local-call subgroup contains 611 functions in 237 modules. Its largest subgroup by module count has a bound local call followed immediately by a local tail call, with 100 functions in 38 modules. Some tail arguments still require BIF results or closures. The compile_proc/3 example still needs a record construction between the prefix and selector. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Release instructions](release-case-prefix.txt)
- [Normalized functions](normalized-functions.txt)
- [Peak frames](peak-slots.txt)
- [Code size gaps](code-delta.json)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Continuations before](case-continuations-before.txt)
- [Continuations after](case-continuations-after.txt)
- [Focused oracle](check-call-sequence-case.ps1)
- [Oracle result](probe.txt)
- [Complete fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
