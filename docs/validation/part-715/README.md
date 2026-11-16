# Part715 LDAP result code and trailing optional field

The compiler extends required field scheduling to an enumerated first field. It reuses the decoded enumeration dispatch, preserves the tagged fallback for an unknown value and keeps the subsequent decoder results in the existing four-slot layout. The regression checks a known code, the unknown-code fallback and both optional outcomes.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses enumeration recognition, typed select operands and list failure emission. Dependencies and licenses are unchanged.

dec_LDAPResult/2 drops from 181 to 180 instructions and matches OTP after renumbering labels. It uses four stack slots instead of ten. The managed image is 99,052 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn still differ. [Instruction comparison](ldap-result-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part715-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part715.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
