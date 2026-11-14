# Part 1645

The compiler extends the existing return block pass to merge identical return blocks without frames. Matching still compares the return operand, stack frame identity and exception-handler scope. OTP beam_jump provides the reference. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified shared returns and the side effect of the fallback and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 54 images. The OTP loader accepted each differing image. The instructions of asn1ct_func:start_link/1 match after label normalization. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [ASN.1](asn1ct-func-disassembly.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
