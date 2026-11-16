# Part 1416 — Preserve live tuple fields across calls

The saved-call planner now allocates stack slots for tuple fields that remain live across an intervening call. It keeps fields used only by the next call in X registers, and reuses the normal slot expiry and frame trimming logic. It schedules the projections while X0 still contains the checked tuple. A field used before and after the call is written once to the stack and can be loaded as an argument.

The deterministic fixture matches OTP byte for byte. It covers tagged and untagged tuples, a known local result shape, reordered fields, a field read twice, successive checked results, and badmatch on the second result. The preceding compiler differs on six of the seven fixture functions. Three focused tests pass with the corpus compiler assembly.

All 1,366 Windows OTP 29.1.1 release files were generated. There are 380 Equal, including ELDAPv3 from C#-generated source, and 986 Different. No source was rejected. OTP prepared all 986 differing files for loading. Compared with Part 1415, 42 release files changed. In all 70 changed functions, the maximum frame size moves toward OTP. It reaches the OTP size in 46. Fourteen functions in twelve modules gain normalized instruction equality with no losses. The combined Code size gap shrinks by 2,834 bytes. ct_master and file_sorter each increase their Code gap by one byte.

The next shared change is to consider saving the entire tuple when several fields have a later first use. OTP does this in the remaining probe and reduces the frame from three slots to two. Full runtime verification, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [status changes](status-changes.txt) and [section size counts](chunk-sizes.txt)
- [Summary](summary.txt), [frame delta](frame-delta.txt), [normalized instructions](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-tuple-spill.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
- [Deferred projection and default version attribute](deferred-projection.md)
