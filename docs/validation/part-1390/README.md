# Part 1390 — Forward live values through case-arm calls

The register case planner accepts straight call sequences ending in a tail call. A branch reads its initial bindings from the selector frame and can forward the current result in X0. The existing constructor planner prepares compound call arguments. Last-use tracking clears dead slots and trims the frame before a subsequent call when the dead slots form its leading segment.

For an eligible list entry, the selector frame receives the head and tail directly. The branch reserves case and empty-list labels in their OTP emission order. The focused oracle checks both branches of the list case, a constructed tail argument, a bound selector result, the empty list and function_clause. The list function matches normalized OTP instructions. The permanent deterministic fixture matches its complete image and differs from the previous compiler's output.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

Twenty-three release images changed. Ten functions gain normalized matches and eighteen peak frames reach the OTP size. Twenty Code size gaps narrow and three widen. The net reduction is 622 bytes. The largest increase is seven bytes in crashdump_viewer. The largest list-entry first-difference group still covers 3,204 functions across 561 modules.

The selected compiler cases, direct whole-module check, three earlier oracle probes and scoped layout check passed. The test and corpus builds use the same compiler assembly. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List case branch audit](list-case-branches.txt)
- [Release case sequences](release-case-sequences.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](case_call_sequence_probe.erl)
- [Focused oracle](check-case-call-sequence.ps1)
- [Focused oracle result](probe.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
