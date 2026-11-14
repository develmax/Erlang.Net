# Part 1676

Single-clause wrappers for supported ordinary erlang calls reuse register argument moves and emit a direct external tail call. The compiler resolves the target through the existing call planner.

The entire deterministic fixture is now byte-identical to OTP. The consume/1 wrapper uses call_ext_only. Three focused checks passed, including the new permanent regression. [Fixture](beam_deferred_record_field.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [Previous-stage VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change affected 42 release images. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
