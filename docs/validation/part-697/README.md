# Part697 LDAP signed octet input types

The C# emitter proves the sign of private signed octet accumulators by checking every local entry, the dispatch comparison and the integer guard on the dispatch caller. The recursive right shift preserves the sign. Exported functions and unproven call paths do not receive the sign annotation. The fixture exercises a guarded dispatch and retains an unproven caller with a fractional accumulator head.

All 18 instructions in each of encode_integer_pos/2 and encode_integer_neg/2 match OTP 29.1.1 after renumbering labels. The managed image grows from 101,740 bytes to 101,760 bytes as typed operands and their table entries are emitted. The instruction count remains 7,864 against 7,028 in the 99,008-byte release image. Code, ExpT, LocT, Attr, Line and Type differ. [Positive comparison](positive-normalized-oracle.txt), [negative comparison](negative-normalized-oracle.txt), [chunks](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the local call scan, octet recursion and typed operand writer. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part697-tool` and ran `dotnet artifacts/beam-backend/part697-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part697.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
