# Part 1180 — branch from register tuple checks to a fallback

Extend register matching to an unguarded tuple clause followed by an unconditional fallback. The first branch allocates after its tests. A simple fallback tuple built from original arguments and literals stays in registers.

The permanent fixture matches the deterministic Windows OTP 29.1.1 output byte for byte. The runtime probe passed on both outputs, including a successful match and fallback on wrong shapes, tags and literal values.

The corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. All 138 changed images moved closer in Code length. OTP prepared all 1,007 differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/tuple_nested_fallback_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Opcode ranking](ranking.txt)
