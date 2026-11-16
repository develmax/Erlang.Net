# Part675 LDAP protected encoder values

The C# emitter forwards a proven tuple projection directly in X0 to iolist_to_binary/1. An unconditional try-of clause that wraps its fresh result variable in a tagged pair can build that pair in X0 without a spill or an unreachable try_case_end. The generic catch path matches three-part catch patterns against the class, reason and stack registers it already saved. It only constructs a catch tuple for patterns that require the entire tuple. The fixture tests both successful iolist conversion and a badarg with a stack.

The protected success sequence in encode/2 matches OTP from the local dispatch call through construction of the result tuple after normalizing the try token register. The managed function drops from 93 instructions and 18 frame slots to 77 instructions and 13 slots. OTP uses 43 instructions and one slot. The remaining discrepancy includes the catch class dispatch, reason projection and stacktrace construction. The 219-instruction protocol operation decoder still matches after renumbering labels. [Protected success comparison](encode-success-oracle.txt), [complete function comparison](encode-protected-oracle.txt), [decoder comparison](decoder-normalized-oracle.txt).

The managed image falls from 105,820 bytes and 8,959 instructions to 105,696 bytes and 8,934 instructions. The release image has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The OTP SSA code generator extracts catch values from the exception registers. The C# path reuses the existing projection proof, call emitter and pattern matcher. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part675-tool`. Run `dotnet artifacts/beam-backend/part675-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part675.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, use the exception class and raw stack registers directly before building a stacktrace on a matching branch.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
