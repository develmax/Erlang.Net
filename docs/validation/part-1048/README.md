# Part 1048 register-only type guards

The general direct tail-call emitter now tests supported unary type guards in incoming X registers after head matching and before argument moves. The ordinary entry path and the frame-free path share the guard reader. More complex guards retain their existing paths.

The OTP probe verifies literal and type guard selection, guard fallthrough, catch-all selection and the original argument in a function_clause stack frame. Its generated branches and calls match the decoded OTP instructions after source paths are normalized.

There are 117 fewer paired functions with an ordinary allocation only in generated code, down from 21602 to 21485. All 1021 differing images prepared in OTP. The corpus admitted and generated 1363 of 1366 files. Every byte comparison status stayed the same, including the exact ELDAPv3 image.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Frame counts](frames.txt)
- [Loader results](loader.txt)
- [Generated execution probe](after.txt)
- [OTP execution probe](otp.txt)
