# Part639 LDAP abstract debug forms

The C# debug writer now handles the generated LDAP guards, operators, alias and binary patterns, blocks, conditionals, three-part catch handlers and list comprehensions. The LDAP release profile enables full abstract code and records the ordered release debug options. The managed regression requires all declared LDAP functions in Dbgi.

The installed OTP compiler compiled the same generated Erlang source as a comparison oracle. All 271 abstract forms match after excluding the file attribute, including all 261 functions and their positions. The ordered debug options match the installed release. The generated Dbgi grows from 66 to 51,134 bytes. The release Dbgi has 54,262 bytes and 300 forms. Its source includes 23 record declarations, a compile attribute, two extra export forms and two extra file attributes. Function source positions still differ between generated and release source. [Same-source comparison](debug-oracle.txt), [release form inventory](debug-inventory.txt).

The image is 108,828 bytes against 99,008 in OTP. Code remains at 11,032 instructions against 7,028 in OTP. Docs, Line and Type remain different. OTP matched 320 direct calls, 83 round trips, 88 BER boundary calls, info/0 and both unknown-type reasons. CInf, Meta and StrT remain byte-exact. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# syntax model, metadata writer and ETF compressor. There are no dependency or license changes. OTP is only the comparison oracle.

Both builds use dotnet build -m:1 --no-restore -v:q. The corpus command is dotnet artifacts/beam-backend/part639-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part639.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The saved oracle expressions use the installed VM and generated image. The complete harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

Both builds and all three focused checks passed. The full corpus recorded 18 Equal, 13 Different, 488 SourceRejected and 847 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused report](focused.json).
