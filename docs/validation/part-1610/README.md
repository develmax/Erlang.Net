# Part 1610 — Emit direct unary arithmetic and map size instructions

Function bodies use the shared garbage-collecting unary BIF emitter for resolved round/1, trunc/1, abs/1, float/1, ceil/1, floor/1 and map_size/1. The fixture matches OTP VM outcomes for valid inputs, a computed operand, badarg and badmap. Allocation and operand types still differ. Three focused checks passed. [Fixture](beam_numeric_bifs.erl) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 pinned sources were accepted and generated. There were 393 Equal and 973 Different. ELDAPv3 and xmerl_xlate remained Equal. Of 116 changed images, the Code size gap shrank in 21, grew in 34 and held steady in 61. The aggregate Code size gap and the count of matching functions after normalization stayed the same. OTP accepted all 973 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt).
