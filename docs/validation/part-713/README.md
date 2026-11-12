# Part713 LDAP three required fields and a trailing optional field

The compiler recognizes three required list fields followed by a tagged optional field. It reuses two consumed tail slots for decoded results, reads the optional head from the last tail slot, and preserves each required match's error operand and source line. The fixture covers all three required failures, the optional default and an ignored improper tail.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing list match, optional decode and failure emission. Dependencies and licenses are unchanged.

dec_ModifyDNRequest/2 drops from 60 to 59 instructions and matches OTP after renumbering labels. It uses four slots instead of ten. The managed image is 99,068 bytes with 7,030 instructions. OTP has 99,008 bytes and 7,028 instructions. Only Code and the Attr vsn differ. [Instruction comparison](modify-dn-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part713-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part713.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
