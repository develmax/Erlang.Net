# Part 1583 — Emit guarded dynamic binary splits without a frame

The compiler keeps the original binary in its input register until the split succeeds. It overwrites the size register with the prefix and the source register with the tail, then builds the tuple from X registers. A failed extraction still reports the original binary. The implementation reuses entry guards, typed register encoding and tuple emission. The reference is OTP 29.1.1 beam_ssa_codegen.erl.

The 668-byte deterministic regression image matches OTP for both argument orders. OTP VM execution agreed on five cases, including a negative size and a too-short input. Three focused tests passed. [Split](split.txt) · [Swapped](swapped.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and generated. The full comparison found 390 Equal and 976 Different, including an exact ELDAPv3 from C# ASN.1 generation. This rule changed zero release images. OTP accepted all differing images for loading. The audit found 297 reference functions in 51 modules with no_fail, bs_get_binary2 and bs_get_tail. These functions include more general control flow than the new rule handles. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt) · [Inventory](split-inventory.txt).
