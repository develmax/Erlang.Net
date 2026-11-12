# Part692 LDAP BER tag length accumulation

The C# emitter recognizes the BER tag accumulation clauses and uses the length encoder result directly in X0. The nonempty branch preserves the tag, tail, input length and payload in four slots. The empty branch returns the input pair without allocating a frame. The result length from the validated encoder and the byte size are annotated. The input accumulator and its first sum remain untyped until all callers prove a nonnegative integer. The fixture covers a tag and the empty tag list.

The managed encode_tags/3 drops from 49 to 30 instructions, the same count as OTP 29.1.1. After renumbering labels, the differences are the two nonnegative accumulator annotations. The input annotation in encode_length/1 remains open. [Tag encoder comparison](encode-tags-normalized-oracle.txt), [disassembly](encode-tags-oracle.txt).

The managed image drops from 102,096 bytes and 7,973 instructions to 102,040 bytes and 7,954 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the BER length shape proof, list and tuple operands, and tail-call instructions. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part692-tool`. Run `dotnet artifacts/beam-backend/part692-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part692.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
