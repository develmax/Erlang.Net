# Part 1221 — align tail operand moves and nested labels

Reserve the two nested boolean arm labels before the outer fallback label. Order independent stack-to-argument moves by their source slot in these tail branches.

The disassembled undefined_function/3 now matches OTP exactly, including labels. Seven error_handler functions match in the disassembly. The remaining six still differ. Only error_handler changed bytes. All 1,366 files were generated, with 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and the failure path probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Instruction comparison](error-handler.txt)
- [Function inventory](functions.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
