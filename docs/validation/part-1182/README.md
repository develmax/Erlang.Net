# Part 1182 — share tests for two tagged tuple branches

Two clauses with the same tuple tag and arity and distinct atom selectors can share shape and literal checks. A select_val chooses the branch before its guard and stack allocation. Failed checks preserve the original input registers.

The permanent fixture matches deterministic Windows OTP 29.1.1 byte for byte. Both outputs passed assertions for branch selection, guard failure, unknown selector, shape failure and original arguments in function_clause errors.

The corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. Ten differing images changed. The Code length gap narrowed in eight and widened in two. The changed functions in those two images had fewer disassembled instructions, but their module Code chunks grew by 50 and 4 bytes. OTP prepared all 1,007 differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/tuple_shared_dispatch_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Functions in widened images](widened.txt)
