# Part720 LDAP response encoder optional projection

The compiler projects the first optional input in a trailing optional sequence before saving the previous required result. It tests the sentinel in x1 and moves the value to x0 only on the encoding path. The fixture covers both outcomes and checks the following optional field.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing deferred field and optional sentinel emitters. Dependencies and licenses are unchanged.

enc_BindResponse/2 and enc_ExtendedResponse/2 each gain one instruction and match the OTP disassembly including labels. Nine functions retain instruction differences. The managed image is 98,984 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](response-encoder-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part720-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part720.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
