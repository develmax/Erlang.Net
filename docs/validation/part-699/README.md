# Part699 LDAP generated list comprehension helpers

The C# emitter recognizes the closed mapping loop with one element argument and one constant argument. It uses one slot for the tail and mapped value, swaps the registers before recursion and leaves empty and invalid inputs frameless. The generated helper takes its source location from the comprehension rather than the enclosing function. The fixture covers order, an empty list and bad_generator on an improper tail.

All seven affected LDAP helpers match OTP 29.1.1 after renumbering labels. Each drops from 37 instructions to 22 and uses one slot instead of five. The managed image drops from 101,704 bytes and 7,849 instructions to 101,400 bytes and 7,744 instructions. OTP has 99,008 bytes and 7,028 instructions. Line now has the same payload length as OTP but its bytes still differ. Code, ExpT, LocT, Attr, Line and Type differ. [Comparison](comprehension-normalized-oracle.txt), [all affected functions](comprehension-delta.txt), [chunks](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses existing list mapping, frame and error emission. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part699-tool` and ran `dotnet artifacts/beam-backend/part699-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part699.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
