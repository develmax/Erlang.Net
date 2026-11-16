# Part 1202 — emit direct comparison operands

The general emitter uses bif2 for exact and ordering comparisons. It reuses the operand handling for numeric comparisons to pass literals and bound registers directly and release computed operand slots after the comparison.

The first allocation moved closer to OTP in all 1,140 functions where its size changed. Code chunk size moved closer in 401 modules. The full corpus generated 1,363 files and recorded 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. OTP accepted the 1,006 differing images for loading. Three focused tests passed and the execution probe matched seven expected results.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation comparison](frame.txt)
- [Changed images](changed-images.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Opcode ranking](ranking.txt)
