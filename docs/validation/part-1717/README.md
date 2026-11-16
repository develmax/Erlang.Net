# Part 1717

The BEAM compiler removes terminal return blocks that have no incoming label references or fallthrough. This follows OTP 29.1.1 beam_jump remove_unused_labels. The pass removes only bare returns and deallocate-return sequences. It reuses the existing label remapping and preserves function entries and line instructions. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 9617 bytes across 390 changed images. The absolute gap decreased in 389 images and increased in 1. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a regression that checks removal of a shared return after all successful branches become tail calls. Four input cases returned matching outcomes when run in the OTP VM against the previous, current and reference images. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
