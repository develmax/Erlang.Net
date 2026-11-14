# Part 1646

The compiler emits one miss return for ordered cons search. A failed comparison branches to that return while an empty list reaches it after the nil test. The recursive call stays in registers. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified numeric equivalence and both miss paths and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 1 images. The OTP loader accepted each differing image. The instructions of ordsets:start_link/1 match after label normalization. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [ordsets](ordsets-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
