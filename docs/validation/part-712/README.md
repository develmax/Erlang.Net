# Part712 LDAP control decoder

The mixed decoder emitter accepts a leading required field followed by two optional fields. It saves the required result in y1 and reuses y2 for the first optional result. It moves the remaining tail into x0 before the final optional check. The regression covers each absent optional field, an ignored improper tail and the required match error.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing optional decoder, frame and list failure emission. Dependencies and licenses are unchanged.

dec_Control/2 drops from 59 to 55 instructions and matches OTP after renumbering labels. It uses three slots instead of fourteen. The managed image is 99,076 bytes with 7,031 instructions. OTP has 99,008 bytes and 7,028 instructions. Only Code and the Attr vsn differ. [Instruction comparison](control-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part712-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part712.json. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. [Focused checks](focused.json), [corpus](otp-corpus.json).
