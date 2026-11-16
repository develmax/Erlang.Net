# Part 1504 — Join numeric pair facts at local calls

The compiler joins arguments from all observed local call sites before analyzing each function's result. It repeats the analysis as a callee receives broader arguments or its result becomes less specific. Exposed entries start with unknown arguments. Pair addition and tag-length emission reuse the joined result and entry facts. Existing context-sensitive inference still supplies integer bounds when the joined analysis does not prove a broader numeric result. The reference is pinned OTP `beam_ssa_type` and the contrasting optimized SSA from Megaco and PKIX1Implicit in Part 1503. No dependency was added. [Joined analysis](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.NumericPairFlow.cs) · [Pair emission](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs#L4272).

The focused preview covers the 23 release images changed in Part 1503. It gains 189 normalized function matches and loses none. `enc_AuditRequest/2` now uses two number operands, and `encode_tags/3` gets the numeric length input in the Megaco example. `enc_PolicyInformation/2` in PKIX1Implicit now retains two untyped operands. The control fixture still agrees with OTP on operand types and 162 inputs, including integer, float, and rejected values. [Preview](preview-functions.txt) · [Fixture](fixture.txt) · [PKIX](pkix.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1504-preview-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1504-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1504-preview-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1504-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1504-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1504-focused.json
dotnet artifacts/beam-backend/part1504-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1504-compiler.json
```

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Nineteen images and 264 function bodies change. The full comparison gains 189 normalized function matches, including all twelve Part 1503 losses, and loses none. The reachable instruction graphs agree for 8,198 functions after label, jump, and type normalization. The aggregate absolute Code size gap falls by 202 bytes. [Restored functions](restored-functions.txt) · [Normalized functions](normalized-functions.txt) · [Graph comparison](reachable-graph.txt) · [Code sizes](summary.txt).

The addition group with integer in C# and number in OTP falls from 687 to 181 occurrences. The missing numeric addition group falls from six to three. The 36 Megaco and 20 PKIX observations agree with OTP and the previous image after removing frames introduced by the probe. [Type ranking before](rank-types-before.txt) · [Type ranking after](rank-types.txt) · [Megaco](megaco.txt) · [PKIX](pkix.txt).


The 96 excess numeric addition annotations reported in Part 1503 are absent from the complete aligned-body inventory. The compiler block passes 3,721 of 3,829 tests. The same 108 tests fail in the Part 1503 baseline. Five focused tests pass with the same compiler binary used for the corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt) · [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The [execution probe](numeric-probe.escript) accepts `fixture`, the fixture source and two BEAM paths, or `megaco` or `pkix` followed by the pinned OTP install and the two generated-image directories.
