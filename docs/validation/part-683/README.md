# Part683 LDAP terminal field result

A list decoder can leave its final field result in X0 when the next operation returns a tuple of that result, existing bindings and constants. The emitter uses get_hd when the remaining tail is unused or examined only by an unconditional no-op case. It retains the original nonempty-list check and badmatch value. The fixture exercises an ignored improper tail, an empty-list failure and a tuple that uses the result twice.

The change removes 11 instructions across 11 LDAP decoders. dec_SearchRequest/2 now uses 131 instructions and eight stack slots, compared with 132 instructions and nine slots before this change. OTP uses 134 instructions and eight slots. Slot numbering, frame trimming and error block placement still differ. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Search decoder disassembly](dec-search-oracle.txt), [instruction deltas](decoder-instruction-delta.txt).

The managed image drops from 102,628 bytes and 8,106 instructions to 102,576 bytes and 8,095 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses list forwarding, the no-op case check and existing tuple operands. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part683-tool`. Run `dotnet artifacts/beam-backend/part683-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part683.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align slot numbering, trimming and range failure placement. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
