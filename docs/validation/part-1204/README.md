# Part 1204 — release map and record operands

Map construction and checked record updates release temporary Y slots after emission while retaining visible bindings. Record updates emit literal fields directly.

First allocation size moved closer to OTP in all 1,225 changed functions. Code chunk size moved closer in 380 modules and farther in none. The full run generated 1,363 files with 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP prepared all 1,006 differing images for loading. Three focused tests passed. The OTP execution probe checked map and record values, exported bindings and badmap.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation comparison](frame.txt)
- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Opcode ranking](ranking.txt)
