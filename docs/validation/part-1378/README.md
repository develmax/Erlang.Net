# Part 1378 — Compare list case selectors before branch frames

After a list clause matches, a two-arm case can compare an input register or a checked record field with a bound variable or literal. The comparison runs before either arm allocates a frame. A direct return or an ordinary tail call can run without a frame. An arm with a call saves only the values it needs. A tuple built immediately from that call result can read it directly from X0.

The oracle fixture checks record access, recursive misses, an empty list, an improper tail and a malformed record. The malformed record raises badrecord at the same line as OTP. The selected-record arm uses one slot where the previous compiler used five, and the bound-variable case uses zero where it previously used three. Both frame sizes match OTP.

In asn1ct_check:leading_attr_index1/5 the record check, field extraction and comparison move before allocation. Peak frame use drops from twelve slots to five. OTP uses three in the matching arm and none in the arithmetic tail arm. The C# compiler still allocates a frame for the arithmetic tail and checks two input tuple shapes that OTP already knows.

The complete Windows OTP 29.1.1 run generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. Nine images changed. Three narrowed their Code size gaps and six widened by 18 bytes each, for a net increase of 42 bytes. The normalized match count is unchanged. All three changed peak frames moved closer to OTP and one reached the OTP size.

All six larger images share do_enc_OCTET_STRING/5. Its comparison now runs before allocation as in OTP, but the generated arms each allocate six slots. OTP computes the recursive call operand in registers and saves at most the list head. The exact-equality versus allocation group falls by seven functions, from 1,430 to 1,423 in 484 modules. The first-nonempty-list group is unchanged.

The selected compiler checks, direct whole-module check and four earlier probes passed. The layout checker passes for the new emitter file. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [List entry groups](list-entry-prefixes.txt)
- [Fixture source](list_register_case_probe.erl)
- [Focused check](check-list-register-case.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Megaco arm comparison](megaco-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
