# Part658 LDAP SearchRequest control-flow labels

The C# emitter assigns success branch and join labels first, followed by enum failure blocks in reverse order and then the tuple mismatch label. This matches the emitted OTP block order. The generic suffix starts allocating labels after the reserved range.

The filtered enc_SearchRequest/2 control flow matches OTP 29.1.1 after rebasing labels to the function start. The comparison covers label definitions, tests, select tables and jumps. The whole function still differs in arithmetic operand types, instruction scheduling and source annotations. It uses eight Y slots and 149 instructions against eight slots and 155 instructions in OTP. [Control-flow comparison](search-flow-oracle.txt), [instruction comparison](choice-encoder-oracle.txt).

The generated module is 106,604 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# emitter and follows the observed OTP code layout. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part658-tool` and ran `dotnet artifacts/beam-backend/part658-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part658.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
