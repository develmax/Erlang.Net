# Part 1179 — match nested tuple entries in registers

For a single unguarded clause, the emitter can check nested tuple shapes and literals before allocation. It retains the original arguments on failure and loads the fields referenced by the body directly into Y registers after allocation. Repeated bindings and patterns outside this tuple path continue through the general matcher.

The new whole-module fixture matches deterministic OTP 29.1.1 byte for byte. Both outputs passed assertions for successful matching, mismatched literals, invalid nested shapes and original arguments in function_clause errors.

The Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. All 71 changed images moved closer in Code length. OTP prepared all 1,007 differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/tuple_nested_entry_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Opcode ranking](ranking.txt)
