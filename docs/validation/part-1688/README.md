# Part 1688

After testing a shared tagged tuple head, the compiler projects the first field after the tag once when every clause in the group matches a literal there. The field register stays reserved across the match tests. Tests and guards retain their original clause order. The reference is constructor matching in OTP 29.1.1 beam_core_to_ssa and release disassembly. No dependency was added.

The fixture eliminates the repeated extraction. The OTP VM verified both matches, rejected shape and field values, fallback, the empty list and original function_clause arguments. Three focused compiler checks passed. The fixture image still differs from OTP. [Fixture](beam_shared_cons_tag.erl) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. The Code size gap shrank across all seven changed images, with the largest reduction of 60 bytes in edoc_layout. The normalized function match counts stayed the same. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
