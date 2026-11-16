# Part 1219 — emit direct tail calls in boolean return branches

A nested boolean case in the success path of a folded entry case can terminate with a local or external tail call on each arm. The unmatched path uses the selector call location before case_end.

Only error_handler changed. Its undefined_function/3 emits both reference tail call opcodes and the reference error line. Operand move order and error block placement still differ. The full Windows OTP 29.1.1 corpus generated all 1,366 files, with 357 Equal and 1,009 Different. ELDAPv3 remained Equal. OTP accepted all differing files. Three focused tests and the execution probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Error handler disassembly](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
