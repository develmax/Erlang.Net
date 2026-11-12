# Part 1146 — keep mixed case tests in X0

The register case emitter now handles a sequence of literal, tagged tuple and unary type tests with a terminal fallback. It preserves the source order of the tests and uses the existing dispatch table for homogeneous literal branches.

The mixed_result/2 probe matches the Windows OTP 29.1.1 instructions after label normalization. Both images exercised the tagged tuple branch, binary branch and fallback. The release asn1ct test_value closure still requires a nested tuple fallback that the register result planner does not handle.

- [Probe source](beam_general_catch_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [Focused tests](focused.json)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. No release image changed. OTP prepared all differing images for loading.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Loader results](loader.txt)
