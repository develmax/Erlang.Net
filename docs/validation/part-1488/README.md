# Part 1488 — Select tuple arity before tag dispatch

The entry planner groups adjacent tagged tuple clauses by argument and arity. It emits `select_tuple_arity` before selecting tags within each arity. A guard failure or a rejected secondary pattern proceeds to the next source clause after the group. A repeated arity and tag ends the group, preserving later eligible clauses. Other arguments keep their branch-specific tests. The pinned OTP `beam_ssa_codegen` source and release disassembly provide the reference. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Baseline arity selectors](../part-1487/tuple-arity-candidates.txt).

The `tuple_arities/2` control starts with an atom clause and exercises two tuple arities, an atom tag used at both arities, and branch-specific patterns in the second argument. OTP and both C# builds agree on the tested values, including failed guards and second-argument tests. The C# candidate now emits the arity table and the nested tag table. In `ct_ssh:handle_msg/2`, the number of tagged tuple tests drops from 90 to 61. OTP uses 36. Both the candidate and OTP emit one arity table and four value tables. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Probe](probe.txt) · [Common Test dispatch counts](ct-ssh-dispatch.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1488-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1488-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1488-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1488.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1488-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1488-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 31 release images and 36 instruction bodies. Twenty-five arity tables in 23 modules now match the OTP arity sets, including the `[2,3,4,5,6]` table in `ct_ssh:handle_msg/2`. The set of normalized function matches and the measured frame sizes stay the same. The aggregate absolute Code size gap increases by 755 bytes, while the gap for `ct_ssh` drops by 66 bytes. The remaining arity selector count falls from 1,794 to 1,769. [New arity tables](new-arity-selectors.txt) · [Remaining selectors](tuple-arity-candidates.txt) · [Frame totals](summary.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not run on this build. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
