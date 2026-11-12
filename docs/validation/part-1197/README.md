# Part 1197 — restore bootstrap header paths in Inets debug forms

The Windows release profile records the bootstrap paths for kernel/include/file.hrl and kernel/include/inet.hrl. It keys the paths by the original include_lib requests and preserves the Inets header mappings.

Fifteen additional Inets Dbgi chunks match the release, with zero regressions. The corpus generated 1,363 of 1,366 images. Results were 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP prepared all 1,006 differing images for loading. All three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Debug chunk delta](debug-delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Opcode ranking](ranking.txt)
