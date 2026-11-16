# Part 1722

Run linear frame trimming again after control-flow optimizations to expose suffixes ending in a local return. OTP 29.1.1 runs beam_trim after beam_jump. For trimming immediately after allocation without relocations, emit a smaller allocation and remap the remaining Y operands. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap changed by a reduction of 229 bytes across 120 changed images. The absolute gap decreased in 70 images and increased in 47. The normalized function comparison records the functions that gained or lost instruction parity.

The build passed without warnings. All three targeted tests passed, including a complete deterministic BEAM comparison against OTP 29.1.1 for a reduced entry frame. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
