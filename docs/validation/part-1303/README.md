# Part 1303 — Reuse input slots for two Unicode conversions before apply

For a dynamic module producer followed by two NUL-terminated Unicode conversions, reuse the two saved input slots for the module and first conversion. Keep the second conversion in X0 until argument setup, then trim the frame before apply and the constant-argument tail call.

The focused probe checks UTF-8 conversion, the NUL suffix, argument order and guard rejection. glu:checkExtension/2 matches OTP after label normalization. The remaining three functions differ in compound guard lowering.

- [Focused execution](probe.txt)
- [Focused command](check-unicode-pair.ps1)
- [glu comparison](glu-wrappers.txt)
