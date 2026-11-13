# Part 1609 — Schedule heap checks and deferred tuple projections

The saved-call emitter reserves heap space while the checked source tuple is still the only live X register. It delays the projection that overwrites X0 until the other fields have been consumed by call argument construction. The tuple-entry emitter places saved-call match failures after the remaining clauses.

The complete Windows OTP 29.1.1 corpus accepted and generated all 1,366 sources. There were 393 Equal and 973 Different. xmerl_xlate is the only new byte-identical image and ELDAPv3 remains Equal. Two more functions match after label normalization. Four other changed images have a combined Code size gap increase of 21 bytes. OTP accepted all 973 differing images for loading. [Corpus](otp-corpus.json) · [Disassembly](hook.txt) · [SHA-256](sha256.txt) · [Function comparison](normalized-functions.txt) · [Loader and Code sizes](loader-and-code.txt).

Three focused checks passed, including the release regression for xmerl_xlate. The builds used a larger .NET stack with shared compilation and analyzers disabled. [Tests](focused.txt).
