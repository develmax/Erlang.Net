# Part 1259 — Use proven cons entry shapes in the general emitter

Reuse the private cons argument proof in single and shared entry shape tests. Skip only the first cons check and keep checks on subsequent cells. For a single unguarded cons clause with a simple tail call, put the tail in the original argument register and call the target without a frame.

The normalized instructions of `test_server:asan_make_opts/1` match Windows OTP 29.1.1. In `dbg_ieval:eval_zip/5`, the redundant shape test is gone. The nested tuple test and the frame still differ. The probe checks deeper list shape failures, shared clause failures, and an improper cons passed to a direct tail.

All 1,366 modules generated successfully, with 359 Equal, 1,007 Different and no rejected sources. All 30 changed images moved closer to the OTP Code length by 173 bytes in aggregate. OTP accepted all 1,007 differing pairs.

In the part 1258 baseline, 129 modules contain 343 functions where OTP tests a tagged list head before its first frame, and the generated function has a tagged tuple test but no matching pair before its first frame. This instruction-level ranking identifies a group for the next entry-pattern change.

- [Probe source](general_cons_probe.erl)
- [Probe command](check-general-cons.ps1)
- [Execution result](probe.txt)
- [Release instruction comparison](release-entry.txt)
- [Tagged list head ranking](tuple-head-ranking.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
