# Part641 LDAP public API forms and try success clauses

The C# BEAM emitter supports try/of/catch without after. It ends the protected region before matching the success clauses. A missing match emits try_case_end. The managed regression checks a matching value, an unmatched value, and propagation of an exception from an of body to an outer handler. The debug writer serializes the success clauses with their annotations.

The generated LDAP public API uses the OTP encode and decode forms. Ignoring coordinates, 178 of 261 function forms now match the installed release, up from 176. encode/2 and decode/2 are the two additions. A same-source OTP compile matches all 297 non-file forms including coordinates. The 23 header record declarations and file transitions still match the release. [Function comparison](public-api-oracle.txt), [same-source comparison](debug-oracle.txt).

The generated image has 11,044 instructions against 11,032 in Part640 and 7,028 in OTP. The generic try/of lowering adds instructions. The module remains Different at 110,204 bytes against 99,008. Dbgi, Docs, Line and Type still differ. OTP matched 320 direct calls, 83 round trips, 88 boundary cases, info/0 and two unknown-type reasons. Both builds and all three focused checks passed.

Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The generator follows the pinned ASN.1 BER API template and reuses the C# emitter and abstract form writer. No dependency or license changes. The installed OTP VM is the development oracle.

Both builds used dotnet build -m:1 --no-restore -v:q. The corpus command was dotnet artifacts/beam-backend/part641-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part641.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The complete harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus processed 1,366 modules. 18 matched, 19 differed, 488 were rejected during source admission and 841 reached unsupported lowering. [Corpus report](otp-corpus.json), [focused tests](focused.json).
