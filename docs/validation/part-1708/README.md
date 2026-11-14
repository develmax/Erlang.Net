# Part 1708

Replace true and false selection arms with an is_boolean test when both lead to the same block. Remove an exact comparison if its failure and success jump targets coincide. Both rules follow OTP 29.1.1 beam_jump. The pass reuses the compact writer and existing atom indices. No dependency was added.

All 1,366 Windows OTP 29.1.1 sources were accepted and compiled. 398 images matched byte for byte, including ELDAPv3 from ASN.1. The OTP loader accepted all 968 differing images. The rules changed 11 images and reduced their combined Code gap by 104 bytes. The normalized function match count stayed the same. The build and all three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Function comparison](normalized-functions.txt) · [Tests](focused.txt).
