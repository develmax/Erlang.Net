# Part672 LDAP tagged decoder returns

The C# emitter returns from each terminal tagged branch, preserving the frame size calculated for the function. A tagged branch can pass the result of a local call in X0 directly to the unshadowed binary_to_list/1 BIF before constructing its result tuple. The focused fixture exercises that conversion, a regular tagged result and the fallback.

The bindResponse branch now matches OTP's call, heap test, tuple construction, deallocation and return sequence. The deallocation operand still differs because the managed decoder uses two stack slots and the OTP decoder uses zero. The dispatch prefix through tuple projection still matches when label numbers are omitted. The managed image shrank from 105,880 to 105,844 bytes while the instruction count increased from 8,943 to 8,968 as branches gained individual epilogues. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi match byte for byte. Code, ExpT, LocT, Attr, Line and Type differ. [Branch disassembly](branch-return-oracle.txt), [chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the existing emitter and managed BIF call emission. Dependencies and licenses are unchanged.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part672-tool` and ran `dotnet artifacts/beam-backend/part672-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part672.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips and 88 boundary calls. The corpus checked all 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. The full runtime harness and canonical readiness regeneration remain pending. [Focused report](focused.json), [corpus report](otp-corpus.json).

The next discrepancy in the tagged branches is the stack frame size. Whole-image parity remains open.
