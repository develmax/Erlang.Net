# Part 1186 — reuse tuple-return lowering after entry guards

When a single clause's entry guard has already been emitted in registers, reuse the call-and-tuple lowering. The call result stays in X0 and only inputs needed after the call are saved. The permanent deterministic OTP 29.1.1 fixture now matches byte for byte. The OTP execution probe passed the successful call and the function_clause case with original arguments.

The Windows corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. The release images were unchanged from Part 1185. ELDAPv3 remained Equal. OTP prepared all 1,007 differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/guard_first_call_probe.erl)
- [Probe assertions](../part-1185/run.eval)
- [Before](../part-1185/after.txt)
- [After](after.txt)
- [OTP](../part-1185/otp.txt)
