# Part 1486 — Share tuple shape tests and literal field selectors

The entry frame planner groups consecutive tuple clauses with the same argument, shape and projection registers when each clause tests a distinct integer or atom in the same field. It emits one tuple check, one set of projections and a `select_val`. A shape mismatch, missing key or failed guard enters the next source clause after the group. A repeated key ends the group so its later clause remains eligible. The pinned OTP `beam_core_to_ssa` source and release disassembly provide the reference. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Baseline selector ranking](tuple-selectors-before.txt).

The `tuple_alias/2` fixture checks successful literal branches, an exact integer versus float mismatch, and a failed guard followed by a repeated key. The reference and both C# builds agree on the tested values. C# shares the initial shape test and projections. OTP also reuses the projections when the guard fails, while C# checks the later clause again from the original tuple. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Instruction and result comparison](probe.txt).

In `erl_prim_loader:check_file_result/3`, the C# compiler groups the three exact error reason matches into a table with the same keys as OTP. C# places the selector in X3 to keep the original tuple in X2. OTP uses X2 for the selector. [Release disassembly](erl-prim-loader-selector.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1486-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1486-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1486-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1486.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1486-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1486-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 22 release images and 26 instruction bodies. The set of normalized function matches and measured frame sizes stay the same. The aggregate absolute Code size gap drops by 564 bytes. Twelve selector shapes across eleven modules reach equality in arity, field index and key set. The missing-selector ranking falls from 2,405 to 2,393 shapes. Of those remaining, 1,768 in 454 modules select the first atom field of an arity-checked tuple. [Normalized functions](normalized-functions.txt) · [Frame totals](summary.txt) · [New selectors](new-tuple-selectors.txt) · [Remaining tag selectors](tuple-tag-candidates.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not run on this build. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
