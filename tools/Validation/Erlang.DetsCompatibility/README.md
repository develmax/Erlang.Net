# Native DETS file interchange

This executable exercises the production internal C# v9 engine through a small
reflection adapter. It does not contain a replacement storage engine. It mutates
three OTP-created files and creates/grows three files in C#, then asks admitted
OTP-29.1.1 to open all six and compare complete sorted contents.

From repository root:

```powershell
pwsh -File tools/validate-dets-original.ps1 -Output artifacts/dets-acceptance/fresh-native
dotnet build tools/Validation/Erlang.DetsCompatibility/Erlang.DetsCompatibility.csproj -m:1 --artifacts-path artifacts/dets-acceptance/interchange-build
dotnet artifacts/dets-acceptance/interchange-build/bin/Erlang.DetsCompatibility/debug/Erlang.DetsCompatibility.dll . artifacts/dets-acceptance/fresh-native artifacts/dets-acceptance/fresh-interchange
```

Both output directories must be new. The native capture requires the existing
official compiler admission and execution manifest. The tool verifies the pinned
release/commit, manifest and native executable/DETS input hashes before and after
the oracle run. The receipt records source-file hashes, production assembly
identity, output-file hashes, case count and full ETF equality. A passing result
covers these six files, not every corrupt/legacy/platform variant.

The library-level acceptance lives in `tests/Erlang.Tests/Otp/Stdlib/Dets` and
uses the same original-BEAM observer on both .NET backends. `-Suite` and `-Gc`
switches on the capture script obtain their separate native reference corpora.
No full regression is part of this tool.
