# Part676 LDAP wrapped exception lowering

The compiler recognizes a protected tuple projection when the local call takes the unchanged function arguments, the projection is proven, the success clause returns a tagged result and the catch handles error and exit with equivalent wrapped and unwrapped outcomes. The emitter reuses the try token slot for the reason. It dispatches directly on the exception class register, checks both reason tags in X registers and builds the stacktrace only for caught classes. The unmatched throw path re-raises using the raw stack and reason.

All 43 disassembled instructions in encode/2 match OTP 29.1.1, including labels, registers, type annotations and line locations. The function previously had 77 instructions and 13 frame slots. It now has one slot. The 219-instruction protocol operation decoder still matches after renumbering labels. [Exact encoder comparison](encode-exact-oracle.txt), [encoder instructions](encode-protected-oracle.txt), [decoder comparison](decoder-normalized-oracle.txt).

The managed image drops from 105,696 bytes and 8,934 instructions to 105,580 bytes and 8,900 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The OTP SSA code generator emits catch register extraction and explicit stack construction. The C# path reuses the existing tuple projection proof, try opcodes and protected pair emitter. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part676-tool`. Run `dotnet artifacts/beam-backend/part676-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part676.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align the protected decode/2 call chain and reuse the wrapped handler for it. OTP keeps only the type argument live over ber_decode_nif/1 and projects element/2 into X1. [Decoder protected call comparison](decode-protected-oracle.txt).

All three focused checks passed. The wrapped catch fixture checks successful conversion, a badarg, a tagged exit and an unmatched throw. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
