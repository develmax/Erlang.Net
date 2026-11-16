# Part705 LDAP optional decoder chains

The compiler tracks the remaining list through successive optional fields and reuses its stack slot for the decoded value. It takes only the head for the final field because the tail has no effect. The fixture covers all three fields, each field absent, all absent and an improper ignored tail.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change extends the existing single optional field recognizer and uses the existing tuple, list and frame instructions. Dependencies and licenses are unchanged.

dec_PasswdModifyRequestValue/2 drops from 79 to 65 instructions and dec_IntermediateResponse/2 drops from 60 to 47. Both match OTP after renumbering labels. The managed image is 99,328 bytes with 7,097 instructions. Line remains byte-identical. [Instruction comparison](optional-decoder-delta.txt), [sections](chunks-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part705-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part705.json. Three focused checks passed. Full runtime validation on these sources and canonical readiness regeneration remain pending.

The corpus covered 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. [Focused checks](focused.json), [corpus](otp-corpus.json).
