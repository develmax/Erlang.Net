# Part 1682

For a list entry with a type-guarded cons result, the emitter reuses the input register for the tail, tests the guard before allocating a frame and uses the existing cons-result emitter to keep values live across each call. The empty-list branch still checks the original input when the list test fails. The reference is the OTP 29.1.1 release disassembly and the list instruction formation in beam_z.

The deterministic fixture matches OTP byte for byte. The OTP VM verified both branches, the empty list and function_clause for an improper tail. Three focused compiler checks passed. [Fixture](beam_guarded_cons_result.erl) · [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. Only proplists changed. Its unfold/1 now matches the OTP instructions and its Code size moved 19 bytes closer to OTP. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Release function](proplists-unfold.txt) · [Loader and code sizes](loader-and-code.txt).

In the preceding corpus, 170 modules used different tail destinations after the same entry test and had an is_nil instruction at both failure destinations. A module can contribute to multiple patterns. The ranking compares the first non-line opcode at each destination and skips pairs with the same destination opcode and register reuse. [Rank](rank.txt) · [Script](rank.escript).

The full runtime suite and readiness regeneration were outside this run.
