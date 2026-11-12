# Part665 LDAP decoder result forwarding

The C# emitter passes a local call result directly to binary_to_list/1 in X0 and shares the source line when both calls have the same location. A case whose branches return exact pairs can omit the repeated tuple check when the match introduces fresh variables. The managed fixture exercises both branches.

The LDAP instruction count drops from 9,502 to 9,181 against 7,028. Line instruction references drop from 995 to 916 against 862. dec_ExtendedResponse/2 decreases from 293 to 260 instructions against 216. The generated module is 106,884 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type still differ. [Instruction comparison](instruction-gaps-oracle.txt), [metadata comparison](metadata-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing C# emitter and exact tuple proof are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part664-delivery-tool` and ran `dotnet artifacts/beam-backend/part664-delivery-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part664-delivery.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
