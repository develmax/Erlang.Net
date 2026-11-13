# Part 1568 — Skip stacktrace construction for wildcard catch patterns

The general try emitter keeps the raw stacktrace for rethrow and skips building the Erlang stacktrace list when all catch patterns ignore the stack field. It still builds the list for patterns that can observe it. This follows OTP 29.1.1 v3_core.erl and adds no dependency.

The fixture uses one fewer stack slot and matches four OTP execution outcomes, including unmatched rethrow and a named stacktrace. Three focused tests passed. [Fixture](beam_unused_catch_stack.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. The rule changed 463 release images. No whole function gained or lost a normalized instruction match. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Loader and Code sizes](loader-and-code.txt).
