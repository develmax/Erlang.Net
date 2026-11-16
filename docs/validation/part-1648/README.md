# Part 1648

The emitter dispatches the literal atom alternatives to the same return reached by a successful integer bound check. It keeps the selector in X0 and uses the checked integer type for the comparison. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified atom matches, integer bounds, and the original exception terms and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 1 images. The OTP loader accepted each differing image. See the systools_rc disassembly for the remaining instruction differences. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [SASL](systools-rc-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
