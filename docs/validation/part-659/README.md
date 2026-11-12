# Part659 LDAP list construction schedule

The C# emitter schedules the final pair-list construction when the first two list elements share their pairs with the first two sum operands and the last pair remains in X0. It checks the heap before extraction, assigns separate registers to the heads and the first two lengths, and builds the list from the end with a literal nil tail. It preserves the source addition order and uses the generic schedule if the register assignment would reach the loader-reserved X register.

The normalized enc_SearchRequest/2 list-construction sequence now matches OTP 29.1.1, including the heap check, tuple projections, registers and all eight put_list instructions. SearchRequest has 148 instructions against 155 and still uses eight Y slots. Arithmetic operand types, earlier call setup and source annotations still differ. [List comparison](search-list-oracle.txt), [instruction comparison](choice-encoder-oracle.txt).

The generated module has 9,550 instructions against 7,028 and is 106,596 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter and follows the OTP instruction schedule and X-register bound in beam_validator.erl. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part659-final-tool` and ran `dotnet artifacts/beam-backend/part659-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part659-final.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The final compiler reproduced the inspected LDAP image by SHA-256. The full corpus checked 1,366 modules and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
