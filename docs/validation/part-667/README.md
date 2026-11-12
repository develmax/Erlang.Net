# Part667 LDAP tagged call results in X0

The C# emitter builds a two-element tagged tuple directly from the result in X0 when its payload is a call or block. It keeps X0 live across the heap check. Constant-return folding keeps precedence and guard emission keeps its existing path. The managed fixture checks a local call returning a list payload.

The bindResponse branch in dec_LDAPMessage_protocolOp/2 now matches the OTP call, heap check and tagged tuple sequence. The module drops from 9,076 to 9,037 instructions against 7,028 in OTP. The generated image is 106,212 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type still differ. [Tagged tuple comparison](tagged-pair-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing C# emitter and literal operand writer are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part667-tool` and ran `dotnet artifacts/beam-backend/part667-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part667.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
