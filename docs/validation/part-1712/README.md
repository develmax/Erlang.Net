# Part 1712

For functions with a single stack allocation, the BEAM compiler removes unused trailing Y slots and updates allocation, initialization, deallocation and framed tail calls together. OTP 29.1.1 beam_ssa_pre_codegen frame_size calculates the frame from the assigned Y registers. The compiler keeps the existing slot indices and avoids frames with trim, exception handlers or debug register descriptions. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 2277 bytes across 400 changed images. The absolute gap decreased in 390 images and increased in 0. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression checking the reduced frame and its matching deallocation. Three input cases returned matching results and call arguments when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
