# Part 1703

Collapse acyclic chains of forwarding labels within a function and remap referring instructions. Preserve entry and function-info labels. The pass excludes functions with catch, try and receive instructions. It reuses the shared-return label map and byte edits, following OTP 29.1.1 beam_jump collect_labels. No dependency was added.

The first corpus run exposed overlapping removal edits in 52 images. Skipping instructions already covered by an edit fixed the failures. The final Windows corpus admitted and compiled all 1,366 sources. 398 images matched, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepted all 968 differing images. Across 749 changed images, the absolute Code-size gap shrank in 747 and grew in two, for a net reduction of 73,451 bytes. Normalized function match counts stayed the same. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt).

The build and all three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Tests](focused.txt).
