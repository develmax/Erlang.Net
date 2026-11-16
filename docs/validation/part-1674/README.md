# Part 1674

A call within a list passed to a tail call leaves its result in X0. Save only the other list elements that survive the call. The list is assembled before emitting a tail call with the reduced frame.

The full deterministic fixture is byte-identical to OTP 29.1.1. The OTP VM verified the PID, atom and default branches for both images. Three focused checks passed, including the permanent byte-comparison regression. [Fixture](beam_call_inside_tail_list.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. Only gen.beam changed. The instructions for format_status_header/2 now match after label normalization. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
