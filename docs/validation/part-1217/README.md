# Part 1217 — emit length directly into a call argument register

When length/1 is the sole computed operand in a call, use gc_bif1 to write its result directly into the destination argument register. Keep ordinary handling for calls to other functions named length.

In error_handler:undefined_function/3 the length instruction and operands match OTP. Code size moved closer in 59 modules and farther in four. First allocation sizes did not change. All 1,366 images were generated. Windows OTP 29.1.1 comparison produced 357 Equal and 1,009 Different. ELDAPv3 remained Equal. OTP accepted all differing images. Three focused tests and the length operand probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Error handler disassembly](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
