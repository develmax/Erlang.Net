# Part684 LDAP terminal frame trimming

When the terminal field reads a list head from Y0 and the result tuple needs no value from that slot, the C# emitter removes the slot before the decoder call. Tuple operands and the final deallocation use the reduced frame. The list match failure branches retain their original frame and source value. The fixture covers normal return, an ignored improper tail and a badmatch before trimming.

All 11 affected LDAP decoders emit the same trim operands as OTP 29.1.1. dec_SearchRequest/2 allocates eight slots and trims to seven before the final call. It now uses 132 instructions against OTP's 134. The first two tuple field slots are still reversed relative to OTP and the range failure blocks still differ. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison pass. [Trim operands](trim-oracle.txt), [search decoder disassembly](dec-search-oracle.txt).

The managed image is 102,608 bytes and 8,106 instructions, up from 102,576 bytes and 8,095 instructions because the 11 trim instructions were added. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the existing trim opcode and tuple operand writer. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part684-tool`. Run `dotnet artifacts/beam-backend/part684-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part684.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align range failure placement and the first two search decoder result slots.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
