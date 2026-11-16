# Part 1391 — Retain case-arm results for tuple returns

The case sequence planner tracks the last reads of input bindings and intermediate call results. It reuses a selector frame slot when an earlier result must survive a later call. If an ordinary call reads a dying argument from the selected slot, the existing saved-call emitter stages the read and uses a swap when appropriate. The tuple builder reads both the retained result and the final result in X0.

The focused oracle exercises two tuple returns, case fallback, empty input and function_clause. The pair function matches the OTP instruction sequence after label normalization. The permanent deterministic fixture matches the complete OTP 29.1.1 image and differs from the previous C# output. In the preserve function, the result used by a constructed argument and by the return tuple has the same value and behavior. Its construction and spill order still differs from OTP.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

Eleven release images changed. Two functions gain normalized instruction matches and none lose a match. Six peak frames reach the OTP size and two move closer. Two inets functions had the OTP size before this change and now use one slot instead of two. Ten Code size gaps narrow and one widens by four bytes. The net reduction is 261 bytes.

The largest list-entry first-difference group still covers 3,204 functions across 561 modules. The selected compiler cases, direct whole-module check, three earlier oracle probes and scoped layout check passed. The test and corpus builds use the same compiler assembly. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List case branch audit](list-case-branches.txt)
- [Release tuple sequences](release-tuple-sequences.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](case_tuple_sequence_probe.erl)
- [Focused oracle](check-case-tuple-sequence.ps1)
- [Focused oracle result](probe.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
- [Construction and spill instructions](retained-constructor-result.txt)
