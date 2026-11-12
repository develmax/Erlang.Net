# Part 1389 — Keep list guard tests before case frames

Guarded and unguarded cons clauses can share one list test and extraction. The compiler evaluates negated type checks in the extracted registers and keeps the guarded tail call frame-free. The selector call in the other branch uses the existing register case plan and allocates the frame for that branch only.

Singleton dispatch also accepts the existing call-based case plans. It resolves the empty-list and longer-list targets after emitting preceding case blocks, which preserves OTP label numbering. The focused oracle exercises both dispatch paths, proper and improper lists, and local boolean selectors. Both fixture functions match OTP after label normalization. The permanent deterministic fixture uses a standard library boolean selector and matches its full OTP image.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

One release image changed. asn1ct_pretty_format:complex_list/1 gains a normalized instruction match and its peak frame drops from five slots to one, matching OTP. The image's Code size gap narrows by 62 bytes. The list-entry first-difference group falls to 3,204 functions across 561 modules. Multi-call case branches in asn1ct_imm:enc_pre_cg_1/3 still use the previous emitter.

The selected compiler cases, direct whole-module check, three earlier oracle probes and scoped layout check passed. The test and corpus builds use the same compiler assembly. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List case branch audit](list-case-branches.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](list_branch_probe.erl)
- [Focused oracle](check-list-branch.ps1)
- [Focused oracle result](probe.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
- [ASN.1 release instructions](asn1-list-branches.txt)
