# Part 1379 — Prepare list case arithmetic in call registers

A list case arm can evaluate one arithmetic call argument using entry registers and an ordinary call plan. The planner avoids overwriting an input needed for another argument or for the list produced after the call. When safe moves lower the live X register count, it prepares those arguments before arithmetic. A tail arm emits a frame-free call. An arm that constructs a list saves only the variable heads that survive the call. A successful literal test can supply a constant head.

The entry planner reuses the input register for the cons tail when a two-clause case can only fall through on nil. It recognizes an empty list result directly. Arithmetic in included source uses the mapped call location, and private call analysis can supply an integer lower bound.

The focused fixture covers two list construction branches, a frame-free tail call, floating arithmetic, badarith and register moves when the arithmetic destination overlaps a live argument. Each fixture frame size matches OTP. A test for arithmetic on an extracted head exposed an out-of-range argument index in the first build. The verified planner tracks the original function arity separately from extracted registers and only applies caller bounds to original parameter bindings. The release instruction comparison shows do_enc_OCTET_STRING/5 matching OTP after label normalization. In asn1ct_check:leading_attr_index1/5 the recursive arithmetic arm now has a frame-free call. The matching arm still saves five slots where OTP saves three.

The verified Windows OTP 29.1.1 run generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. Of 200 changed images, 44 narrowed their Code size gaps, 31 widened them and 125 kept the same gap. The net reduction was 993 bytes. Six Megaco functions gained normalized matches and no function lost a match. Each of their images narrowed its Code size gap by 64 bytes. All 52 changed peak frames moved closer to OTP and eight reached the reference size.

The first nonempty-list versus allocation group falls from 3,417 to 3,388 functions across 571 modules. The largest source-backed arithmetic argument group is direct tail calls, with 403 differing functions in 208 modules. The case-to-list group falls from 18 functions in 17 modules to 12 functions in 11 modules. This audit measures source shape and normalized instruction differences. Each function can contribute to several groups.

The selected compiler checks, direct whole-module check and three prior focused probes passed. The layout checker passes for the new emitter file. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [List entry groups](list-entry-prefixes.txt)
- [Arithmetic argument audit script](rank-arithmetic-arguments.escript)
- [Arithmetic argument groups before](arithmetic-shapes-before.txt)
- [Arithmetic argument groups after](arithmetic-shapes-after.txt)
- [Fixture source](list_case_arithmetic_probe.erl)
- [Focused check](check-list-case-arithmetic.ps1)
- [Focused output](probe.txt)
- [Megaco instructions](megaco-functions.txt)
- [ASN.1 compiler instructions](release-functions.txt)
- [Direct arithmetic call candidates](next-direct-arithmetic.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
