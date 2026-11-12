# Part 1513 — Carry local call result unions into exact comparisons

The register case emitter uses an inferred union for an exact comparison on the result of a local call. The analysis joins the categories of terms returned by clauses and follows local tail calls through mutually recursive functions. An unknown return path suppresses the annotation, including calls to native implementations. The supported unions cover atoms with tuples, nonempty lists, or lists that can be empty. [Result analysis](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.CallResultTypes.cs) · [Case emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs).

The reference is pinned OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. OTP `beam_ssa_type` joins successful return types and `beam_types` encodes the resulting union. The change reuses the existing case plans and typed register writer. No dependency was added.

The deterministic regression fixture matches OTP byte for byte, and all six focused tests pass. It agrees with OTP on 40 returned terms. The recursion probe checks 112 additional calls and verifies both typed and untyped operands for mutually recursive producers. [Fixture source](../../../tests/Erlang.Tests/Resources/Beam/beam_result_union_probe.erl) · [Fixture BEAM](../../../tests/Erlang.Tests/Resources/Beam/beam_result_union_probe.beam) · [Fixture results](fixture.txt) · [Cycle results](cycle.txt) · [Focused tests](focused.json) · [Hashes](sha256.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1513-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1513-tests
Copy-Item artifacts/beam-backend/part1513-tool artifacts/beam-backend/part1513-verified-tool -Recurse
Copy-Item artifacts/beam-backend/part1513-tests/Erlang.Compiler.dll artifacts/beam-backend/part1513-verified-tool/Erlang.Compiler.dll
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1513-verified-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1513-verified.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1513-tests/Erlang.Tests.dll --case compiler/beam/atom-case-emits-local-tails --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1513-focused.json
dotnet artifacts/beam-backend/part1513-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1513-compiler.json
```

The Windows OTP 29.1.1 comparison accepts all 1,366 sources and generates all 1,366 images. 386 images are Equal and 980 are Different, with no status transitions. `ELDAPv3` remains Equal using the C# ASN.1 generator. OTP accepts both images in all 980 differing pairs. The CLI returns 1 for the remaining byte differences. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Thirteen images change. Five of the 15 changed functions gain normalized instruction equality. The 17 newly typed operands agree with the reference type, including one comparison with opposite test polarity in the OTP layout. The missing union group falls from 27 operands in 23 modules to 22 in 19. Frame sizes are unchanged, and the absolute aggregate Code size gap grows by 34 bytes. Previous and current C# images have the same reachable instruction graphs after type normalization in all 1,829 functions of the changed modules. [Instruction delta](normalized-functions.txt) · [Typed operands](union-delta.txt) · [Remaining types](rank-types.txt) · [Size summary](summary.txt) · [Graphs](reachable-graph.txt).

The largest remaining aligned-body type group is atom selectors in 22 modules. The caller audit finds 21 private selectors. Twenty of the 22 selectors test an entry register immediately. Two callers pass a direct list_to_atom result and the rest mostly pass variables. [Selector callers](atom-selector-callers.txt).

The compiler block passes 3,722 of 3,830 tests with the same 108 failures as Part 1512. The full .NET suite was not rerun. Canonical readiness and publication remain pending. The readiness comparison retains the immutable Part 561 snapshot. [Compiler tests](compiler-tests.json) · [Test statuses](compiler-test-status.txt).
