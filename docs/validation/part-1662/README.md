# Part 1662

Match a nested literal tuple with one exact test before entering the call frame. Use the original retained record to read the call argument. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified the result, side effect and three rejected patterns. Three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change aligned the instructions of ex_splitterWindow:handle_event/2 after label normalization in one release image. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
