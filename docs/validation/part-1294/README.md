# Part 1294 — Keep a tuple entry call result in X0

Reuse the bound-call tuple emitter after planned entry tuple checks. A call result that is immediately returned in a tuple can stay in X0.

The focused probe verifies correct matching and failure. The frame shrinks from three slots to two, while OTP uses one. The remaining slot holds an entry field that only feeds the first call.

- [Focused execution](probe.txt)
- [Before and after instructions](tuple-frames.txt)

The full corpus generated all 1,366 images with 360 Equal and 1,006 Different. Release image bytes remained unchanged. OTP accepted all differing pairs.

- [Full corpus](otp-corpus.json)
- [OTP loader](loader.txt)
