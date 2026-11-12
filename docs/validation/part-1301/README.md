# Part 1301 — Select between two integer guard alternatives at entry

For an exact-equality disjunction against two different integers on the same parameter, emit select_val before allocation. Both values reach the same success label.

The focused test checks both accepted integers and rejects an equal-valued float. glu:quadricTexture/2 now matches after label normalization, raising the module total to 24 of 30.

- [Focused execution](probe.txt)
- [Focused command](check-integer-guard.ps1)
- [glu wrapper comparison](glu-wrappers.txt)

The full corpus generated all 1,366 images with 360 Equal, 1,006 Different and zero rejected sources. OTP accepted all differing pairs. Only glu changed, and its absolute Code size gap increased by 49 bytes despite the new normalized function match.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
