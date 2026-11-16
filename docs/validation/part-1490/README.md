# Part 1490 — Keep saved tuple case fields in X

The register tuple arm planner can project fields directly from a saved Y selector into X. It handles flat tagged patterns without guards or aliases when the branch result can use registers directly. The shared pattern matcher still checks literals and bound variables before projection. The saved selector remains available at the join and on a failed match. The change uses the existing register operand encoding and the tuple field operation in pinned OTP `beam_ssa_codegen`. [Case emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs#L10852).

The `saved_tuple_call/2` fixture compares an exact bound key, exercises a local call and a same-tag fallback, and returns the original selector. OTP and both C# builds agree on the tested values, including the fallback for `7.0` when the bound key is `7`. The C# fallback branches now write payloads into X and the function uses three Y slots instead of four. OTP uses two slots and shares field extraction before the key test. The older `saved_tuple_case/2` control still uses the branch continuation path. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L130) · [Disassembly and results](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1490-checked-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1490-checked-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1490-checked-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1490-checked.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1490-checked-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1490-checked-focused.json
```

The Windows OTP 29.1.1 corpus admits and generates all 1,366 modules. The byte comparison records 385 Equal and 981 Different, including an Equal `ELDAPv3` generated from ASN.1 by C#. No image changes equality status. The OTP loader accepts both binaries in each of the 981 differing pairs. [Full corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 171 release images and 347 function bodies. Eighty-nine measured frame sizes move closer to OTP, including one that reaches OTP's size. The aggregate absolute Code size gap drops by 1,912 bytes. No function gains or loses normalized instruction equality. All 981 differing files still have different Code payloads. The module with the fewest differing chunks has four. [Frame totals](summary.txt) · [Normalized instruction comparison](normalized-functions.txt) · [Chunk ranking](chunk-ranking.txt) · [Closest instruction bodies](nearest-after.txt).

Five focused checks pass with the same compiler binary used by the corpus tool. The full .NET suite was not rerun. The readiness generator requires a complete passing test report. Canonical readiness and publication remain pending, with Part 561 as the immutable readiness delta baseline. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).
