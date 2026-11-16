# Part 1687

Consecutive structural list clauses with the same tagged head tuple share one arity and tag test. Failure of the shared test skips the group. Failure of a field test or guard proceeds to the next clause in its original order. The compiler keeps the same projection and capture planners. The reference is constructor matching in OTP 29.1.1 beam_core_to_ssa. No dependency was added.

The focused fixture reduces two identical tagged tuple tests to one. The OTP VM verified tag and field rejection, successful matches, an invalid nested tuple, the empty list and the original function_clause arguments. Three focused compiler checks passed. OTP still emits different dispatch and register sequences for the whole fixture. [Fixture](beam_shared_cons_tag.erl) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. The Code size gap shrank in all 73 changed images, including init by 14 bytes. The normalized function match counts stayed the same. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
