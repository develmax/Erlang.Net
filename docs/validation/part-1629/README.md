# Part 1629 — Coalesce line instructions for discarded constructor calls

Calls with prepared operands in discarded tuples and lists share a line instruction when their source location repeats. General expression emission still resolves and emits the call. The tracked location is scoped to the constructor evaluation and cleared at other evaluated expressions. Pinned OTP beam_z and beam_jump provide the reference. No dependency was added.

The permanent same-line fixture matches OTP byte for byte. The OTP VM observed the same call order and error stack, including the failing callee's source line. Three focused tests passed. [Disassembly](disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

A trial of function-wide suppression lost two release matches, cdv_term_cb and ssl_client_session_cache_db, and failed the single-line try/after fixture. OTP can distinguish exception scopes at one source location. The trial was replaced with a rule for straight-line constructor operands.

The final build accepted and generated all 1,366 Windows OTP 29.1.1 release images. The bytes remained identical to the previous run for all release images, with 393 Equal and 973 Different. ELDAPv3 was Equal. The OTP loader accepted all 973 differing images. The full test suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Loader](part1629-loader-and-code.txt).

Continue on branch frame placement and Y register allocation, then compare the functions and complete release images. [Frame ranking](../part-1627/frame-ranking.txt).
