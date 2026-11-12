# Part670 LDAP direct unwrap-to-dispatch forwarding

The C# emitter applies the verified singleton unwrap directly to the first local call result when no outer case branch reads or matches the original binding. It saves the normalized value once for fallback handling and uses the copy already in X0 for tagged dispatch. When a branch uses the original binding, the emitter retains it. The regression covers that fallback.

The call and singleton check now run consecutively as in OTP. A saved copy of the normalized value remains before the tuple test. The LDAP module drops from 8,967 to 8,949 instructions against 7,028. It is 105,904 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Prefix comparison](forwarded-prefix-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing C# emitter and AST traversal are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part670-tool` and ran `dotnet artifacts/beam-backend/part670-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part670.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
