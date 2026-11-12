# Part 1222 — match simple exception handlers in X registers

For a direct nonreturning Erlang BIF and a single unguarded catch with a literal class and reason, test the handler registers before materializing the stack. Rethrow an unmatched exception using the untouched reason and raw stack registers.

The initial frame in error_handler:crash/1 decreased from seven to four slots against the two-slot OTP reference. Code size and first frame size moved closer in three modules. The full Windows OTP 29.1.1 run generated all 1,366 files, with 357 Equal and 1,009 Different. ELDAPv3 remained Equal. OTP accepted all differing images. Three focused tests and the exception probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [First allocation sizes](frame.txt)
- [Crash disassembly](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Exception probe](probe.txt)
- [Opcode ranking](ranking.txt)
