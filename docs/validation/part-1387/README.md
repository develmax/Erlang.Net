# Part 1387 — Plan call sequences after list entry

The list entry planner now passes extracted bindings to the existing straight and saved call planners. When the tail is used only after the first call, the eligible branch allocates its frame after the list test and extracts the tail directly into its saved slot. The head goes into the original X register when the first call is its final use, or into a saved slot when later calls still need it.

The focused oracle checks both head lifetimes, forwarding a result in X0, preserving an earlier call result, the empty-list clause and non-list failure. Both functions match OTP after label normalization. The permanent deterministic fixture matches the entire OTP 29.1.1 image. The previous compiler produces a different image for the same fixture.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

Thirty-seven images changed. Eight functions gain normalized instruction matches across seven modules and none lose a match. All twenty changed peak frames reach the OTP size. Twenty-two images narrow their Code size gap, four widen it by up to three bytes and eleven retain it. The net gap reduction is 207 bytes.

The list-entry first-difference group still covers 3,206 functions across 561 modules. A source-backed audit finds 103 variable-head sequences ending in a call across 68 modules and 70 ending in a case across 51 modules. The next examples cover singleton and open cons dispatch, constructor arguments and branch continuations.

The selected compiler cases, direct whole-module check, three earlier oracle probes and scoped layout check passed. The test and corpus builds use the same compiler assembly. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List sequence endings](list-sequence-endings.txt)
- [Next list continuations](next-list-continuations.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](list_call_sequence_probe.erl)
- [Focused oracle](check-list-call-sequence.ps1)
- [Focused oracle result](probe.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
