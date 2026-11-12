# Part657 LDAP frame slot numbering

The C# emitter assigns slots from the top of the frame for the proven register-only suffix that reuses the input tuple slot. The two highest slots are filled from the arguments before any call. Only the lower slots need init_yregs. Other general frames continue to initialize all their slots.

The enc_SearchRequest/2 prologue now matches OTP for allocation, initialization of Y0 through Y5 and argument moves into Y6 and Y7. The tuple failure label still has a different number. The function has eight Y slots and 149 instructions against eight slots and 155 instructions in OTP. Arithmetic types, instruction ordering and source annotations still differ. [Prologue comparison](search-prologue-oracle.txt), [register comparison](search-registers-oracle.txt).

The generated module has 9,561 instructions against 7,028 and is 106,604 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# emitter, the existing pair lifetime proof and OTP frame initialization rules. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part657-tool` and ran `dotnet artifacts/beam-backend/part657-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part657.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
