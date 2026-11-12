# Part 1512 — Preserve numeric bounds through octet shifts

The joined call analysis keeps the lower bound from Erlang term-order tests even when the value can be nonnumeric. The octet emitter writes the bounded mixed operand type at the comparison and shift. A successful integer shift preserves the numeric bound and proves integer type for the following mask. When no sign bound is available, the mask still receives the integer type. [Call analysis](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.NumericPairFlow.cs) · [Octet emission](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.LengthEncoding.cs).

Pinned OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5` supplies the reference through `beam_ssa_type:infer_relop_any/2`, `beam_types:subtract/2`, and release disassembly. The change reuses the existing joined facts and Type chunk writer. No dependency was added.

The permanent fixture compares whole images for a guarded mixed input and an unknown input. Its deterministic OTP and C# files have the same SHA-256. All 35 return, exception, and module-stack observations agree in the previous and current C# outputs and OTP. Both builds and all six focused tests pass. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_octet_shift_range_probe.erl) · [Fixture BEAM](../../../tests/Erlang.Tests/Resources/Beam/beam_octet_shift_range_probe.beam) · [Fixture observations](fixture.txt) · [Focused tests](focused.json) · [Hashes](sha256.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1512-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1512-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1512-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1512-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1512-tests/Erlang.Tests.dll --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1512-focused.json
dotnet artifacts/beam-backend/part1512-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1512-compiler.json
```

The Windows OTP 29.1.1 comparison accepts and compiles all 1,366 sources. 386 images are Equal and 980 are Different, with no status transitions. `ELDAPv3` remains Equal using the C# ASN.1 generator. OTP accepts both images in all 980 differing pairs. The corpus CLI returns 1 for the remaining byte differences. [Corpus](otp-corpus.json) · [Status transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Eighteen images change and all 26 changed function bodies gain normalized instruction equality. Both groups of 26 missing shift and mask operands disappear from the aligned-body ranking. Frame sizes are unchanged. The absolute aggregate Code size gap grows by 176 bytes as the type operands are emitted. The previous and current C# images have the same reachable instruction graphs after type normalization in all 6,142 functions of the changed modules. [Instruction delta](normalized-functions.txt) · [Type differences before](bitwise-types-before.txt) · [Type ranking after](rank-types.txt) · [Code sizes](summary.txt) · [Control flow](reachable-graph.txt).

The release ECPrivateKey probe confirms the operand types of all three octet helpers and agrees with both C# images on 72 encode results and exceptions. [Instructions](ec-private-key-positive.txt) · [Release probe](release-octet-probe.escript) · [Release observations](ec-private-key.txt).

Exact comparisons with missing union operands remain in 23 modules. Four images still have only one normalized body difference each. [Type ranking](rank-types.txt) · [Nearest files](nearest.txt).

The compiler block passes 3,722 of 3,830 tests with the same 108 failures as Part 1511. The SDK layout pass wrapped the new emitter call after the build. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison retains the immutable Part 561 snapshot. [Compiler tests](compiler-tests.json) · [Test statuses](compiler-test-status.txt).
