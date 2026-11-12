# Part660 LDAP call setup and relative source lines

The C# emitter saves enum results in their success branches and schedules the save of a direct result after the next tuple projection when the following operation is another direct call. Input slot reuse requires both final operations to be direct calls. The ASN.1 writer restores per-field separators and multiline enum branches without changing the parsed forms.

The normalized enc_SearchRequest/2 instruction prefix through the last field call matches OTP 29.1.1, including the saved registers and branch-local moves. All release-relative line offsets match. The C# emitter also records offset 56 for the final tail call. The absolute start is 1097 against 1635 in the release. Arithmetic operands remain untyped. The function uses 156 instructions against 155 and eight Y slots in both images. [Call setup and line offsets](search-calls-oracle.txt), [instruction comparison](choice-encoder-oracle.txt).

The generated module is 107,696 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. The Dbgi, Docs and Line sizes are closer to OTP. Parsed abstract forms and debug options still match after removing annotations. [Chunk comparison](chunks-oracle.txt), [abstract form comparison](all-forms-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# emitter and BER writer and follows the OTP constructed BER field layout. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part660-tool` and ran `dotnet artifacts/beam-backend/part660-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part660.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
