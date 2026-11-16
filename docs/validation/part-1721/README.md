# Part 1721

The BEAM compiler replaces direct jumps to a return or deallocate-return sequence with that sequence after block sharing. This follows OTP 29.1.1 beam_jump unshare. Functions with catch or try handlers retain the shared form. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap changed by a reduction of 54904 bytes across 833 changed images. The absolute gap decreased in 818 images and increased in 11. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression checking that two successful branches return directly. Four paths through the fixture produced matching values and call arguments in the OTP VM. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
