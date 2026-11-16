# Part686 LDAP list decoder slot layout

For a consecutive list decoder prefix with a binary-to-list conversion followed by enum selection, the C# emitter assigns its reserved slots from high to low. When a list tail is consumed and has no live alias, the next tail is assigned its own slot and the old slot is cleared before the call. The decoded result then takes the cleared slot. Generic slot bookkeeping converts the physical indices back to allocation indices when computing live temporary limits.

All 134 instructions in dec_SearchRequest/2 match OTP 29.1.1 after renumbering labels, including the register allocation, slot initialization, type annotations, source locations and result tuple. The two range failure blocks still match. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Search decoder comparison](search-normalized-oracle.txt), [disassembly](dec-search-oracle.txt).

The managed image grows from 102,584 bytes and 8,096 instructions to 102,608 bytes and 8,102 instructions because six slot initializations are now emitted. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The reference pre-codegen pass reserves Y registers and computes live intervals. The C# path reuses the existing local frame planner, liveness checks and stack slot initialization. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part686-tool`. Run `dotnet artifacts/beam-backend/part686-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part686.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align label assignment and remaining functions, beginning with match_tags/2. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
