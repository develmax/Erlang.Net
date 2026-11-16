# Part 1725

Reuse X registers in eligible leaf functions whose instructions require neither a call frame nor heap reservation. Check liveness along every branch, preserve the registers needed by a failed function clause and elide initialization only when later instructions have no use for it. The pass rewrites Y operands in the assembled BEAM instruction stream using the existing reader and compact writer.

All 1,366 release files were generated. 398 images matched exactly, including ELDAPv3. OTP accepted all 968 differing images. The Code size gap shrank by 19,953 bytes in total across 255 changed images. The gap decreased in 254 images. In http_response it increased by 10 bytes because the only changed function, fill_headers/3-fun-0, now matches the OTP instructions while the image was already smaller than OTP. The instruction comparison recorded 104 newly matching functions and 0 that stopped matching after normalization of labels and closure identifiers.

The build had zero warnings and three targeted tests passed. The whole-module test includes an OTP fixture with a singleton-list entry clause and a fallback that returns the original argument. Before the pass, the fixture kept all three live values in the stack frame. The emitted image now matches OTP byte for byte.

The corpus has 6,662 functions in 775 images where the candidate allocates a frame immediately and OTP starts with a predicate before its first frame.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
