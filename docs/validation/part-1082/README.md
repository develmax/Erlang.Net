# Part 1082 retain prefix-only binding results in X registers

The general emitter reuses lexical capture analysis to find initial bindings used in the continuation. It allocates unused-in-continuation results in X registers above the input registers, within the available range. The chained lookup probe runs on OTP and requires five Y slots where the preceding build required six.

All 1,366 Windows OTP 29.1.1 release files were accounted for. Results are 348 Equal, 1015 Different, 3 SourceRejected. ELDAPv3 is Equal. All 1015 differing images were accepted by the OTP loader and their hashes match the previous generated images.

- [Full corpus](otp-corpus.json)
- [OTP loader results](loader.txt)
- [Focused checks](focused.json)
- [Probe source](init_prefix_probe.erl)
- [Disassembly before](before.txt)
- [Disassembly after](after.txt)
- [Opcode ranking](opcode-ranking.txt)
