# Part 1711

The BEAM compiler keeps a call result in X0 through a heap check when the only following use is construction of the returned tuple or list. The code follows OTP 29.1.1 beam_ssa_codegen prefer_xregs and liveness. It preserves the heap word count and marks X0 live. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 2682 bytes across 272 changed images. The absolute gap decreased in 272 images and increased in 0. Normalized function matches stayed the same.

The build passed without warnings. All three targeted tests passed, including regression checks for both returned constructions. Three input cases returned matching results and call arguments when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
