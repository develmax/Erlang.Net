# Part 1678

Single-clause wrappers around terminal Erlang calls use the existing resolved call planner with control-transfer support and emit direct external tail calls.

The entire deterministic fixture is byte-identical to OTP. The OTP VM verified exception classes, reasons and top stack frames. Three focused checks passed, including the permanent byte comparison. [Fixture](beam_terminal_wrappers.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change affected 20 release images. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
