# Part 1475 — Keep nested case selector calls in registers

The general case emitter reuses the entry selector planner for a two-call selector whose outer call accepts the inner result in a register. The branch reference check saves parameters needed by the branches or continuation. The inner result moves straight into the outer call, and an unchanged source location is emitted once for the two calls. Other selector shapes continue through the existing lowering.

The control exercises a branch that reads a parameter after both calls and a branch that raises an error from a tuple payload. The current C# compiler reduces its frame from three slots to two. OTP uses one slot. Both C# images and the pinned OTP control image return the same results and errors.

The xmerl_xsd_re map/1 release example stops copying its input into Y before calling scan/1. Its frame drops from two slots to one, compared with zero in OTP. The remaining slot holds a tuple field for constructing the error argument.

- [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs), [control source](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl), and [probe](probe.txt)
- [Release before](../part-1474/map-selector-before.txt) and [release after](map-selector-after.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1475-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1475-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1475-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1475.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1475-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1475-focused.json
```

The complete Windows OTP 29.1.1 comparison generates all 1,366 release images, with 384 Equal and 982 Different. There are zero rejected sources, unsupported modules, compiler errors and whole-file status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. The OTP loader accepts both images in all 982 differing pairs.

The change affects 49 images and 56 normalized instruction bodies. Frame sizes move closer to OTP in 28 functions, including five that reach the OTP frame size. The aggregate Code size gap falls by 264 bytes. The set of instruction bodies equal after normalization stays the same.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [loader](loader-and-code.txt)
- [Frame totals](summary.txt), [normalized instructions](normalized-functions.txt), and [source ranking](case-arm-ranking.txt)
- [Focused tests](focused.json) and [compiler hashes](compiler-hash.txt)

All five focused .NET checks pass using the corpus compiler binary. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
