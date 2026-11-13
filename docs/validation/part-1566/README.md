# Part 1566 — Plan case selectors bound by one call

The saved-call planner now accepts a terminal case preceded by one call result binding. The producer and selector share X0. The planner keeps values used in the branches in Y slots and uses the same frame and branch emission as for longer sequences. The change reuses the existing planner and follows the call-result convention in OTP 29.1.1 beam_ssa_codegen.erl. No dependency was added.

Six functions gained a complete instruction match after label normalization and none lost one. The fixture matched three execution outcomes on OTP. Its successful branch still extracts a field where OTP substitutes a constant. Three focused compiler tests passed. [Function comparison](normalized-functions.txt) · [Release disassembly](map_intersect_kv_1.txt) · [Fixture](beam_bound_case_selector.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. Sixteen images changed, and each moved closer in Code section size. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt).
