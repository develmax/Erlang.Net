# Part 1397 — Keep forwarded operands and captures in registers

Straight call sequences now use the shared nested argument planner, so an immediately consumed result can stay in X0 across a nested call. Tail-call closures can capture the prefix result in X0 while passing it to the callee. The closed caller proof follows parameters forwarded at tail calls and recognizes literal aliases in entry patterns. Public inputs, disagreements and recursive proof cycles retain dynamic values.

The OTP oracle verifies nested map operands, X0 captures, literals passed through a wrapper and literals bound by a nested tuple pattern. It also exercises mixed constant and dynamic callers. Four probe functions match OTP frame sizes and normalized instructions, with closure identifiers normalized. The permanent deterministic fixture matches the complete OTP image. The selected compiler cases, whole-module check and prior computed-tail oracle pass with the same compiler assembly used by the corpus.

All 1,366 Windows OTP 29.1.1 release images generate. The strict comparison has 372 Equal, 994 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

The change affects 56 release images. Forty-seven functions gain normalized instruction matches and none lose one. All 57 changed peak frames reach the OTP size. Fifty Code gaps narrow and four widen, with a net reduction of 503 bytes.

The do_share_switch/3 instructions match after label normalization. The do_match_value/5 frame reaches one slot with the literal fail captured. The validate_bs_get_all/8 frame reaches six slots with a literal and X0 in the closure capture list. Closure names and identifiers still differ in those two release functions. The largest frame group covers 1,743 functions in 554 modules. The immediate local-tail subgroup covers 84 functions in 29 modules. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Release instructions before](operands-before.txt)
- [Release instructions after](operands-after.txt)
- [Normalized functions](normalized-functions.txt)
- [Peak frames](peak-slots.txt)
- [Code size gaps](code-delta.json)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Continuation ranking](case-continuations.txt)
- [Focused oracle](check-forwarded-tail.ps1)
- [Oracle result](probe.txt)
- [Complete fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
