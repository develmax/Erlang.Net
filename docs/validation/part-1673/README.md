# Part 1673

A guarded return of a call result leaves the value in X0. The emitter saves only the fallback argument, attaches an integer type to a comparison after is_integer/1, and uses existing private call-site constant inference for the selector arguments.

The full deterministic fixture is byte-identical to OTP 29.1.1. Both images returned nonnegative integers and selected the fallback for other values. Three focused checks passed, including the permanent byte-comparison regression. [Fixture](beam_guarded_call_return.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. Only shell.beam changed. The get_env/2 instructions now match OTP after label normalization. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
