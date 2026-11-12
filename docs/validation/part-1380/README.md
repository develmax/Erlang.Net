# Part 1380 — Schedule arithmetic in direct tail calls

The entry planner uses the same arithmetic argument preparation in direct calls and in case arms. A matched list or tuple clause can compute the argument in X registers and transfer directly to a local or external function. Companion clauses can return a constructed list or tuple and pass a constructed argument to a call. The emitter reuses an input register for a cons tail when the matching clause has no other failure path.

The type proof follows closed local call chains backwards through forwarded arguments and nonnegative increments. A literal seed establishes a lower bound. A public entry, function reference or unproven call edge prevents the fallback proof from attaching a bound. The existing register planner preserves call operands that overlap the arithmetic destination.

The oracle fixture covers four functions with frame-free arithmetic calls, a proper list result from a nil clause, a tagged head, an improper tail, badarith, a floating operand, an argument permutation and a private forwarded seed. asn1ct_check:tag_nums_root2/3 matches the OTP instruction sequence after label normalization. component_index1/4 has the correct count bound and frame-free control flow. It still extracts the tagged field to X5 where OTP reuses X4.

The verified Windows OTP 29.1.1 run generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. Of 58 changed images, 57 narrowed their Code size gaps. array.beam widened by 16 bytes in sparse_mapfoldr_1/8 despite moving entry tests before allocation. The net reduction was 3,320 bytes. Seven functions gained normalized matches and none lost one. All 68 changed peak frames moved closer to OTP and 54 reached the reference size.

The direct arithmetic tail group falls from 403 functions in 208 modules to 396 in 205. The first nonempty-list versus allocation group falls by 36 functions to 3,352 in 571 modules. The largest source-backed list clause group now has a cons argument to a call in 255 functions across 146 modules.

The selected compiler checks, direct whole-module check and earlier focused regressions passed. The layout checker passes for the arithmetic emitter. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [List entry groups](list-entry-prefixes.txt)
- [Arithmetic argument groups](arithmetic-shapes.txt)
- [Fixture source](direct_tail_arithmetic_probe.erl)
- [Focused check](check-direct-tail-arithmetic.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Array instructions](array-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
