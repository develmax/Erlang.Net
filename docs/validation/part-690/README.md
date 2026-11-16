# Part690 LDAP BER length encoding

The C# emitter recognizes the short and long BER length clause family. It returns the short form without a frame and keeps the octet list and computed length in X registers on the long path. The arithmetic operands carry the range proven by the length guard. The source test covers 127, 128 and a float in the short clause, preserving the original Erlang comparison semantics.

The generated encode_length/1 drops from 56 instructions to 30, the same count as OTP 29.1.1. After renumbering labels, the remaining instruction differences are the input nonnegative-integer annotation and the list annotation on the length operand. Those types are not assumed by this lowering. The earlier encoder and decoder instruction comparisons still pass. [Length comparison](length-normalized-oracle.txt), [disassembly](encode-length-oracle.txt).

The managed image drops from 102,148 bytes and 7,999 instructions to 102,096 bytes and 7,973 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# lowering follows the BER length branches in asn1rtt_ber.erl and reuses existing operand, tuple and call writers. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part690-tool`. Run `dotnet artifacts/beam-backend/part690-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part690.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, prove the input and octet list types. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
