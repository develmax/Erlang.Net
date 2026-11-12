# Part698 LDAP single optional decoder

The C# emitter recognizes a reader followed by a case that decodes one optional tagged field and ignores the remainder. It keeps the reader result in X0, checks the head and tag directly and constructs the final record without storing the unused tail. The fixture covers a present field, a different tag, an empty input and an ignored improper tail.

All 26 instructions in dec_PasswdModifyResponseValue/2 match OTP 29.1.1 after renumbering labels. The previous managed function used 41 instructions and nine slots. The generated image drops from 101,760 bytes and 7,864 instructions to 101,704 bytes and 7,849 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type differ. [Comparison](optional-decoder-normalized-oracle.txt), [disassembly](optional-decoder-oracle.txt), [chunks](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the no-op tail check, tuple tests and the tagged result emitter. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part698-tool` and ran `dotnet artifacts/beam-backend/part698-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part698.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. Next, compare the generated comprehension loops. [Focused report](focused.json), [corpus report](otp-corpus.json), [comprehension disassembly](comprehension-oracle.txt).
