# Part656 LDAP final pair placement

The C# emitter recognizes the register-only suffix before assigning pair slots. The final call result stays in X0 through list construction and length arithmetic. When the final field is read by a direct call and the suffix cannot access the input tuple, the emitter reads that field into X1 before replacing the tuple with the penultimate result.

enc_SearchRequest/2 uses eight Y slots, matching OTP 29.1.1. The instruction count remains 149 against 155 in OTP. Register numbering, instruction order, line information and arithmetic operand types still differ. The previously aligned choice encoders still match after normalizing source lines and local labels. [Instruction comparison](choice-encoder-oracle.txt), [register comparison](search-registers-oracle.txt).

The generated module has 9,561 instructions against 7,028 and is 106,652 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# emitter and existing pair and suffix proofs. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part656-tool` and ran `dotnet artifacts/beam-backend/part656-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part656.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused compiler checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The final corpus checked all 1,366 modules, reporting 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
