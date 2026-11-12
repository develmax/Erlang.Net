# Part663 LDAP runtime helper coordinates and literal folding

The ASN.1 writer emits the OTP runtime heading and multiline value and suppression forms. All 300 top-level form coordinates now match, including the 25 remaining mismatches from Part662. Full abstract form annotations still differ.

The C# emitter folds a tagged tuple to a literal when a local call can only return the proven constant or raise. It still emits the call. This restores the unbindRequest literal and keeps input validation in decode_null/2. The emitter also reuses OTP-style constant move ordering for local calls whose arguments are all constants. The 111 terms now appear in the same order and LitT is byte-exact. [Top-level coordinates](coordinates-oracle.txt), [literal comparison](literals-oracle.txt).

Docs and LitT now match byte for byte, alongside AtU8, ImpT, StrT, CInf and Meta. The generated module is 108,284 bytes against 99,008. Seven sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing C# writer and emitter are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part663-verified-tool` and ran `dotnet artifacts/beam-backend/part663-verified-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part663-verified.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Corpus report](otp-corpus.json).

All three focused checks passed. The initial fixture used a standalone case outside the supported emission profile. The corrected fixture exercises the same constant result and failure through the supported match-and-case form. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. [Initial focused report](focused-initial.json), [final focused report](focused.json).
