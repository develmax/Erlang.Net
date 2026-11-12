# Part604 exact Docs compression and BEAM emission

The Windows x64 C# backend writes deterministic ETF and compresses its body with a small native adapter built from the OTP-pinned zlib 1.3.2. The adapter follows the bounded one-shot deflate path in ERTS. No OTP VM is invoked by the compiler. The two existing Docs images now match completely on Windows x64. Other platforms retain the .NET ETF compressor and only assert raw Docs payload parity. [Native build recipe](../../../tools/Native/Erlang.BeamZlib/README.md).

`pwsh -File tools/Native/Erlang.BeamZlib/build-windows.ps1 -Verify` reproduced the retained binary with SHA-256 `A5C671DF194C4E15D9BC3E05D475890DF5FAED1A5293B9587579730317D7F811`. The local Debug tool package contained the DLL and the zlib license.

New emission covers self, node, selected unary type predicates, explicit display, tuples with live argument operands, and empty or single-cell lists. The parser accepts additional named warning-only options. The admitted OTP provider built the BIF and constructor fixtures with deterministic output. Both full images matched and both generated modules ran in the original VM.

Reference sources are [ERTS ETF encoding](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/emulator/beam/external.c), [ERTS zlib call](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/emulator/beam/erl_zlib.c), and [OTP opcodes](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/genop.tab).

The extracted local tool package produced the same bytes for the documented fixture. `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. Seven selected cases passed, covering 22 byte-equal fixture modules on Windows x64. [Focused report](focused.json).

`dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part604.json` exited 1 after checking 1,366 installed images. It reported 501 source rejections, 864 unsupported modules after parsing, one missing source, and zero regenerated release images. Full regression, canonical readiness regeneration, and publication remain open. [Per-image report](otp-corpus.json).
