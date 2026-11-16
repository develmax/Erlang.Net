# Part 1375 — Keep returned tuple fields in registers during arithmetic

For a local call with a proven result tuple arity, the compiler retains the returned tuple in X0. It extracts operands needed for arithmetic first and keeps the remaining fields in the tuple across the garbage-collecting BIF. Only entry arguments used after the call occupy Y slots. The shared pair fact analysis proves when the second tuple field is a positive integer and records that bound in the arithmetic instruction. Other calls retain their existing runtime tuple checks.

The oracle fixture checks integer and float addition, reusing the computed field in the returned tuple, guard failure, badarith and an unproven return shape that raises badmatch. The instruction sequence for add/2 matches OTP. The frame falls from four slots to one. The release instruction comparison confirms the same change in AttributeCertificateVersion1-2009:enc_obj_id_tail/2.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. All 24 changed images narrowed their Code size gaps by a combined 388 bytes. The normalized comparison gained 24 matches and lost none. All 25 changed peak frames reach OTP's size.

The sequence audit now counts 1,847 variable-head sequences with allocation operand differences across 568 modules, down from 1,870 across 577. It classifies first expressions using OTP abstract forms. Assignments from direct calls account for 641 functions across 250 modules, and assignments from explicit external calls account for 342 functions across 237. In asn1ct:common_imports/2 the first result only feeds a call inside the returned tuple. OTP forwards it in X0 and the C# compiler currently allocates a slot for it. The list-test-before-allocation group remains at 574 modules.

Four earlier probes, the selected Docs and release image tests and the direct whole-module check passed. A formatting-only rebuild has identical compiler method bodies and passes the focused oracle. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Sequence prefix audit](rank-sequence-prefixes.escript)
- [Sequence prefixes before](sequence-prefixes-before.txt)
- [Sequence prefixes after](sequence-prefixes-after.txt)
- [Fixture source](tuple_arithmetic_probe.erl)
- [Focused check](check-tuple-arithmetic.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Layout rebuild comparison](layout-il.txt)
