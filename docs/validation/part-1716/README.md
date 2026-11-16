# Part 1716

The BEAM compiler folds a call followed by a jump directly to a shared return or deallocate and return. The tail-call operands follow OTP 29.1.1 beam_z. Ordinary local calls, external calls and framed dynamic apply retain their original operands. Functions with exception handlers keep their shared return jumps. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 28404 bytes across 764 changed images. The absolute gap decreased in 762 images and increased in 2. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression that checks the short-circuit call uses a tail instruction and the other branch can still return. Three input cases returned matching results when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
