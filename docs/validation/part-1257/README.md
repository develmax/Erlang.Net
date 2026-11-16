# Part 1257 — Reuse the cons extraction across guarded entry clauses

For consecutive cons clauses followed by a nil clause, branch to the nil check before overwriting the input register. On the cons path, extract the head once into a scratch register and place the tail in the original argument register. A failed guard can then enter the next cons clause without another shape check or extraction. The last cons clause must be unguarded and the other argument patterns must be variables.

The instructions of `test_server_ctrl:get_latest_dir/2` match the Windows OTP 29.1.1 release after label normalization. The probe covers a successful guard, guard fallthrough, the empty input and an improper tail.

The full run produced 359 Equal and 1,007 Different images with zero source rejections. Five images changed, with their Code lengths moving 81 bytes closer to OTP in aggregate. The cons-register ranking identifies 478 baseline images with a similar instruction symptom, including generated parser functions.

- [Probe source](shared_cons_probe.erl)
- [Probe command](check-shared-cons.ps1)
- [Execution result](probe.txt)
- [Split frame regression](split-frame-probe.txt)
- [Cons register ranking](cons-ranking.txt)
- [Release function comparison](get-latest-dir.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
