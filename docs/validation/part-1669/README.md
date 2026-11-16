# Part 1669

The compiler emits get_hd when tuple-headed list patterns leave the tail unused. A variable return can load the already matched tuple field into X0 directly. OTP 29.1.1 beam_z keeps separate head and tail instructions unless both extractions can be combined. The existing entry emitters are reused.

The checked and unknown functions in the fixture match OTP after label normalization. The remaining functions and metadata in the fixture still differ. The OTP VM verified both images and three focused checks passed. [Fixture](beam_proper_cons_entry.erl) · [Instructions](disassembly.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change altered 19 release images. The instructions in test_server_ctrl:get_props/1 now match after label normalization. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
