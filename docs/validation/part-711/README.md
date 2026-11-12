# Part711 LDAP required and optional decoder pair

The compiler accepts a required penultimate field when it is also the first field. The optional field reads the tail saved in y1 and retains the missing-field default. The regression covers a present optional field, an absent optional field and the required match error.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the mixed decoder emitter and its failure path. Dependencies and licenses are unchanged.

dec_SaslCredentials/2 and dec_ExtendedRequest/2 each drop from 41 instructions to 38 and match OTP after renumbering labels. Both use two slots instead of eight. The managed image is 99,100 bytes with 7,035 instructions. OTP has 99,008 bytes and 7,028 instructions. Only Code and the Attr vsn differ. [Instruction comparison](required-optional-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part711-final-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part711-final.json. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The final corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. [Focused checks](focused.json), [corpus](otp-corpus.json).
