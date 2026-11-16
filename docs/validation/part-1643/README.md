# Part 1643

The emitter groups literal list-head keys with identical replacement bodies into one select_val target. It reads the cons once and keeps the fallback head in X2 until the prepend. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified replacement and fallback behavior and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 1 images. The OTP loader accepted each differing image. The instructions of xmerl_xsd_type:start_link/1 match after label normalization. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Xmerl](xmerl-xsd-type-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
