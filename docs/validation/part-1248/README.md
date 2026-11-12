# Part 1248 — Match caught singleton-list dispatch

Keep the catch token in one Y register and inspect the returned list in X registers. Test the tuple tag and the empty tail before extracting the payload and constructing the result. Each arm returns directly.

In `snmpa_error`, `report_module/0` now matches the OTP instructions after label normalization. The remaining function mismatch is `report_err/3`. The release uses a four-slot frame, keeps the dynamic module in an X register and tests the inferred tuple type.

The probe runs the original and generated images in OTP. It checks the absent table, empty lookup, wrong tuple arity, valid singleton dispatch and multiple rows.

The corpus produced 1,366 images with 358 Equal, 1,008 Different and zero rejected sources. Only `snmpa_error.beam` changed. Its generated Code section fell from 274 to 251 bytes, the release size. OTP loaded all 1,008 differing pairs.

- [Execution probe](check-caught-tagged-singleton.ps1)
- [Probe result](probe.txt)
- [Normalized function comparison](snmpa-error.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
