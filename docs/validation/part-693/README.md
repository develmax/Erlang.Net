# Part693 LDAP BER octet accumulator

The C# emitter recognizes a zero base case followed by a recursive byte shift and mask. It keeps the value and accumulator in X registers and emits a frameless tail call. It preserves the incoming tail value and leaves the input untyped. The fixture covers multiple octets, the zero case and an improper accumulator tail.

The managed minimum_octets/2 drops from 31 instructions to 15, the OTP 29.1.1 count. After renumbering labels, the remaining differences are operand type annotations for the input and recursive branch. The nonnegative BER length input proof through encode_tags remains open. [Comparison](octets-normalized-oracle.txt), [disassembly](octets-oracle.txt).

The managed image is 101,988 bytes and 7,938 instructions, down from 102,040 bytes and 7,954 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses arithmetic, list construction and tail-call emission. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part693-tool` and ran `dotnet artifacts/beam-backend/part693-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part693.json` using the installed release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
