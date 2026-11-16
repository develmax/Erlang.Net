# Part 1224 — omit initialization for an entry try token

Skip the token slot in init_yregs when an unguarded single clause starts with a try that writes that slot before evaluating any protected operands.

The crash/1 disassembly in error_handler matches OTP exactly. Code size moved closer in 275 modules and farther in none. All 1,366 Windows OTP 29.1.1 images were generated. The comparison produced 357 Equal and 1,009 Different with zero rejected sources. ELDAPv3 stayed Equal. OTP accepted all differing images. Three focused tests and the GC and catch probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Crash comparison](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
