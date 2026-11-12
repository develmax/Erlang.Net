# Part654 LDAP enum dispatch and tuple slot reuse

The C# emitter dispatches proven enum field cases directly from X0, schedules constant arguments with the existing OTP move ordering, and emits a tagged exit on the failure edge. The final pair can reuse the input tuple slot when the suffix neither reads nor matches the input. The fixture checks successful enum selection and the original invalid enum payload.

enc_SearchRequest/2 decreases from 191 to 173 instructions and from 13 to 12 Y slots against 155 instructions and 8 slots in OTP. The previously aligned choice encoders still match after normalizing source lines and local labels. [Instruction comparison](choice-encoder-oracle.txt), [register comparison](search-registers-oracle.txt).

The instruction count difference is now accounted for by 11 additional moves and seven additional line instructions. Both images use three heap checks, 24 tuple projections, eight cons operations and seven gc_bif instructions.

The generated module has 9,648 instructions against 7,028 and is 106,956 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter, tuple lifetime analysis and constant move ordering. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part654-tool` and ran `dotnet artifacts/beam-backend/part654-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part654.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused compiler checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The final corpus checked all 1,366 modules, reporting 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
