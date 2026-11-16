# Part 1215 — dispatch boolean case results from X0

Use select_val for a case consisting of unguarded true and false clauses. Keep the original selector in X0 on the unmatched path and report it through case_end.

Code size moved closer to OTP in 538 modules and farther in 66. First allocation size moved closer in 2,469 functions and farther in 111. In error_handler:undefined_function/3 the first allocation is now three slots, and the boolean dispatch uses the reference opcode. The preparation of function_exported/3 and the branch tail calls still differ.

All 1,366 images were generated. The Windows OTP 29.1.1 comparison produced 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and both boolean branch orders passed, including the original selector in case_clause.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Allocation comparison](frame.txt)
- [Error handler disassembly](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
