# Part 1503 — Propagate numeric facts through pair additions

The deferred pair emitter reuses the fixed-point source analysis to track the second field of a returned tuple when it is a number. Successful addition produces a numeric result even when an input has an unknown type. The emitter carries that result into the next addition and retains existing nonnegative integer bounds where proven. For tag length encoding, the second addition uses the result of the first addition. An incoming length receives a numeric annotation when the local argument analysis proves it. The reference is pinned OTP `beam_ssa_type`, release disassembly, and the arithmetic controls in Part 1502. No dependency was added. [Pair inference and emission](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralEmitter.cs#L4240) · [Tag lengths](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.LengthEncoding.cs#L192).

The fixture covers two numeric tuple fields and a pair of fields with only one proven numeric. The emitted operand types match OTP for both functions. The OTP VM observes identical results and errors across 162 inputs on the release compiler and both C# images. A release `AuditRequest` encoder probe covers 36 inputs, including rejected values, with matching results and errors after removing frames introduced by the probe. [Fixture](beam_numeric_pairs.erl) · [Fixture results](fixture.txt) · [Megaco results](megaco.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1503-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1503-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1503-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1503-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1503-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1503-focused.json
dotnet artifacts/beam-backend/part1503-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1503-compiler.json
```

The complete Windows OTP 29.1.1 comparison admits all 1,366 sources and produces 1,366 images. It records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Twenty-three images and 170 function bodies change. The reachable instruction graphs agree for 8,762 functions after label, jump, and type normalization. The Code size gap grows by 628 bytes. The type inventory reduces missing numeric annotations on addition from 182 to six and records 96 numeric annotations where OTP leaves the operand untyped. Twenty-five function bodies gain normalized equality and twelve lose it. [Type ranking before](rank-types-before.txt) · [Type ranking after](rank-types.txt) · [Normalized functions](normalized-functions.txt) · [Graph comparison](reachable-graph.txt) · [Code sizes](summary.txt).

The OTP SSA listing types `encode_tags/3`'s accumulator as numeric in the Megaco sample and reports a numeric second tuple field at the callers. In PKIX1Implicit, OTP reports `{any(), any()}` for the two fields projected by `enc_PolicyInformation/2`, and leaves its addition operands untyped. The current source analysis retains more precise numeric information in that call context. The next change needs to align the result summary seen across the recursive call boundary. [Numeric SSA](otp-numeric-ssa.txt) · [Untyped SSA](otp-untyped-ssa.txt).


The compiler block passes 3,721 of 3,829 tests. The same 108 tests fail in the Part 1502 baseline. The five focused tests pass with the same compiler binary used for the corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness delta uses the immutable Part 561 baseline. [Compiler block](compiler-tests.json) · [Test status comparison](compiler-test-status.txt) · [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The [execution probe](numeric-probe.escript) accepts `fixture`, the fixture source, and two BEAM paths, or `megaco`, the pinned OTP install, and the two generated-image directories.
