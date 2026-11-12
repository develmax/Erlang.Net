# Part 1205 — release call, send and division operands

The general emitter releases temporary operands after function-value calls, recursive closure calls, sends and floating-point division. First allocation size moved closer to OTP in 363 functions. Code size moved closer in 176 modules. Neither metric moved farther in any measured comparison.

The full Windows OTP 29.1.1 corpus generated 1,363 images, with 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP accepted all differing images for loading. Three focused tests passed and the execution probe verified call, recursion, message delivery and division.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation comparison](frame.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Opcode ranking](ranking.txt)
