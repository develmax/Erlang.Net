# Part666 LDAP case pair destinations

The C# emitter can write the members of a two-branch case directly to the slots of a fresh pair match. It evaluates the members in source order and keeps each branch's bindings separate until the join. It reuses the generic case pattern and failure handling. Entirely constant tuple branches keep the existing path.

The LDAP instruction count drops from 9,181 to 9,076 against 7,028 in OTP 29.1.1. Code shrinks from 35,684 to 35,132 bytes. The generated module is 106,332 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type still differ. [Instruction comparison](instruction-gaps-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter and exact pair proof. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part666-tool` and ran `dotnet artifacts/beam-backend/part666-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part666.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
