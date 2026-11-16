# Part 1649

The emitter shares the success block for integer literal alternatives and three ordered, disjoint ranges. It preserves OTP lower-bound annotations along failed upper-bound tests and returns the final comparison directly. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified 25 outcomes including integer keys, floating-point members and nonnumeric terms. Three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 6 images. The OTP loader accepted each differing image. After label normalization, instructions in seven Xmerl functions newly match OTP. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Xmerl](xmerl-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
