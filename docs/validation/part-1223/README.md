# Part 1223 — forward stack trace operands into tail raise

Use the stack trace directly in X0 for tl/1 and write the replacement list into the third argument register for erlang:raise/3. Leave the unmatched exception path intact.

In error_handler:crash/1 the frame now matches OTP at two slots. The disassembly matches after excluding one generated init_yregs for the try token. Only error_handler changed. All 1,366 files were generated. The Windows OTP 29.1.1 comparison produced 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and the stack trace probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Frame delta](frame.txt)
- [Crash comparison](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Stack trace probe](probe.txt)
- [Opcode ranking](ranking.txt)
