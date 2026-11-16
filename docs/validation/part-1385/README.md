# Part 1385 — Reuse stack slots for saved call results

Straight ordinary call sequences now track the last read of an input or call result. A result used after an intervening call gets a Y slot. The planner reuses a dying argument's slot after preparing its current use, emits a swap when the argument and result can exchange X0 and Y, and trims slots after their final use when they can be removed from the frame.

The oracle checks five execution cases. Four functions match OTP instructions after label normalization, covering reuse, overlapping intervals and a result used immediately and again after a call. The permanent beam_saved_call_probe fixture matches the complete deterministic OTP 29.1.1 image. Its previous generated image differs. The constructor case executes with the same values and effects. Its spill currently precedes the heap check and list construction, while OTP retains X0 through construction.

The Windows OTP 29.1.1 comparison generated all 1,366 release images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading. The test and corpus builds use the same compiler assembly.

Fifty-three images changed. Twenty-nine functions gain normalized matches across 27 modules and none lose one. Forty-six of the 47 changed peak frames move closer to OTP and 36 reach its size. The peak frame in dbg_debugged:eval/3 falls from two slots to one while OTP keeps two. Forty-five images narrow their Code size gap, five widen it and three retain it. The net gap reduction is 395 bytes. The largest gap increase is 18 bytes in systools_make.

The variable-head sequence allocation group falls from 1,823 functions in 562 modules to 1,809 in 558. List entry before allocation becomes the largest first-difference group at 3,213 functions across 561 modules. The source-backed list audit finds a variable-head clause with a case body in 247 functions across 139 modules. The terminal-expression audit separately finds 360 case endings across 230 modules in the variable-head sequence group.

The selected compiler cases, direct whole-module check, focused oracle checks and scoped layout check passed. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Sequence endings](sequence-endings.txt)
- [List entry prefixes](list-entry-prefixes.txt)
- [Next list entry instructions](next-list-entry.txt)
- [Fixture source](saved_call_result_probe.erl)
- [Focused oracle check](check-saved-results.ps1)
- [Focused output](probe.txt)
- [Constructor spill instructions](constructor-spill.txt)
- [Permanent fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
