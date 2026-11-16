# Part 1685

Structural tuple matching reuses the register containing a temporary tuple for its last projected binding. List dispatch can also project a nested tuple into the head register when later cons clauses discard the head. Aliases retain their source. This follows the observed OTP 29.1.1 release layout and the live interval rules in beam_ssa_pre_codegen. No dependency was added.

The fixture matches OTP byte for byte. The OTP VM verified nested bindings, shape and literal rejection, and the improper-tail error. Three focused compiler tests passed. [Fixture](beam_structural_nested_last_field.erl) · [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. The change affected 160 images and aligned the normalized instructions of dist_ac:del_dist_loaded/2. The Code size gap shrank in 14 images, including eunit_autoexport by 3 bytes, and grew by 12 bytes in init. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
