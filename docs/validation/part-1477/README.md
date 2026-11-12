# Part 1477 — Share tagged case payload projection

The compiler shares extraction of a two-element tuple payload across terminal case arms with distinct atom tags and a fresh payload variable. Every arm must have a supported register plan before this path emits instructions. The selector stays in x0 for an unmatched tag or arity. Supported branch calls are emitted in tail position, and the failure instruction records the case location. Other shapes keep their existing emission path.

The released xmerl_xsd_re map/1 now matches the OTP disassembly instruction for instruction, including absolute labels, register operands and line locations. The runtime comparison checks successful parsing and a syntax error. The permanent case fixture verifies ordinary results, a tail error, and the original case_clause payload for both an unknown tag and an incorrect arity.

- [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs), [permanent test](../../../tests/Erlang.Tests/Compiler/Beam/BeamModuleEmissionChecks.cs), and [control probe](probe.txt)
- [Exact map/1 instructions](map-selector-comparison.txt) and [runtime comparison](runtime.txt)

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1477-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1477-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1477-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1477.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1477-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1477-focused.json
```

The Windows OTP 29.1.1 comparison generates all 1,366 release images, with 384 Equal and 982 Different. There are zero rejected sources, unsupported modules, compiler errors and whole-file status transitions. ELDAPv3 remains Equal after C# ASN.1 generation. OTP accepts both images in all 982 differing pairs.

Seven release images change. Two function bodies reach normalized instruction equality: xmerl_xsd_re map/1 and diameter_config make_caps/2. No normalized body loses equality. The aggregate Code size gap falls by 51 bytes. The xmerl_xsd_re module still differs in chr/2 and scan/2.

- [Full corpus](otp-corpus.json), [status changes](status-changes.txt), and [OTP loader](loader-and-code.txt)
- [Normalized instructions](normalized-functions.txt), [code totals](summary.txt), and [remaining xmerl functions](xmerl-functions-after.txt)
- [Nearest files before this change](nearest-before.txt) and [focused checks](focused.json)

All five focused checks pass using the corpus compiler binary. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot.
