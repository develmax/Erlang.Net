# Part 1249 — Match the snmpa_error release image

Infer the return shape of an eligible local selector from its tagged singleton-list branch and literal fallback. If that shape proves the caller's tag, emit a typed tuple test. Keep the extracted dynamic call target in an X register, clear the saved argument slots before apply, and return separately from both case arms.

The separately rebuilt Windows OTP 29.1.1 `snmpa_error.beam` matches the release SHA-256. The execution probe from part 1248 passes against both images.

The full corpus generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Only `snmpa_error.beam` changed. OTP loaded all 1,007 differing pairs.

The part 1248 baseline has 726 differing images with a function that allocates more slots than OTP and clears slots before saving arguments where OTP saves an argument immediately. In `asn1ct:check/2` the generated frame has nine slots and the release has two. This count groups instruction symptoms across source shapes.

- [Whole-image SHA-256](snmpa-error-hashes.txt)
- [Execution probe result](probe.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Entry-frame baseline ranking](entry-frame-ranking.txt)
