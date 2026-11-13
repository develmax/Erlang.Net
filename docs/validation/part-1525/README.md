# Part 1525 — Construct terminal tuple results from register projections

For eligible tuple cases with an atom alternative, the compiler keeps the tested selector in X0 until all shape and equality tests succeed. It reserves heap space while the source tuple is still live, extracts the payload directly into X0, and constructs the result. Only input variables needed by the pattern or the result are saved. Exact comparisons use the existing local call-site atom proof where available.

The implementation reuses the tuple projection, entry call and tuple construction emitters. No dependency was added. The reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. [Implementation](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.CallTupleCaseProjection.cs).

The deterministic fixture matches the complete OTP image. It checks nested tuple extraction, exact integer and float key comparisons, preservation of an outer argument, and malformed inputs. The OTP oracle compares 120 return values, exceptions, and fixture stack observations. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_construct.erl) · [Hashes](sha256.txt) · [Observations](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1525-release-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1525-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1525-release-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1525-release.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
$tests = 'artifacts/beam-backend/part1525-tests/Erlang.Tests.dll'
dotnet $tests --case compiler/beam/whole-module-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1525-focused.json
```
