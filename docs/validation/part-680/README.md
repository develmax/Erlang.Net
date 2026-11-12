# Part680 LDAP list head forwarding

For a top-level decoder sequence, a proven fresh list head consumed only by the next local decoder can go directly into X0. The emitter preserves the tail in a stack slot and reuses the input slot when neither its binding nor an alias can be read later. It emits separate failure blocks that retain the original badmatch value and source location. The fixture covers successful decoding, a live input list, an empty list and an improper tail.

The change removes 144 instructions across 20 functions. The managed search decoder drops from 176 instructions and 28 stack slots to 160 instructions and 20 slots. OTP uses 134 instructions and eight slots. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Search decoder disassembly](dec-search-oracle.txt), [instruction deltas](decoder-instruction-delta.txt).

The managed image drops from 104,212 bytes and 8,530 instructions to 103,604 bytes and 8,386 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# emitter reuses the expression walker, pattern checks and existing BEAM list and call opcodes. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part680-tool`. Run `dotnet artifacts/beam-backend/part680-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part680.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, forward list heads through enum and range decoders and align the last tuple construction. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
