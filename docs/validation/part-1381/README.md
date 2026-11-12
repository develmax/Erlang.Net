# Part 1381 — Preserve cons aliases and prepare nested list arguments

List entry dispatch accepts a one-cell aliased list pattern and keeps the original input register alongside the head and tail. Adjacent clauses can share the list test and extraction. Ordinary tail calls with nested cons arguments reuse the call construction scheduler after the specialized simple forms have been considered.

The emitter reuses a dead tuple head register when its only field extraction feeds a test. The planner checks later clause bodies, guards and patterns before allowing that overwrite. An unused binding in a later clause leaves the register available.

The oracle fixture covers a guarded list alias, alias equality, nested cons construction, an improper list and a later clause that still reads the tuple head after a failed comparison. asn1ct_check:component_index1/4 now matches OTP after label normalization. check_enum_number_root/4 checks the list before allocation, and its plain cons branch calls directly. The branch using gb_sets:insert/2 still needs a closer frame and call schedule.

The verified Windows OTP 29.1.1 run generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. Of 117 changed images, 108 narrowed their Code size gaps, eight widened them and one kept the same gap. The net reduction was 6,029 bytes. string.beam has the largest wider gap at 648 bytes. Sixteen functions gained normalized matches and none lost one. All 155 changed peak frames moved closer to OTP and 92 reached the reference size.

The first nonempty-list versus allocation group falls from 3,352 functions across 571 modules to 3,213 across 561. The source-backed cons-argument subgroup falls from 255 functions in 146 modules to 227 in 135. Variable-head sequences with allocation operand differences are now the largest first-difference group by module count, at 567 modules.

An intermediate candidate moved the heap test after list extraction in sofs:extc_tail/3 and xref_base:reach/3. The final emitter reserves the required heap after the successful list test and before extraction in an unguarded simple branch. The focused fixture asserts that order and the complete comparison preserves both normalized matches.

The selected compiler checks, direct whole-module check and three earlier focused probes passed. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [List entry groups](list-entry-prefixes.txt)
- [Fixture source](cons_argument_probe.erl)
- [Focused check](check-cons-arguments.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
