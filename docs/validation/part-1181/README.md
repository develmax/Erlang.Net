# Part 1181 — test tuple guards before allocation

The register-based tuple matcher handles conjunctions of built-in unary type guards. A field used only by a guard stays in a scratch X register. A field also read by the body can be kept for the success path. Shape and guard failures preserve original arguments for the next clause or function_clause.

The permanent fixture matches deterministic Windows OTP 29.1.1 byte for byte. Both outputs passed assertions for successful matching, guard failure and fallback on shape mismatch.

The corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. The Code length gap narrowed in all 44 changed images. OTP prepared all 1,007 differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/tuple_guard_entry_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Opcode ranking](ranking.txt)
