# Part 1677

The ordinary-call planner admits the supported spawn, spawn_link, spawn_monitor and spawn_opt signatures. Single-clause wrappers use register argument moves and a direct external tail call. Existing clause-specific emitters can also use the resolved process calls.

The entire deterministic fixture is byte-identical to OTP. The OTP VM verified child execution, monitor exits and MFA dispatch. Three focused checks passed, including the permanent byte comparison. [Fixture](beam_spawn_tail_wrappers.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. The change affected 31 release images. Normalized instruction comparisons found gains in 24 modules. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
