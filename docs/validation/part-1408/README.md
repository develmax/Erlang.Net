# Part 1408 — Keep call results live through tuple construction

The saved-call planner now handles tuple returns. It extends the lifetime of arguments and intermediate results that the tuple or its closure needs and retains the last call result in X0. Calls still execute in source order. A returned closure and its tuple share a heap check. Lifted closures that simply return a constant or an argument can use the direct return emitter.

The first corpus run exposed two previously matching bodies. A constant tuple was constructed at runtime instead of loaded as a literal. The final call cleared a dead slot instead of trimming it when the tuple still needed another slot. The final plan retains literal folding and trims eligible slots before the final call.

The deterministic OTP fixture compares the complete image and executes plain and retained call results, an uncaptured closure and a closure that captures the final call result.

The final Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal. OTP prepared all 986 differing images for loading. The change modifies 96 images. Normalized instructions newly match in 187 functions across 72 modules and no previous match is lost. The total Code size gap shrinks by 1,757 bytes. Peak frame sizes reach the reference in 154 functions. The single apparent increase in frame distance compares differently numbered closures at different source lines in merl_transform.

Two selected compiler cases passed. The whole-module case exceeded the 15-second harness limit on both attempts. The same test body completed successfully in 17.3 seconds when invoked directly from the same test assembly. The corpus and test assemblies used the same compiler hash. The older full test run reported failures and supplies no readiness evidence for this compiler assembly.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Previously matching bodies](preserved-instructions.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Frame delta](frame-delta.txt) and [audit](frame-audit.txt)
- [Oracle](check-call-sequence-tuple.ps1), [result](probe.txt), [hashes](fixture-hashes.txt) and [compiler tests](focused.json), [first harness run](focused-concurrent.json) and [direct test result](whole-module-direct.txt)
