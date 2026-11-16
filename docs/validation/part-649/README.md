# Part649 LDAP scalar encoder instruction parity

The C# emitter lowers atom dispatch with constant payloads and binary or list measurement followed by a local tail call. The two forms match the pinned OTP-29.1.1 instruction sequences for encode_boolean/2 and encode_restricted_string/2 after normalizing source lines and local labels. The managed regression covers both literal values, binary and list lengths, tagged exit payloads, and function_clause.

The managed LDAP image has 10,575 instructions across 274 functions against 7,028 instructions across 274 functions in the release. It occupies 109,872 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code, register layout, Dbgi, Docs, Line, Type and other chunks still differ. encode_null/2 differs in the order of two moves and encode_integer/2 still uses 37 instructions against 19. [Encoder comparison](encoder-oracle.txt), [chunk comparison](chunks-oracle.txt).

The OTP behavior checks matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. Abstract forms and debug options match after removing source coordinates. The release reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency or license changes.

Built the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part649-tool` and ran `dotnet artifacts/beam-backend/part649-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part649.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json.

All three focused compiler checks passed. The corpus reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported across 1,366 installed modules. The full runtime test harness and canonical readiness regeneration remain unrun. [Corpus report](otp-corpus.json), [focused checks](focused.json).
