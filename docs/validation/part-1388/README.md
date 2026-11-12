# Part 1388 — Share singleton and open cons extraction

The entry planner handles three unguarded clauses with singleton, open cons and empty list patterns on one argument. It tests for a cons cell, extracts its head and tail once, and selects the singleton branch by testing the tail. Both arms reuse existing call and construction plans.

Public inputs retain the empty-list check on the non-cons path. For private inputs, the proper-list proof follows direct variable forwarding and leading comprehension bindings. It rejects cycles and unknown callers. A successful proof supplies the typed tail operand and removes the redundant empty-list check.

The focused fixture checks empty, singleton and longer lists, a private wrapper and a comprehension-produced list. Non-list and improper public inputs still raise function_clause. Both fixture functions match the OTP instruction sequence after label normalization. The permanent deterministic fixture matches the entire OTP 29.1.1 image. The previous compiler produced a different image for that fixture.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

Three images changed. asn1ct_check:make_constr_set_vs_1/1 gains a normalized instruction match. Its peak frame and the vertical/1 frames of both pretty printers decrease from three slots to one, matching OTP. All three Code size gaps narrow, for a total of 109 bytes. The pretty printer functions still differ on the nonempty entry proof and typed tail test.

The list-entry first-difference group drops to 3,205 functions in 561 modules. Case bodies after an unguarded variable-head pattern remain in 241 source-backed candidates across 135 modules. The selected compiler cases, direct whole-module check, three earlier oracle probes and scoped layout check passed. The test and corpus builds use the same compiler assembly. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List sequence endings](list-sequence-endings.txt)
- [Next case instructions](next-list-case.txt)
- [Pretty printer instructions](next-vertical.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Fixture source](singleton_cons_probe.erl)
- [Focused oracle](check-singleton-cons.ps1)
- [Focused oracle result](probe.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
- [ASN.1 release instructions](asn1-singleton-cons.txt)
