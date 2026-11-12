# Part719 LDAP final optional encoder slot reuse

The compiler reuses the source tuple slot for the penultimate encoded pair after projecting the final optional input. It compares the last input in x1 and moves it into x0 only on the call path. The regression covers both optional outcomes and accumulated lengths.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the deferred field schedule and sentinel handling. Dependencies and licenses are unchanged.

enc_LDAPMessage/2, enc_LDAPResult/2 and enc_ModifyDNRequest/2 have the same disassembled instructions and labels as OTP. Eleven functions retain instruction differences. The managed image is 98,980 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](final-optional-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part719-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part719.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
