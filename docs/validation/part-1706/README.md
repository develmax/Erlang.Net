# Part 1706

Invert a comparison when the conditional failure label follows an unconditional jump. The inverse branches to the original jump target and otherwise falls through. The rewrite preserves test operands and labels and uses the six inverse opcodes in OTP 29.1.1 beam_jump. It runs between iterations of the existing control-flow simplifier. No dependency was added.

The Windows corpus accepted and compiled all 1,366 sources. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepted all 968 differing images. 682 images changed. The absolute Code-size gap shrank in 680 and grew in two, with a net reduction of 33,486 bytes. The number of normalized function matches stayed the same. The build and all three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt).
