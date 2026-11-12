# Part710 LDAP two required decoder fields

The compiler keeps the remaining input list in y1 across the first decoder call. Before the second call it moves the first result into y1 and trims the frame, matching OTP's lifetime for the discarded tail. Separate failure labels preserve the first and second badmatch terms. The regression checks an ignored improper tail and both required field failures.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing decoder call, list match and failure emission. Dependencies and licenses are unchanged.

Seven decoders gain one instruction each and match OTP after renumbering labels. The managed image grows from 99,108 to 99,128 bytes with 7,041 instructions. OTP has 99,008 bytes and 7,028 instructions. Code and the Attr vsn still differ. Line, Type, ExpT and LocT remain byte-identical. [Instruction comparison](two-required-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

The length helpers still lack a proof that every incoming accumulator length is a nonnegative integer. Their annotations were left pending. Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part710-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part710.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
