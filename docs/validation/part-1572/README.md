# Part 1572 — Share a constant fallback in tagged try results

A terminal try can match a tagged tuple in X0 and return the selected field or a tuple built from it. When the unmatched result and the handled exception return the same constant, they share a return block. Exceptions of other classes resume with the original trace and reason. The implementation reuses the entry call, tagged tuple test and tuple construction emitters. The reference is OTP 29.1.1 beam_ssa_codegen.erl.

The fixture matches OTP instructions after normalizing source filenames. Five execution outcomes cover a successful projection, a wrong tag, a wrong arity, a caught error and a rethrown exception. Three focused compiler tests passed. [Fixture](beam_unused_catch_stack.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. The new rule did not change any release image. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).
