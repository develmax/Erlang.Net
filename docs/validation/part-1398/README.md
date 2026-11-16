# Part 1398 — Evaluate size and arithmetic operands in registers

The entry planner can keep the prefix call result in X0 while computing a tail-call argument with size or integer operations. It reuses temporary registers, forwards the original result to the tail call and saves only inputs used after the prefix. The operand types carry known integer bounds and the binary alignment from iolist_to_binary or list_to_binary.

The OTP oracle checks byte and bit sizes, padding, two live arithmetic temporaries, badarg and a same-named external call that raises undef. The size and padding functions match OTP frame sizes and normalized instructions. The deterministic fixture matches the complete OTP image, including the binary unit in the Type chunk. The selected compiler cases, whole-module check and preceding forwarded-tail oracle pass with the corpus compiler assembly.

All 1,366 Windows OTP 29.1.1 release images generate. There are 372 Equal, 994 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

The change affects 11 release images. Eight functions gain normalized instruction matches and none lose one. All nine changed peak frames reach OTP. The hash_xof/3 frame falls from three slots to two, and pack/4 falls from six to four. Both match the OTP instruction sequence after normalizing labels. Seven Code gaps narrow by a net 118 bytes, with none wider.

The largest frame group covers 1,740 functions in 553 modules. Its immediate local-tail subgroup covers 82 functions in 27 modules. Remaining examples include fname/2 with an append expression and int_get_user/2 with a dynamic module. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Release instructions before](operands-before.txt)
- [Release instructions after](operands-after.txt)
- [Normalized functions](normalized-functions.txt)
- [Peak frames](peak-slots.txt)
- [Code size gaps](code-delta.json)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Continuation ranking](case-continuations.txt)
- [Focused oracle](check-bif-tail.ps1)
- [Oracle result](probe.txt)
- [Complete fixture hashes](fixture-hashes.txt)
- [Type chunk bytes](type-chunk.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
