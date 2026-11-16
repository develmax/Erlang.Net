# Part 1684

Structural list matching skips tuple fields whose single variable binding is unused in the guard, body, and pattern references. Registers assigned to literal field tests can be reused after the test. The implementation reuses the compiler's entry reference analysis. The reference is OTP 29.1.1 release disassembly and live intervals in beam_ssa_pre_codegen. No dependency was added.

The deterministic fixture matches OTP byte for byte and the OTP VM verified rejection of a nonmatching literal, field selection and the error for an improper tail. Three focused compiler tests passed. [Fixture](beam_structural_unused_fields.erl) · [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. 152 images changed. The Code size gap shrank in 109 images and stayed the same in 43. Normalized function match counts stayed the same. The eunit_autoexport Code chunk moved from 1,071 to 1,064 bytes against 862 bytes in OTP. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Loader and code sizes](loader-and-code.txt) · [Function comparisons](normalized-functions.txt) · [EUnit disassembly](eunit-rewrite.txt).

Full runtime tests and readiness regeneration were outside this run.
