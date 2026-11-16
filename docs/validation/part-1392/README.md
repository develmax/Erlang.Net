# Part 1392 — Construct call arguments before reusing a stack slot

Both saved-call planners can use the slot of a dying constructor operand to preserve an earlier result. The constructor reads the old Y value before X0 is stored in its slot. X0 stays live through the heap check and the constructor sequence. A value used by a later direct stack load still blocks reuse.

The focused OTP oracle loads and executes the previous, current and reference images. Both case functions match the normalized OTP instructions in the current image. The permanent deterministic fixture matches the complete OTP 29.1.1 image and differs from the previous C# image.

The full Windows corpus generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 stays Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

Four release images change. Six functions in two modules gain normalized instruction matches and none lose one. Seven functions across three modules reach the OTP peak frame size. All four Code size gaps narrow by a total of 51 bytes. Five of the six newly matching functions are in httpc_manager. Its complete image still differs in Code, atoms, imports and metadata.

The largest list-entry first-difference group still covers 3,204 functions across 561 modules. Two selected compiler cases, the whole-module check, two earlier oracle probes and the compiler source layout check pass. The test and corpus builds use the same compiler assembly. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Normalized functions](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List case branch audit](list-case-branches.txt)
- [Focused oracle](check-case-tuple-sequence.ps1)
- [Focused oracle result](probe.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
