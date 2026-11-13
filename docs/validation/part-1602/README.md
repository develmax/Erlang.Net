# Part 1602 — Emit direct length instructions in function bodies

The previous release corpus had 878 functions across 308 modules containing both an OTP direct length instruction and a C# external call to length/1. The counts compare instruction kinds present within each function. [Ranking](bif-rank.txt).

Body calls resolved to erlang:length/1 now use the existing garbage-collecting size instruction emitter. It uses Y registers and literals directly and retains the evaluated argument in X0 for a computed operand. The error edge uses label zero.

The OTP VM produced the same result for a proper list and the same badarg outcomes for an atom and an improper list. Allocation and type annotations still differ in the fixture. Three managed checks passed. The successful builds used a larger .NET stack and disabled shared compilation and analyzers. [Fixture](beam_length_body.erl) · [Previous disassembly](baseline-disassembly.txt) · [Current disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 pinned release sources were accepted and generated. There were 392 Equal and 974 Different, with ELDAPv3 Equal. The change affected 321 images. dist_ac:get_weight/0 now matches after label normalization. The Code size gap grew by 860 bytes overall, with 48 images closer, 119 farther and 154 unchanged in size. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt) · [dist_ac disassembly](dist-ac-disassembly.txt).
