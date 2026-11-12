# Part685 LDAP out-of-line range failures

Range checks in top-level list decoder steps now branch to failure blocks emitted after the return and before the list match failures. Each block emits its own source location, preserves the error reason and tail-calls exit/1 with the original frame size. The generic range path inside other expressions retains its existing inline emission.

The two range failure blocks in dec_SearchRequest/2 match OTP 29.1.1 after omitting label numbers. Both use the release source locations and call_ext_last with the eight-slot frame. The function drops from 132 to 128 instructions against 134 in OTP. The change removes ten instructions across four LDAP decoders. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Search range blocks](search-range-oracle.txt), [instruction deltas](decoder-instruction-delta.txt).

The managed image drops from 102,608 bytes and 8,106 instructions to 102,584 bytes and 8,096 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses range tests, deferred decoder failure blocks and the existing external tail-call opcode. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part685-final-tool`. Run `dotnet artifacts/beam-backend/part685-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part685-final.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align the search decoder Y-slot layout and label order.

All three focused checks passed on the final build. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
