# Part 1409 — Compile calls in returned tuple fields

The saved-call planner now extracts ordinary calls from fields of a returned tuple and from nested tuple and list constructors. It evaluates the extracted calls in source order and plans their result lifetimes together with preceding calls. It reuses the register constructor to build the result after the last call. A final argument swap that reuses a saved slot runs before loading independent constants.

The deterministic fixture covers two field calls that share a preceding result, nested calls inside tuple and list constructors, and two process dictionary writes. The fixture matches the complete OTP image. Running both images confirms the values and the order of the dictionary writes.

The final Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal. OTP prepared all 986 differing images for loading. The change modifies 74 images. Normalized instructions newly match in 128 functions across 60 modules and no previous match is lost. The total Code size gap shrinks by 1,878 bytes. Peak frame sizes reach the reference in 141 functions.

The initial fixture reproduced two swaps emitted after loading a constant. The final variant moves the swap first and matches all three fixture functions byte for byte. The refreshed audit leaves 121 functions across 85 modules in the bound remote call and tuple return group. The two motivating release examples still need comprehension or closure argument preparation.

All three focused compiler cases passed using the same compiler assembly as the corpus runner. Full runtime verification for this assembly, canonical readiness regeneration and publication are pending.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Frame delta](frame-delta.txt) and [audit](frame-audit.txt)
- [Oracle](check-tuple-field-calls.ps1), [result](probe.txt), [hashes](fixture-hashes.txt) and [compiler tests](focused.json)
