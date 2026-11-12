# Part694 LDAP BER match and collect

The compiler recognizes a two-argument local reader followed by a nonempty-list or binary case. It retains the reader result in X0, tail-calls the list collector and returns a binary directly. The parser records the case keyword location, including preprocessed file and line, so the case failure retains the original value and source location. The fixture covers the list, binary and case_clause paths.

All 16 instructions in match_and_collect/2 match OTP 29.1.1 after renumbering labels. The previous managed function used 34 instructions and five stack slots. The new path allocates an empty frame for the reader call. The BER length input type proof remains open. [Comparison](match-collect-normalized-oracle.txt), [disassembly](match-collect-oracle.txt).

The managed image drops from 101,988 bytes and 7,938 instructions to 101,928 bytes and 7,920 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# implementation reuses the existing call, list test and tail-call emitters. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part694-tool` and ran `dotnet artifacts/beam-backend/part694-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part694.json` using the installed release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
