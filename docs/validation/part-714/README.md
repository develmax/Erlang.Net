# Part714 LDAP required range and trailing optional field

The compiler extends required field scheduling to a bounded first decoder result followed by a second required field and a trailing optional field. It keeps the first result in y1, reuses y2 for the second result and emits separate range and list-match failure paths. The fixture covers both range bounds, both required match errors and an absent optional field.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing range tests, local decoder calls and optional field emitter. Dependencies and licenses are unchanged.

dec_LDAPMessage/2 drops from 54 to 52 instructions and matches OTP after renumbering labels. It uses three stack slots instead of nine. The managed image is 99,056 bytes. Both modules have 7,028 instructions, and the OTP image is 99,008 bytes. Code and the Attr vsn still differ. [Instruction comparison](ldap-message-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part714-final-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part714-final.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
