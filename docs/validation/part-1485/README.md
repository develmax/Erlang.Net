# Part 1485 — Select literal clauses before guarded fallback

The entry frame planner groups consecutive unguarded clauses that match distinct integer or atom literals in the same argument. It emits `select_val` and passes unmatched values to the remaining clauses in source order. It also folds constant bitstring returns in literal clauses. The C# emitter reuses the existing exact literal table and register guard paths. The pinned OTP reference is `beam_core_to_ssa` and the release disassembly. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Baseline candidates](guarded-prefix-before.txt).

The `literal_entry/1` fixture exercises a negative integer, zero, a later float literal, two different guarded branches, and the unconditional fallback. The C# and OTP images now begin with equivalent literal tables and frameless constant returns. The following integer comparison and its failure label still differ. The pinned VM returns the same values from the reference and both C# images on all tested inputs. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Instruction and result comparison](probe.txt).

An unordered integer and atom control produces the same selector, arm order, and guard instructions in both compilers. In the release `ssh_bits:mpint/1`, the selector and two constant branches match the OTP disassembly. The other branches retain differences in binary lowering and frame sizes. The pinned VM agrees on fifteen values and errors, including large integers and invalid terms. [Unordered control](prefix-order.txt) · [Release disassembly](ssh-bits-mpint.txt) · [Release values](ssh-bits-values.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1485-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1485-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1485-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1485.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1485-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1485-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 42 release images and 51 instruction bodies. Eight functions reach normalized instruction equality, with all previous matches retained. Ten frames move closer to OTP, including seven that reach its frame size. The aggregate absolute Code size gap drops by 611 bytes. The ranking of functions whose entry selector is still missing falls from 88 functions in 58 modules to 57 in 38. [Normalized functions](normalized-functions.txt) · [Frame totals](summary.txt) · [Remaining prefix candidates](guarded-prefix-after.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not run on this build. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
