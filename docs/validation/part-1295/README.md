# Part 1295 — Extract the first call argument directly into X0

For a planned tuple entry followed by a unary static call and a returned tuple, extract the call argument directly into X0 when the returned tuple does not read that field. Keep the other fields in their existing slots.

The focused probe matches the OTP frame size of one slot and checks successful and failing inputs. The glu baseline exposes a wider case for the next iteration: most differing wrappers save the dynamic module result to the stack before apply, while OTP keeps it in X registers.

- [Focused execution](probe.txt)
- [Focused command](check-tuple-entry.ps1)
- [glu function baseline](glu-baseline.txt)
- [Representative wrapper instructions](glu-wrapper-baseline.txt)

The full corpus generated all 1,366 images with 360 Equal and 1,006 Different. Release image bytes remained unchanged. OTP accepted all differing pairs.

- [Full corpus](otp-corpus.json)
- [OTP loader](loader.txt)
