# Part 1514 — Infer atom types for entry selectors

The joined argument analysis propagates atoms from literals, successful atom conversion BIFs, and successful is_atom guards. It uses the resulting proof in entry value dispatch, including literal clauses, folded literal guards, and tail-call tables. Unknown or external entry arguments keep their existing representation. The analysis reuses existing call joins and the typed register writer. [Argument facts](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.NumericPairFlow.cs) · [Literal dispatch](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.Lowering.cs).

The reference is OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. OTP `beam_ssa_type` supplies the call-site type flow, while the release instructions provide the selector operands. No dependency was added.

The deterministic fixture matches OTP byte for byte. It covers list_to_atom, a guarded atom argument, and an unknown input. OTP and both C# images agree on 30 returns, errors and module-stack observations. Both builds and all six focused checks pass. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_atom_selector_probe.erl) · [Fixture BEAM](../../../tests/Erlang.Tests/Resources/Beam/beam_atom_selector_probe.beam) · [Fixture results](fixture.txt) · [Focused checks](focused.json) · [Hashes](sha256.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1514-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1514-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1514-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1514-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1514-tests/Erlang.Tests.dll --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1514-focused.json
dotnet artifacts/beam-backend/part1514-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1514-compiler.json
```

The Windows OTP 29.1.1 comparison accepts and compiles all 1,366 sources. 386 images are Equal and 980 are Different, with no status transitions. `ELDAPv3` remains Equal using the C# ASN.1 generator. OTP accepts both images in all 980 differing pairs. The corpus CLI returns 1 for the remaining byte differences. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Twenty-three images and 31 function bodies change. Eight bodies gain normalized instruction equality. Of the 31 added atom operands, 15 match the reference selector with the same keys and three match the operand of a narrower reference selector. OTP emits exact tests in 11 other cases and constant returns in two. The aligned-body missing atom selector group falls from 22 to 14 modules. The absolute aggregate Code size gap grows by 62 bytes. Frame sizes remain the same. Previous and current C# instruction graphs agree after type normalization for all 3,020 functions in the changed images. [Instruction delta](normalized-functions.txt) · [Selector delta](selector-delta.txt) · [Remaining types](rank-types.txt) · [Graphs](reachable-graph.txt) · [Sizes](summary.txt).

The compiler block passes 3,722 of 3,830 tests with the same 108 failures as Part 1513. The SDK layout pass applied only whitespace to the changed call sites after the build. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison retains the immutable Part 561 snapshot. [Compiler tests](compiler-tests.json) · [Test statuses](compiler-test-status.txt).
