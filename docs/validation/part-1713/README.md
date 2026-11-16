# Part 1713

For straight-line function entries, the BEAM compiler reads eligible arguments from their original X registers through the first call preparation. It drops the saved copy when the X register remains available until the last read and the Y slot has no later references. This follows OTP 29.1.1 prefer_xregs. The existing frame sizing pass removes trailing slots that become unused. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 149 bytes across 435 changed images. The absolute gap decreased in 133 images and increased in 294. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression covering an argument used before the call and arguments needed across the call. Three input cases returned matching results and call arguments when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
