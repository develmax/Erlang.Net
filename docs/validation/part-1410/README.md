# Part 1410 — Prepare closures across calls

The saved-call planner now schedules an anonymous closure before the subsequent argument calls. If the closure survives a call, the compiler writes it directly to a stack slot and keeps the preceding result in X0. If the current call consumes it, the compiler can prepare it in X registers and release slots that only held its captures. Tuple returns and tail-call continuations use the same argument preparation.

The deterministic fixture covers a closure before a nested call, a closure that captures the preceding result, a closure passed directly, a tail call and reuse of a capture slot. Its complete image matches OTP and both images return the same values.

The release function mod_auth_plain:list_groups/1 now matches the normalized OTP instructions. ct_release_test:fetch_all_apps/1 reaches the OTP frame size of two slots and its body still differs in the indices and uniqueness values of its closures.

The intermediate corpus exposed one lost normalized match in xref. The compiler now retains the captured closure failure stub, can replace a capture at its last use, and combines the allocation check with the subsequent argument constructor. The final xref:get_xref_ignorelist/1 matches the normalized OTP instructions, and the failure stub retains its prior match.

The final Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal. OTP prepared all 986 differing images for loading. The change modifies 47 images. Normalized instructions newly match in 24 functions across 20 modules with no previous match lost. The total Code size gap shrinks by 905 bytes. Peak frame sizes reach the reference in 45 functions. Two lifted functions in dbg_wx_filedialog_win and observer_trace_wx use one additional stack slot.

The source audit found 573 functions across 237 modules with a differing entry frame and a tuple match on the initial call result. The subset that immediately makes a tail call covers 49 functions across 40 modules. These are candidate shapes for instruction comparisons.

All three focused compiler cases passed using the corpus compiler assembly. Full runtime verification for this assembly, canonical readiness regeneration and publication are pending.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Instructions before](operands-before.txt), [after](operands-after.txt) and [xref failure stub](captured-failure.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Frame delta](frame-delta.txt), [frame audit](frame-audit.txt) and [source audit](source-audit.txt)
- [Oracle](check-call-closures.ps1), [result](probe.txt), [hashes](fixture-hashes.txt) and [compiler tests](focused.json)
