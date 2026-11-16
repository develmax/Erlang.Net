# Part700 terminal decoded ranges

The C# emitter reuses the existing range checks for a two-argument local call followed by an immediate bounded return. It keeps the result in x0 and emits the failure as a tail call to exit/1 with a zero-slot frame. The regression covers an integer, a float and violations of both bounds.

The pinned reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The range matcher, inferred operand types and frame emission are reused from the managed compiler. Dependencies and licenses are unchanged.

All 15 instructions in dec_MessageID/2 match OTP after renumbering labels, down from 29. The managed image drops from 101,400 bytes and 7,744 instructions to 101,320 bytes and 7,716 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type still differ. Line contains 856 locations on both sides and counts 858 instructions in the managed image versus 862 in OTP. The first difference after its header is at byte 308 of the chunk. [Range comparison](message-id-oracle.txt), [instruction counts](disassembly-oracle.txt), [chunks](chunks-oracle.txt), [line table](line-oracle.txt).

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part700-tool` and ran `dotnet artifacts/beam-backend/part700-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part700.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls.

All three focused checks passed. The full release corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).

The decoded Line table has 37 positions with different entries. The first differing position is 144, with line 814 in OTP and line 811 in the managed image. Four OTP entries are replaced by the opening line of the corresponding list comprehension in the managed image. [Decoded order comparison](line-order-oracle.txt).

The full runtime harness was started separately and has no completed report at this checkpoint. Canonical readiness regeneration remains pending. The scope retains the immutable Part561 comparison report.
