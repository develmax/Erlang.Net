# Part 1510 — Carry tuple fields through call sequences

The saved call planner handles a sequence of tuple matches whose final call supplies fields to a flat tuple result or a terminal literal case with flat tuple arms. It passes fields needed only by the following call in X registers. At the end of the sequence it keeps the final tuple in X0 and projects the fields needed by each return path. Unproven tuple shapes retain their original `badmatch` edge and value. [Call sequence](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.SavedCallSequence.cs) · [Tuple field plan](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.CheckedCallSequence.cs).

The reference is pinned OTP `beam_ssa_pre_codegen` and the release instructions for `xmerl_xsd_type:duration_atoms_time/1`. The change reuses the saved call and checked tuple planners. No dependency was added.

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1510-projected-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1510-projected-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1510-projected-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1510-projected.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1510-projected-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1510-projected-focused.json
dotnet artifacts/beam-backend/part1510-projected-tests/Erlang.Tests.dll --block compiler --report artifacts/beam-backend/part1510-projected-compiler.json
```

Both final builds pass with no warnings or errors. All five focused tests pass. The corpus, harness, and fixture tools use the same compiler hash. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).

The fixture exercises terminal cases, direct tuple returns, multiple projections, unknown shapes, and an already bound case pattern. The two unknown-shape functions move from six frame slots to two, matching OTP. The two local examples move from six slots to one, against OTP's zero. All 153 returns, error reasons, and module frames in exception stacks agree with OTP. [Fixture](beam_tuple_call_chain.erl) · [Oracle](tuple-chain-probe.escript) · [Results](fixture.txt).

The Windows OTP 29.1.1 comparison accepts all 1,366 sources and generates all 1,366 images. There are 385 Equal and 981 Different, with no status transitions. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP loads both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Transitions](status-changes.txt) · [Loader](loader-and-code.txt).

Eight images and nine function bodies change. All nine frames move closer and eight reach OTP's frame size. `xmerl_xsd_type:duration_atoms_date/1` and `duration_atoms_time/1` gain normalized instruction equality. The aggregate Code size gap falls by 180 bytes. All 16 release xmerl observations agree. [Frames](frame-delta.txt) · [Function matches](normalized-functions.txt) · [xmerl instructions](xmerl-duration-atoms.txt) · [xmerl probe](xmerl.txt).

The tuple-chain audit falls from 82 functions in 35 modules to 77 in 34. Some remaining chains interleave map construction or comprehensions with calls. The next complete-file candidate, `AlgorithmInformation-2009`, has one remaining normalized body difference, `enc_ParamOptions/2`. Six modules have 28 functions with the same atom dispatch, local tail calls and nested exit fallback. [Remaining chains](terminal-shapes-after.txt) · [Nearest files](nearest.txt) · [Atom case ranking](atom-tails-after.txt) · [Instructions](algorithm-information-enum.txt).

The compiler block passes 3,721 of 3,829 tests. All 108 failures have the same status as Part 1509. The final formatter pass changed whitespace only. The full .NET suite was not rerun. Canonical readiness and publication remain pending, and the Part 561 comparison baseline is unchanged. [Compiler block](compiler-tests.json) · [Status comparison](compiler-test-status.txt).
