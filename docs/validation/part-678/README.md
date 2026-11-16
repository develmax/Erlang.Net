# Part678 LDAP optional field pairs

The compiler recognizes optional tuple fields whose absent branches return the same constant pair and whose present branch calls a local encoder proven to return a pair. It emits the constant pair directly on the absent path and keeps each result as one value until its members are needed. A two-value default check uses ordered select_val. The final field can reuse X0 and clears the input tuple stack slot before the last encoder call when the tuple is dead. The fixture exercises one-value and two-value defaults and the present path.

All 68 instructions in enc_MatchingRuleAssertion/2 match OTP 29.1.1 after label renumbering. Previously the managed function used 117 instructions and 19 stack slots. Both versions now use five slots. The compiler emits 316 fewer instructions across 11 functions. The instruction lists for enc_Control/2 and enc_PasswdModifyRequestValue/2 also match after renumbering labels. The exact encode/2 and decode/2 comparisons and the normalized protocol operation decoder comparison still pass. [Matching rule comparison](matching-normalized-oracle.txt), [instruction deltas](optional-instruction-delta.txt).

The managed image drops from 105,452 bytes and 8,862 instructions to 104,256 bytes and 8,546 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# emitter reuses tuple return proof, deferred tuple fields, the existing select opcode and ordered terms, and stack slot initialization. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part678-tool`. Run `dotnet artifacts/beam-backend/part678-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part678.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. The largest remaining instruction surplus is dec_SearchRequest/2, followed by match_tags/2. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
