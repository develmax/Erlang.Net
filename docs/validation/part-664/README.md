# Part664 LDAP nested source coordinates and local tail-call lines

The C# ASN.1 writer follows the OTP column layout for wrappers, choice branches, aliases, collection helpers, enum error expressions and sequence fields. The info function uses the same MODULE macro as OTP. All nested and top-level source coordinates now match. The prior build differed in 2,678 coordinates across 140 forms. Dbgi is byte-exact. [Prior nested coordinates](prior-nested-coordinates-oracle.txt), [final nested coordinates](nested-coordinates-oracle.txt).

The C# general emitter omits the call-site line before local tail calls, following OTP beam_ssa_codegen.erl. The managed instruction count drops from 9,548 to 9,502 against 7,028. The number of line instructions drops from 1,041 to 995 against 862. Line and Type still differ. [Chunk comparison](chunks-oracle.txt), [metadata comparison](metadata-oracle.txt).

The generated module is 108,096 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi match byte for byte. Code, ExpT, LocT, Attr, Line and Type remain different.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# writer and emitter. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part664-verified-tool` and ran `dotnet artifacts/beam-backend/part664-verified-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part664-verified.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
