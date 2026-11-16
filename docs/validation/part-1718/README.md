# Part 1718

The BEAM compiler removes internal labels from fallthrough paths when no instruction references them. This follows OTP 29.1.1 beam_jump remove_unused_labels. Function entries, function-info labels and line instructions are preserved. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 53672 bytes across 779 changed images. The absolute gap decreased in 777 images and increased in 2. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression checking the label count after removing an unreferenced fallthrough label. Three input cases returned matching outcomes when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
