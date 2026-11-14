# Part 1683

Structural list entry dispatch reuses the original list register for the tail when the final cons clause covers all remaining nonempty inputs and subsequent clauses only match nil. Register allocation preserves the previous closure capture ordering. The reference is OTP 29.1.1 release disassembly and X register reservation in beam_ssa_pre_codegen. No dependency was added.

The deterministic projection fixture matches OTP byte for byte. The OTP VM tested nested matches, the catch-all cons clause, nil and the original arguments in function_clause. Three focused compiler checks passed. [Projection](beam_structural_cons_projection.erl) · [VM fixture](beam_structural_cons_tail.erl) · [SHA-256](fixture-sha256.txt) · [Disassembly](projection.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. The comparison found 158 changed images and three new instruction matches after label normalization, with no losses. The OTP loader accepted all 968 differing images. The Code size gap grew by 3 bytes for eunit_autoexport. [Corpus](otp-corpus.json) · [Function comparisons](normalized-functions.txt) · [Loader and code sizes](loader-and-code.txt).

Full runtime tests and readiness regeneration were outside this run.
