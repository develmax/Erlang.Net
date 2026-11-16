# Part636 LDAP tag selection and temporary register reuse

The general C# BEAM emitter recognizes case expressions with distinct homogeneous atom or integer keys and a final unconditional variable pattern. It also recognizes two-element tagged tuples with an unbound field variable. It checks tuple shape once, then uses select_val. Unknown keys and wrong shapes enter the same fallback. Cases with guards, duplicate keys or prebound fields retain the existing lowering.

The mutually exclusive branches share temporary Y slots. They keep outer bindings intact and leave their result in X0. The managed execution regression covers both tagged branches, a wrong tuple arity, an unknown tag and a non-tuple.

OTP 29.1.1 loaded the generated image and matched 320 direct calls, 83 round trips, 88 BER boundary calls, info/0 and two unknown-type reasons. The two outer dispatchers retain their 162 instructions each and matching forwarding behavior. Both builds and all three focused checks passed.

The entire module now has 13,922 instructions against 14,370 in Part635 and 7,028 in OTP. dec_LDAPMessage_protocolOp/2 fell from 457 to 375 instructions and from 116 to 12 Y slots. OTP uses 219 instructions and no Y slots for that function. The generated file is 67,648 bytes against 99,008 in the release. Dbgi, Docs, Line and Type remain different. See [stack frames](frame-oracle.txt), [instruction comparison](disassembly-oracle.txt) and [chunks](chunks-oracle.txt).

The implementation reuses the C# emitter and the OTP select_val instruction already used by the forwarding dispatcher. The reference remains OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency or license change.

Corpus command uses dotnet artifacts/beam-backend/part636-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part636.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. Both builds used dotnet build -m:1 --no-restore -v:q. The saved oracle expressions use the installed VM and generated release-profile image. The complete test harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The final corpus processed all 1,366 files. 18 matched, 13 differed, 488 were rejected at source admission and 847 reached unsupported lowering. [Per-image report](otp-corpus.json), [focused tests](focused.json).
