# Part 1255 — Emit atom case tails in guarded clauses

Apply terminal atom dispatch after clause patterns and guards have succeeded. Permit fresh branch-local bindings before a local tail call and prepare their values with the existing sequence emitter. Branches with already bound match patterns retain the general path.

In `asn1ct_check`, the guarded enumeration branches now use recursive `call_last`. The instruction counts for `check_enum_number_root/4` and `check_enum_ext/5` fall from 82 to 75 and from 118 to 111, respectively. OTP emits 62 and 87 instructions and uses different frames for different paths. That frame placement remains open.

The part 1254 baseline contains 4,743 functions across 762 images where OTP emits allocations of different sizes and the generated function emits a single allocation. This instruction-level ranking identifies candidates for path-specific frames. It does not imply that one frame transformation covers them all.

The full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. All 132 changed images moved closer to the release Code section length, by 3,205 bytes in aggregate. OTP accepted all 1,007 differing image pairs.

- [Probe source](guarded_atom_tail_probe.erl)
- [Probe command](check-guarded-atom-tails.ps1)
- [Behavior and error cases](probe.txt)
- [Computed tail regression](computed-probe.txt)
- [Identity fold regression](identity-probe.txt)
- [Enumeration instruction comparison](enumeration-instructions.txt)
- [Split frame baseline ranking](split-frame-ranking.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Updated atom tail ranking](atom-tail-ranking-summary.txt)
- [OTP loader](loader.txt)
