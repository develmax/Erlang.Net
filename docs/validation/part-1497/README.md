# Part 1497 — Omit unused tuple reads and share nested fallback returns

The entry planner uses existing body reference analysis to omit unused tuple fields from the live X range and from projection. It applies the change to a checked tuple without field tests or equality constraints in the deferred selector path. The register case emitter shares a fallback when both paths return the same register or constant through the same frame. It keeps the original failure target for earlier sequential tests. Pinned OTP `beam_jump` provides the reference for sharing identical instruction sequences. No dependency was added. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs#L1175) · [Case emitter](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L2175).

The control fixture executes the inner success and failure, outer failure, and empty input. Its instructions match OTP after label normalization. In the isolated release build, `xmerl_scan:update_attributes1/2` also returns to normalized instruction equality. [Control execution and instructions](probe.txt) · [Release instructions](xmerl-lookup.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1497-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1497-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1497-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1497.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1497-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1497-focused.json
```

Five focused checks pass with the same compiler binary used for the corpus. The full .NET suite was not rerun. Canonical readiness and publication remain pending, with Part 561 as the immutable readiness delta baseline. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).

All 1,366 Windows OTP 29.1.1 sources pass the front end and produce BEAM files. The byte comparison records 385 Equal and 981 Different, including Equal for the C# ASN.1 generated `ELDAPv3`. OTP accepts both binaries in all 981 differing pairs. Seven images change. One function regains normalized instruction equality, with no loss of such equality in the changed images. The aggregate absolute Code size gap decreases by 52 bytes. [Corpus](otp-corpus.json) · [Normalized instructions](normalized-functions.txt) · [Loader and Code sizes](loader-and-code.txt).

The diagnostic scan finds 2,759 excess repeated simple return blocks in 1,315 functions across 435 modules, down from 2,763 blocks in 1,319 functions across 436 modules. This count measures candidate occurrences for function-local sharing. [Before](return-blocks-before.txt) · [After](return-blocks-after.txt).
