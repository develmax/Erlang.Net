# Part 1178 — extract tuple fields directly into the stack

After the entry tests, framed tuple clauses allocate using the original argument count and extract only referenced fields directly into Y registers. Tail calls still use the register path. The new whole-module fixture matches deterministic OTP 29.1.1 byte for byte, and both outputs passed the runtime assertions for successful matching and original arguments in function_clause errors.

The Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. The bytes changed in 357 differing images. Code length moved closer in 349 and the gap did not grow in any image. OTP prepared all differing images for loading. Three focused tests passed, including the new fixture.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/tuple_register_entry_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
