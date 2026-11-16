# Part 1306 — Place frames after tagged tuple clause checks

Extend the existing per-clause frame emitter to tagged tuple arguments with distinct variable fields. Check the tag and arity in X registers, extract the fields, and evaluate the guard before entering the branch frame. Preserve the original arguments across failed tests for subsequent clauses. Save only tuple-result operands that survive the call.

The focused case checks the complete instruction sequence against OTP 29.1.1 and executes matching and fallback paths. The successful branch now uses one Y slot instead of two. The fallback returns the original term after wrong arity, wrong tag, non-tuple input or a failed guard.

- [Focused command](check-entry-tagged-frame.ps1)
- [Instructions before the change](before.txt)
- [Matching OTP and C# instructions](probe.txt)
- [Opcode sequences for six changed release modules](release-samples.txt)

The release sample reports whether each changed function matches the OTP opcode sequence after and before the change, followed by the two instruction counts. The opcode comparison omits operands, labels and source positions. The full corpus run uses the pinned documentation overlay and saves generated differences.

All 1,366 release modules emitted BEAM images. The corpus has 361 Equal, 1,005 Different and zero rejected sources. Fourteen images changed. The absolute Code size gap decreased in twelve modules and stayed unchanged in two. OTP accepted all differing images. Full runtime tests, readiness regeneration and CI were outside this check.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [OTP loader](loader.txt)
