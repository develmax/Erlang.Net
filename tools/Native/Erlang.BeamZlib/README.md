# Pinned OTP BEAM compressor

The C adapter uses zlib 1.3.2 from the OTP reference tag in `reference-baseline.json`. It reproduces the bounded one-shot deflate call used by ERTS for compressed ETF. The C# compiler supplies the ETF bytes and writes the compressed ETF header. The adapter has no dependency on the Erlang VM.

From the repository root, run `pwsh -File tools/Native/Erlang.BeamZlib/build-windows.ps1`. The script needs the reference OTP checkout and Visual Studio's x64 C++ toolchain. It archives the pinned sources without modifying the checkout and builds the Windows x64 asset in the compiler runtime directory.

The retained asset was built with MSVC 14.51.36231. Use `-Verify` with the same toolchain to compare a fresh build with that asset. The source archive is identified by the pinned OTP commit and the upstream zlib identity in `erts/emulator/zlib/vendor.info`. [zlib license](../../../licenses/ZLIB-LICENSE.txt).
