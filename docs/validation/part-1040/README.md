# Part 1040 orphan clause labels

Part 1038 removed unreachable function-clause epilogues but still emitted their final labels. Removing only the orphan label from the failing probe restored preparation. The general emitter now allocates and marks a failure label only when the final clause can fail selection or another clause follows. Body-match failure paths keep their own labels.

The probe generated with Part 1039 returned badfile in OTP 29.1.1. The fixed compiler generated a module that loaded and returned the expected tuple in OTP. To repeat, compile loader_probe_1.erl with Erlang.Tool compile-beam and run the resulting file with erl -noshell -eval using the probe script and -extra path/to/loader_probe_1.beam.

| Differing images prepared by OTP | Accepted | badfile |
| --- | ---: | ---: |
| Part 1037, before the regression | 1016 | 5 |
| Part 1039 | 43 | 978 |
| Part 1040 | 1016 | 5 |

All corresponding reference images prepared successfully. Each remaining badfile image contains a failing function whose frame exceeds the OTP 1024-register limit. The full corpus admitted and generated 1363 of 1366 images. All per-file comparison statuses remained unchanged at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Oversized frames](large-frames.txt)
- [Regression probe result](probe-result.txt)
- [Regression probe script](run-loader-probe.eval)
