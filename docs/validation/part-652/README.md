# Part652 LDAP deferred field projection

The C# emitter recognizes a checked tuple followed by local pair-producing calls and enum cases that consume its fields. It validates tuple shape before allocating the frame, keeps the input tuple in one Y slot and projects each field just before use. The lowering requires fresh bindings, proven pair returns, and a suffix that cannot access the deferred field names. The mismatch path retains the original tuple and raises badmatch. This follows the tuple projection sinking described in OTP-29.1.1 lib/compiler/src/beam_ssa_opt.erl.

enc_SearchRequest/2 decreases from 217 to 191 instructions and from 30 to 20 Y slots. The release uses 155 instructions and 8 Y slots. The remaining gap includes keeping pair results intact across calls, enum branch layout and building the final list and length. The two previously aligned choice encoders still match after normalizing line instructions and local labels. [Register comparison](search-registers-oracle.txt), [instruction comparison](choice-encoder-oracle.txt).

The generated module has 9,675 instructions against 7,028 and is 107,092 bytes against 99,008. After normalization 121 of 274 functions still differ. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code, ExpT, LocT, LitT, Attr, Dbgi, Docs, Line and Type still differ. The C# ASN.1 writer restores the OTP root type headings and wrapper line breaks. The two enc_LDAPMessage form coordinates now match and 251 top-level coordinates still differ, down from 253. [Chunk comparison](chunks-oracle.txt), [coordinate comparison](coordinates-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter and AST traversal. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part652-final-tool` and ran `dotnet artifacts/beam-backend/part652-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part652-final.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused compiler checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. All 1,366 corpus entries were checked, with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
