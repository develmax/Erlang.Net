# Part 1291 — Reverse the two stack slots for consecutive case bindings

Apply the OTP frame-local Y register numbering to the two-slot sequence of consecutive call and case bindings. OTP performs this inversion in turn_yregs_1/3 in beam_ssa_pre_codegen.erl.

All 1,068 functions in the standalone gl build now match after normalizing label numbers. The Code section has the same length as OTP and differs at 19 bytes. The raw disassembly isolates the different operands to label numbering in init_nif/0 and rec/1. Attr also differs in the 16-byte module digest. The file hashes remain different.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Normalized instruction comparison](gl-init.txt)
- [Raw label operands](gl-raw-operands.txt)
- [Section comparison](gl-chunks.txt)
- [Byte differences](gl-byte-deltas.txt)
- [File hashes](gl-hashes.txt)
