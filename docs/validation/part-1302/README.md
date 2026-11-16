# Part 1302 — Check tuple arity and tuple-or-binary guards before allocation

Reuse the tuple arity test for an exact tuple_size comparison on an entry argument. Emit short-circuit tests for tuple-or-binary alternatives on the same argument.

The focused test checks valid and invalid inputs for both guards. glu:pickMatrix/5 and glu:scaleImage/9 match after label normalization, raising the module total to 26 of 30.

- [Focused execution](probe.txt)
- [Focused command](check-tuple-guards.ps1)
- [glu comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal, 1,006 Different and zero source rejections. OTP accepted all differing pairs. Only glu changed. Its absolute Code size gap increased by 98 bytes while two more functions matched after label normalization.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
