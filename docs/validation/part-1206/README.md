# Part 1206 — emit simple guard comparisons before allocation

The general emitter extends the existing entry guard prefix to comparisons of direct arguments and constants. Comparison tests preserve the original argument registers and target the function entry failure label.

The full Windows OTP 29.1.1 corpus generated 1,363 images. It recorded 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. Bytes changed in 66 images. Code size moved closer in 24 modules and the first allocation size moved closer in three functions. OTP accepted all differing images for loading. Three focused tests passed. The probe checked opcode placement, one successful call and three function clause failures with original arguments in the stack.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation comparison](frame.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](probe.erl)
- [Probe result](probe.txt)
- [Opcode ranking](ranking.txt)
