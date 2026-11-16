# Part 1618 — Propagate matched selector literals through saved call cases

The saved-call case planner now substitutes a known atom or integer for the bound selector within simple literal arms before calculating lifetimes. A selector with no other branch use stays in X0 for dispatch. Existing liveness and slot reuse handle other values that survive across the selector call. The pinned OTP 29.1.1 compiler and release disassembly provide the reference. No dependency was added.

The permanent beam_case_saved_value fixture matches OTP byte for byte. The OTP VM confirmed both branch results, case_clause on unmatched input, its source line, and call order. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The generated release images were byte-identical to part 1617. The new parity is demonstrated by the permanent fixture. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt).

All 973 remaining images differ in Code. The closest image by section count is logger_handler_watcher, whose handle_call/3 uses seven stack slots in C# and two in OTP. Its deferred tuple projections and record-update operands provide a release example for the next frame investigation. [Chunk ranking](baseline-chunk-rank.txt) · [Release disassembly](logger-handler-watcher-handle-call.txt).
