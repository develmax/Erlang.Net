# Part669 LDAP singleton unwrapping and decoder entry

The C# emitter unwraps a verified singleton case in X registers, retaining the original input for other shapes. The decoder entry forwards both arguments directly to its first local call when the nested case cannot read or match those arguments later. This avoids the initial argument slots and the unreachable clause fallback.

The singleton instruction sequence in dec_LDAPMessage_protocolOp/2 matches OTP 29.1.1. The module drops from 9,011 to 8,967 instructions against 7,028 and is 105,960 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type still differ. [Singleton comparison](singleton-prefix-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing C# emitter and case pattern helpers are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part669-final-tool` and ran `dotnet artifacts/beam-backend/part669-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part669-final.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
