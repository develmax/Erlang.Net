# Part668 LDAP tagged branch argument forwarding

The C# emitter extracts the payload of a proven two-element tuple into X2 before dispatch. Eligible branches forward X2 directly to the local decoder and load the constant argument in OTP order. Other branches can save the payload to their existing slot. The fallback still sees the original input, including tuples with the wrong arity and non-tuples.

The bindResponse call setup in dec_LDAPMessage_protocolOp/2 matches OTP 29.1.1. The generated LDAP module drops from 9,037 to 9,011 instructions against 7,028. The image is 106,104 bytes against 99,008. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type still differ. [Call setup comparison](tagged-call-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The existing emitter and constant call result proof are reused. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part668-tool` and ran `dotnet artifacts/beam-backend/part668-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part668.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
