# Part671 LDAP fallback-only input preservation

The C# emitter keeps a computed tuple in X0 through tagged dispatch. It saves the original input only on the failure edge when a branch needs a bound variable. A fallback that returns the input or passes it directly to a local call can use X0 without a frame transfer. Cases that overwrite X0 keep their existing saved input.

The dec_LDAPMessage_protocolOp/2 prefix from match_tags/2 through both tuple projections matches OTP 29.1.1 after excluding label numbering. The managed image drops from 8,949 to 8,943 instructions against 7,028. It is 105,880 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type still differ. [Prefix comparison](dispatch-prefix-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing C# emitter and tagged result helper are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part671-final-tool` and ran `dotnet artifacts/beam-backend/part671-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part671-final.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
