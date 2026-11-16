# Part 1672

When a call-result case returns the same original argument for nil and a specific atom, allocate one slot for that argument. Keep the selector in X0 and share the fallback return block.

The full deterministic fixture is byte-identical to OTP 29.1.1. Both images returned the fallback for the two matching values and preserved unmatched selector values. Three focused checks passed, including the permanent byte-comparison regression. [Fixture](beam_shared_call_case.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change altered 1 release images. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
