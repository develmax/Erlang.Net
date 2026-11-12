# Part 1213 — emit an external tail call after a folded entry case

Emit call_ext_last for a simple external call in the success branch of a folded entry case. Both branches can now return directly, so the join and frame return are unnecessary. In error_handler:undefined_lambda/3 the instruction sequence now follows the reference, with Y-register numbering and labels still different.

Only error_handler changed bytes. All 1,366 images were generated. The Windows OTP 29.1.1 comparison produced 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and three probe calls passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Error handler disassembly](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
