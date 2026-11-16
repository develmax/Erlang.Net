# Part 1298 — Drop unreachable failure epilogues after complete entry guards

For a single clause with unconditional parameter patterns, omit the framed failure epilogue when the entry guard has already been emitted in full. Its failure edges point directly to function_info before allocation.

The focused probe verifies the guard target, absence of the redundant jump and function_clause on invalid input. The standalone glu build increases its normalized instruction matches from two to ten of 30 functions.

- [Focused execution](probe.txt)
- [Focused command](check-guard-frame.ps1)
- [glu wrapper comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal, 1,006 Different and zero rejected sources. OTP accepted all differing pairs. All 243 changed images moved closer in Code size, by 10,116 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
