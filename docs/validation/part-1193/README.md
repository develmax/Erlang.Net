# Part 1193 — fold constant bindings in tuple returns

The compiler folds constant bindings into a tuple returned alongside incoming arguments. The tuple uses the existing register constructor. The ct_master_status:handle_call/2 instructions now match the Windows OTP 29.1.1 release. The permanent bound_tuple_probe fixture matches the deterministic OTP image byte for byte.

The full corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. The Code length gap narrowed in all three changed images. ELDAPv3 remained Equal. OTP prepared all differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Release disassembly](disassembly.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/bound_tuple_probe.erl)
