# Part 1227 — fuse entry allocation for multi-argument calls

Allow a single list operand in a multi-argument entry call. Use the final argument register when it can be overwritten, or a free X register when the previous value must be forwarded elsewhere. Feed the scratch result through the existing move scheduler.

Code size moved closer in 62 modules and farther in one. First allocation size moved closer in 50 functions and farther in none. All 1,366 Windows OTP 29.1.1 files were generated, with 357 Equal and 1,009 Different. ELDAPv3 remained Equal. OTP accepted all differing files. Three focused tests and two overlapping argument layouts passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Frame sizes](frame.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
