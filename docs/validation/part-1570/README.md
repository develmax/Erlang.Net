# Part 1570 — Construct catch result tuples from the reason register

A simple catch that returns a tuple can use the exception reason directly from X1. The compiler reuses the case tuple planner and keeps X1 live during heap allocation. Handlers requiring general expression lowering retain their existing reason handling.

The fixture uses three Y slots, down from four, and four execution outcomes match OTP. The surrounding try branches and return layout still differ. Three focused compiler tests passed. [Fixture](../part-1568/beam_unused_catch_stack.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. The rule changed 39 images. No normalized function matches changed. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Loader](loader-and-code.txt).
