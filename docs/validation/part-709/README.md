# Part709 LDAP two-field encoders

The emitter projects both fields before calling the first encoder. It saves the tag and the second input in two stack slots, swaps the second input with the first result, and projects both returned pairs directly into the final list and length operands. The regression covers payload order, total length and the tuple match failure.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses deferred tuple recognition, proven pair returns, arithmetic operand types and frame emission. Dependencies and licenses are unchanged.

Eight two-field encoders drop from 34 to 32 instructions and match OTP after renumbering labels. The managed image is 99,108 bytes with 7,034 instructions. OTP has 99,008 bytes and 7,028 instructions. Code and the Attr vsn still differ. Line, Type, ExpT and LocT remain byte-identical. [Instruction comparison](two-field-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part709-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part709.json. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed after a sequential build retry. The parallel test build had encountered a lock on the shared runtime intermediate assembly. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. [Focused checks](focused.json), [corpus](otp-corpus.json).
