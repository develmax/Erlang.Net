# Part 1642

Sibling calls can now use the existing call sequence planner when the second call consumes a saved input in a constructor. The emitter reads the old slot before storing the first call result in that same slot. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified the constructed arguments and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 1 images. The OTP loader accepted each differing image. The instructions of diameter_stats:start_link/1 match after label normalization. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Diameter](diameter-stats-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
