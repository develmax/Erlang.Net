# Part 1575 — Keep the last call result in X0 across terminal case branches

When terminal case lowering needs one extra spill slot for a result used across a subsequent call, it reserves that slot in the entry frame. The last call result stays in X0 until the tail call arguments are ready. The planner accepts processes/0 and system_info/1 as ordinary external calls and follows OTP 29.1.1 erl_bifs.erl and beam_ssa_codegen.erl when omitting the call-site line for processes/0.

In erts_trace_cleaner:call_check/0, the frame drops from two slots to one. The instructions, register operands and source line operands match OTP after label normalization. [Disassembly](call_check.txt).

Three focused compiler tests passed. The OTP VM agreed on both literal branches and the unmatched case. The fixture still emits an explicit move for the matched false literal. [Tests](focused.txt) · [Fixture](beam_case_result_registers.erl) · [Outcomes](fixture.txt) · [Instructions](run.txt).

All 1,366 sources were accepted and generated, with 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. The compiler changed 24 images, yielding eight new normalized instruction matches and zero losses. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Functions](normalized-functions.txt) · [Loader](loader-and-code.txt).
