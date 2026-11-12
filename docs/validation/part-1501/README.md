# Part 1501 — Type integer selectors from local call results

The register-case emitter passes its selector type to the common `select_val` writer. For a local call, it uses the existing fixed-point result analysis to prove that successful returns are integers. Calls whose result can also be a non-integer retain the ordinary register operand. The pinned OTP `beam_ssa_codegen` uses the inferred selector type when emitting `select_val`. No dependency was added. [Selector proof](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L2160) · [Operand writer](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.TupleMembership.cs#L123).

In the controlled fixture, OTP and the current compiler attach `t_integer` to the result of a signed bit-pattern decode. Both retain an untyped operand for a call with an unrestricted return branch. The release OTP VM observes the same values and errors across all 523 inputs on the original and both C# images. [Fixture](beam_typed_selector.erl) · [Execution and operands](fixture.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1501-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1501-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1501-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1501.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1501-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1501-focused.json
dotnet artifacts/beam-backend/part1501-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1501-compiler.json
```

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Four images change. The 26 affected bodies match the OTP instructions after label normalization. The added operand wrappers increase the aggregate absolute Code size gap by 52 bytes, because these modules still have other Code differences. Measured frame sizes stay equal to the prior build. The reachable instruction graph comparison covers 2,209 functions after normalizing labels, jumps, type wrappers, and checksum-derived closure identity. [Normalized functions](normalized-functions.txt) · [Code and frame totals](summary.txt) · [Graph comparison](reachable-graph.txt).

In `AlgorithmInformation-2009`, `dec_ParamOptions/2` now has the same integer selector as OTP. All 260 decoding observations agree, including errors after removing frames from the probe itself. The corpus selector scan removes the entire 26-operand local-call group. The remaining 280 missing integer selectors immediately after labels include 252 parser goto selectors across 14 modules. [ASN.1 probe](asn1.txt) · [Before](selectors-before.txt) · [After](selectors-after.txt) · [Parser group](parser-selector-ranking.txt).


The compiler block passes 3,721 of 3,829 tests. The same 108 tests fail in the Part 1500 baseline. The five focused tests pass with the same compiler binary used for the corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt) · [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The [execution probe](selector-probe.escript) accepts `fixture`, the fixture source, and the two BEAM paths, or `asn1`, the pinned OTP install, and the two generated-image directories.
