# Part 1710

The BEAM compiler merges a frame allocation and a heap check separated only by moves or Y-register initialization. This follows the heap accounting in OTP 29.1.1 beam_ssa_codegen. The combined instruction preserves the original entry live count and the exact requested heap words. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The aggregate Code size gap shrank by 24283 bytes across 700 changed images. The absolute gap decreased in 699 images and increased in 1. Normalized function matches stayed the same.

The build passed without warnings. All three targeted tests passed, including a regression that checks the emitted combined allocation. Three input cases returned matching results and call arguments when run in the OTP VM against the previous, current and reference images. The first corpus attempt used a headers-only source tree and missed seven Markdown documentation dependencies. The final run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt)
