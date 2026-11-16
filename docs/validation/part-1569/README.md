# Part 1569 — Dispatch simple catches from exception registers

An unguarded catch with a literal class, a fresh reason variable and a wildcard stack compares the class directly in X0. It stores the reason only when the matching handler reads it. A mismatched class rethrows using the original X2 trace and X1 reason. The rule follows OTP 29.1.1 exception register use and reuses lexical reference analysis and BEAM instruction emission.

The fixture frame shrank from six Y slots to four. Four OTP execution outcomes matched and three focused tests passed. [Fixture](../part-1568/beam_unused_catch_stack.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. The rule changed 179 images. No normalized function matches changed. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Loader](loader-and-code.txt).
