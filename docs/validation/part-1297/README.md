# Part 1297 — Trim dead slots before a discarded dynamic call

When a forwarded dynamic call is followed only by a returned literal, prepare the call registers and trim the entire frame before apply. Return with the remaining zero-slot frame.

The focused probe checks trim and deallocate, plus a fallback that keeps a live binding. The glu:lookAt/9 instructions now include the release trim. Argument moves and the unreachable guard failure epilogue remain different.

- [Focused execution](probe.txt)
- [Focused command](check-dynamic-trim.ps1)
- [glu wrapper comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal and 1,006 Different. OTP accepted all differing pairs. Only glu changed, and its Code grew by 39 bytes as the release trim operations were introduced. The extra failure epilogues are addressed in part 1298.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
