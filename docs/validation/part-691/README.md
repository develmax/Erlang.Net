# Part691 LDAP BER octet list type

The existing local call fact analysis now tracks proper lists through empty literals, list constructors and recursive call contexts. The BER length emitter annotates the length/1 operand only when the octet function is proven to return a proper list. An improper list stays untyped and still raises badarg. The fixture exercises this path.

In encode_length/1 the length/1 operand now matches OTP 29.1.1. After renumbering labels, the input nonnegative-integer annotation is the only remaining difference across the 30 instructions. That annotation requires a proof through encode_tags and its callers. [Length comparison](length-normalized-oracle.txt), [disassembly](encode-length-oracle.txt).

The managed image remains 102,096 bytes and 7,973 instructions. The Code payload grows by two bytes and the IFF alignment absorbs the difference. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# change reuses the existing recursive fact analysis and typed operand writer. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part691-tool`. Run `dotnet artifacts/beam-backend/part691-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part691.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
