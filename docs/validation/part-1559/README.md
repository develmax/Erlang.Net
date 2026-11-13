# Part 1559 — Fold constant wildcard record fields

The compiler folds record constructors with a constant wildcard initializer into literal tuples, including when the record is nested in call arguments. Explicit fields override the wildcard, and a dynamic wildcard still uses expression lowering. This reuses the existing record expansion and constant reader and follows record_inits/2 in OTP 29.1.1 erl_expand_records.erl. No dependency was added.

In ct_config:release_allocated/0, the match specification becomes a single literal, and the result of ets:select/2 reaches the tail call in X0 with zero Y slots. The fixture matched four outcomes on the OTP VM. Three selected compiler tests passed. [Disassembly](release_allocated.txt) · [Fixture](beam_record_wildcard_literal.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. Ten images changed. Six functions in four modules gained a complete instruction match after label normalization, and none lost one. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Instruction comparison](normalized-functions.txt) · [Loader and Code sizes](loader-and-code.txt).
