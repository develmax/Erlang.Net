# Part682 LDAP decoder entry and return

The C# emitter forwards unchanged incoming arguments directly to an initial local reader when the rest of the decoder does not reference their bindings. If the initial list is dead after the split, its result stays in X0. The first badmatch edge receives the original value in X0. A fresh tuple that is bound and immediately returned is constructed directly in the return register. The fixture covers the normal path, an empty list and a non-list reader result.

The change removes 236 instructions across 23 LDAP functions. The managed search decoder drops from 144 instructions and 12 stack slots to 132 instructions and nine slots. OTP uses 134 instructions and eight slots. Registers, failure blocks and the final tuple still differ. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Search decoder disassembly](dec-search-oracle.txt), [instruction deltas](decoder-instruction-delta.txt).

The managed image drops from 103,416 bytes and 8,342 instructions to 102,628 bytes and 8,106 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses existing list forwarding, frame allocation and tuple emission. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part682-tool`. Run `dotnet artifacts/beam-backend/part682-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part682.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, match the search decoder's Y-slot layout and move its range failures out of line. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The initial build exposed an ambiguous C# type pattern, which was corrected before validation. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
