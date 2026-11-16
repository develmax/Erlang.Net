# Part 1614 — Keep unary BIF operands and results in X registers

The existing register planner handles unary hd/1, tl/1, node/1 and tuple_size/1 with register or literal inputs. The planner delays the tuple heap check until after potentially failing BIFs and counts the live result registers.

The entire fixture matches OTP 29.1.1 byte for byte. Both SHA-256 values are 59B7B7A2FDB91A7FF50ECA2CEE03729AB0C67ED6DB566BBCC479E9BB1DB00E80. The OTP VM agrees on the result and both badarg paths. Three focused tests passed. [Fixture](beam_register_bifs.erl) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 pinned release sources were accepted and generated. There were 393 Equal and 973 Different. The Code size gap shrank in all four changed files, by 34 bytes in total. dets_utils:all/1 now matches after label normalization. OTP loaded all 973 differing images. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt).
