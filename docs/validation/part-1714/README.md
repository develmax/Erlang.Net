# Part 1714

The BEAM compiler consolidates split init_yregs instructions in the entry frame prologue. OTP 29.1.1 beam_ssa_codegen emits a single sorted initialization list for slots that still need a value. Incoming X-to-Y copies keep their relative order. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 2764 bytes across 309 changed images. The absolute gap decreased in 309 images and increased in 0. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression covering two initialization instructions produced by entry argument copy elimination. Three input cases returned matching results and call arguments when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
