# Part 1720

The BEAM compiler removes moves whose source and destination are the same register. The OTP 29.1.1 disassembly contains no identity moves in the corresponding images. The existing edit writer leaves labels and line instructions intact. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 2529 bytes across 277 changed images. The absolute gap decreased in 277 images and increased in 0. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression checking that X0 is not copied to itself. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
