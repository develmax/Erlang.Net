# Part696 LDAP signed integer octet accumulation

The C# emitter recognizes the signed termination clauses and reuses the frameless byte-shift and mask loop. For a private function whose only nonrecursive calls start with an empty accumulator, the head of a nonempty accumulator is proven to be a byte. The emitter uses the OTP integer comparison on that path and preserves the original Erlang comparison for other callers. The fixture covers 128, -129 and a fractional accumulator head.

Both encode_integer_pos/2 and encode_integer_neg/2 use 18 instructions, down from 34. After renumbering labels only the sign-related input annotations differ from OTP 29.1.1. The managed image drops from 101,844 bytes and 7,896 instructions to 101,740 bytes and 7,864 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type differ. [Positive comparison](positive-normalized-oracle.txt), [negative comparison](negative-normalized-oracle.txt), [chunks](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The change reuses the octet step emitter, typed registers and the source call walker. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part696-tool` and ran `dotnet artifacts/beam-backend/part696-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part696.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
