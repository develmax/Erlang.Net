# Part681 LDAP list forwarding through enum and range checks

The C# emitter forwards the consumed list head directly into the local call that feeds an enum or inclusive range check. The enum and range lowerings still own their validation and result binding. A nonmatching form uses ordinary expression emission. The regression fixture covers a known and an unknown enum, an in-range float, a range exit and an improper tail.

The change removes 44 instructions across eight LDAP functions. The managed search decoder drops from 160 instructions and 20 stack slots to 144 instructions and 12 slots. OTP uses 134 instructions and eight slots. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Search decoder disassembly](dec-search-oracle.txt), [instruction deltas](decoder-instruction-delta.txt).

The managed image drops from 103,604 bytes and 8,386 instructions to 103,416 bytes and 8,342 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the existing list forwarding, selector and range emission. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part681-tool`. Run `dotnet artifacts/beam-backend/part681-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part681.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align the initial tag match, final tuple and remaining slot allocation in the search decoder. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
