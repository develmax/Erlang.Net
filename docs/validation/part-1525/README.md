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

## Results

All 1,366 source files were accepted and generated. The comparison has 387 Equal and 979 Different, with zero status transitions. ELDAPv3 remains byte-identical from the C# ASN.1 generator. OTP loads all 979 differing images. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt) · [Status changes](status-changes.txt).

The change reaches 12 modules and 13 functions. Nine functions match OTP frame sizes, and none move farther away. The aggregate Code size gap drops by 47 bytes. The normalized instructions for dialyzer_dep:map__lookup/2 now match. Three focused compiler tests pass. [Frame delta](frame-delta.txt) · [Function delta](normalized-functions.txt) · [Focused tests](focused.json).

The first difference is still a larger C# allocate in 882 modules and 11,111 functions, down from 11,120 functions in Part 1524. The remaining comparison differences include typed saved operands in inets and net. In dbg_icmd:get_binding/2, OTP elides the atom test after lists:keyfind/3 has excluded the tuple case. [First differences](first-difference-ranking.txt) · [inets](inets-key1search.txt) · [net](net-win_name2index.txt) · [dbg_icmd](dbg-icmd-get-binding.txt).

The compiler block passed 3,719 of 3,830 tests. Three additional cases timed out during the concurrent corpus run. The isolated retry passed 3 of three. The full runtime suite and canonical readiness were not rerun. [Compiler block](compiler-test-status.txt) · [Retry](compiler-retest.json).
