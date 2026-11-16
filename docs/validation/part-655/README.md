# Part655 LDAP register-only length accumulation

The C# emitter recognizes a suffix that constructs a list from proven pair members and adds other pair members in source order. It builds the list in X1, keeps the running sum in X2 and loads each next operand into X3. A single source location covers a chain of additions on the same source line. The tail call consumes the list and sum directly. Other suffixes keep the general path.

enc_SearchRequest/2 decreases from 173 to 149 instructions and from 12 to 9 Y slots. OTP uses 155 instructions and 8 slots. The arithmetic operand types, register numbering and instruction placement still differ. The previously aligned choice encoders still match after normalizing source lines and local labels. [Instruction comparison](choice-encoder-oracle.txt), [register comparison](search-registers-oracle.txt).

The managed function now has 39 move and 19 line instructions against 46 and 18 in OTP. The counts of heap checks, tuple projections, cons operations and arithmetic BIFs still match.

The generated module has 9,561 instructions against 7,028 and is 106,664 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter, the exact pair arity proof and BEAM arithmetic instructions. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part655-tool` and ran `dotnet artifacts/beam-backend/part655-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part655.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused compiler checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked all 1,366 modules, reporting 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
