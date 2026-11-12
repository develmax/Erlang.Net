# Part702 LDAP sequence encoders

The C# emitter recognizes the empty and nonempty sequence-encoder clauses when the item encoder is proven to return a pair with a natural length. It emits the nonempty branch first, holds the tail and accumulators in three stack slots, and delays loading the payload until after length addition. The accumulator length is annotated only when the function is private and its nonrecursive calls supply natural seeds. The regression covers order, length, empty input and an improper tail.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses its pair facts, local-call inspection, frame instructions and typed operand encoding. Dependencies and licenses are unchanged.

Eleven encoders each drop from 46 to 33 instructions and match OTP after renumbering labels. The managed image drops from 101,164 bytes and 7,664 instructions to 100,680 bytes and 7,521 instructions. OTP has 99,008 bytes and 7,028 instructions. The Line section now matches byte for byte, including all 856 coordinates and their order. Code, ExpT, LocT, Attr and Type still differ. [Encoder comparison](sequence-encoder-delta.txt), [Line](line-oracle.txt), [sections](chunks-oracle.txt), [instruction gaps](instruction-gaps-oracle.txt).

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part702-tool` and ran `dotnet artifacts/beam-backend/part702-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part702.json` using the installed release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls.

Three focused checks passed from an isolated build. The full runtime harness on these sources and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
