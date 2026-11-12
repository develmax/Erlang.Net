# Part638 LDAP call argument register allocation

The general C# BEAM emitter loads literal arguments directly into X registers for regular and local tail calls. It evaluates other arguments in source order and retains their results across nested calls. After ordinary calls, temporary Y slots can be reused while slots referenced by existing or newly bound variables stay reserved. The planner tracks the highest slot used across all paths. The managed regression exercises nested call arguments, literal forwarding and a binding retained after the call.

The generated ELDAPv3 image has 11,032 instructions against 12,402 in Part637 and 7,028 in OTP. The Code payload is 41,339 bytes against 46,252 previously and 27,815 in OTP. The enc_LDAPResult/2 body fell from 646 to 406 instructions and from 28 to 20 Y slots. OTP uses 374 instructions and 4 Y slots there. All 274 function identities and order still match. The generated image is 57,760 bytes against the release's 99,008. See [frame comparison](frame-oracle.txt), [instruction gaps](instruction-gaps.txt) and [chunk comparison](chunks-oracle.txt).

OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type reasons. Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter. No dependency or license changes. Dbgi still carries a stub. Docs, Line and Type still differ.

Both builds used dotnet build -m:1 --no-restore -v:q. The corpus command was dotnet artifacts/beam-backend/part638-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part638.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The saved oracle expressions use the installed VM and generated release-profile image. The full harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

All three focused checks passed. The corpus processed all 1,366 modules with 18 Equal, 13 Different, 488 SourceRejected and 847 Unsupported. [Corpus report](otp-corpus.json), [focused report](focused.json).
