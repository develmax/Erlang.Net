# Part 1228 — fuse entry allocation for tuple operands

Use the existing constructed argument plan for tuples built from incoming parameters and literals. Reserve the tuple heap together with the frame and use a free X register if the destination is still needed by another operand.

Code size moved closer in 40 modules. The first allocation size moved closer in 60 functions. No measured size moved farther. All 1,366 Windows OTP 29.1.1 files were generated, with 357 Equal and 1,009 Different. ELDAPv3 remained Equal. OTP accepted all differing files. Three focused tests and the direct and overlapping destination probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Frame sizes](frame.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
