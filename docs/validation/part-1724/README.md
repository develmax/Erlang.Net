# Part 1724

Remove writes from incoming X registers to trailing Y slots that have no other uses. Preserve writes to interior slots. For supported leaf functions with no remaining Y slots, remove the stack frame while retaining heap checks. Treat standalone tests and register initializations as barriers when looking ahead for a pure external call in the trim cost model. No dependency was added.

All 1,366 release sources compiled. 398 images matched byte for byte, including ELDAPv3 built from ASN.1. The OTP loader accepted all 968 differing images. The sum of absolute Code size differences decreased by 13,107 bytes across all 551 changed images. 371 functions gained instruction parity after normalizing labels and closure identifiers. No previously matching function lost that parity.

The build passed without warnings. All three targeted tests passed, including a new whole-image OTP fixture for a guarded function with no live frame slots. The corpus run used the pinned OTP source overlay with both headers and documentation.

[Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [First instruction differences](first-opcode-rank.txt)
