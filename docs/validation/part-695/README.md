# Part695 LDAP single optional field

The C# emitter recognizes the one-field tuple and the optional encoder pair. It checks the input tuple before allocating a frame, saves only the tag and feeds the pair in X0 directly to the singleton list and tail-call argument construction. The fixture covers a present field, the default and malformed input.

All 26 instructions in enc_PasswdModifyResponseValue/2 match OTP 29.1.1 after renumbering labels. The previous managed function used 50 instructions and seven slots. The new frame has one slot. The managed image drops from 101,928 bytes and 7,920 instructions to 101,844 bytes and 7,896 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type differ. [Comparison](single-optional-normalized-oracle.txt), [disassembly](single-optional-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The change reuses optional field lowering, exact tuple return proof and the existing call writer. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part695-tool` and ran `dotnet artifacts/beam-backend/part695-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part695.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending.

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
