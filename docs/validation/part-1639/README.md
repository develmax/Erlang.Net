# Part 1639

The C# compiler skips constant expressions with unused results before planning case selectors and case arms. This includes disabled debug macros that expand to ok. The ordinary call planner now accepts erlang:whereis/1. The case emitter keeps the selected value in X0, saves only the surviving inputs and emits tail calls directly. Pinned OTP 29.1.1 sys_core_fold and release disassembly provide the reference. No dependency was added.

The fixture is byte-identical and the OTP VM verified the two lookup outcomes. All three focused checks passed. [Disassembly](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 Windows OTP release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. No release image changed equality status. ELDAPv3 remains byte-identical. The update changed 42 generated images and the OTP loader accepted every differing image. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside this run.
