# Part653 LDAP deferred pair results

The C# emitter keeps a proven two-element result in one Y slot until a consumer reads a member. List construction extracts each required member directly into an X register. The lowering declines a suffix that pattern matches an already bound pair member, preserving exact equality and badmatch behavior through the general path.

The enc_SearchRequest/2 frame decreases from 20 to 13 Y slots against 8 in OTP 29.1.1. The instruction count remains 191 against 155. The generated module is 107,044 bytes against 99,008, with 121 of 274 functions still differing after normalizing line instructions and local labels. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Register comparison](search-registers-oracle.txt), [instruction comparison](choice-encoder-oracle.txt), [chunk comparison](chunks-oracle.txt).

Both versions have 24 get_tuple_element instructions. The managed image still has 69 moves against 46 in OTP and seven heap checks against three.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# emitter and the previously verified tuple return proof. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part653-tool` and ran `dotnet artifacts/beam-backend/part653-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part653.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused compiler checks passed, including a regression for matching an already bound pair member. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported across 1,366 files. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
