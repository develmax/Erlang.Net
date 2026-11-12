# Part 1496 — Align tuple projection and heap checks around case selectors

The entry planner defers the first selector operand from a checked input tuple until it can write directly to X0. When the remaining fields feed saved bindings, it writes them directly into Y after frame allocation. The plan verifies that omitting the temporary projections leaves a contiguous live X range. It also avoids adding a second saved-field slot while multiple fields from the entry tuple already occupy individual slots.

The register case planner uses the literal atom and position in `lists:keyfind/3` to prove the tuple tag. It still tests tuple shape and arity. When a retained field feeds a tuple or list argument, the compiler can check the heap while the selector is live in X0, before projecting the field into Y. The references are pinned OTP `beam_call_types` and release disassembly. [Entry projection](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs#L1148) · [Tuple proof and emission](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L2240).

The retained and computed nested-case controls now match OTP instructions after label normalization. The new entry control matches OTP's `allocate 4,4`, projects the first field into X0 and the second into Y1, and agrees on all tested values. Its fallback blocks still differ. An intermediate version deferred one projection and left a hole in the live X range. The final version also delays the saved field projection and checks register contiguity. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L189) · [Execution and disassembly](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1496-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1496-final-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1496-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1496-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1496-final-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1496-final-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The byte comparison records 385 Equal and 981 Different, including an Equal `ELDAPv3` from the C# ASN.1 generator. OTP accepts both binaries in all 981 differing pairs. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).

Eight release images change. `dbg_ieval:rec_clauses/3` and `erl_eval:match_clause/5` return to the OTP frame sizes. The live argument count at allocation also matches in `rec_clauses/3` and remains one higher in `match_clause/5`. Two function bodies reach normalized instruction equality. One body, `xmerl_scan:update_attributes1/2`, loses equality because the entry emitter now also extracts four unused fields. The aggregate absolute Code size gap drops by 25 bytes. [Frames](frame-delta.txt) · [Previous instructions](cases-before.txt) · [Current instructions](cases-after.txt) · [Unused projections](xmerl-lookup.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not rerun. Canonical readiness and publication remain pending, with Part 561 as the immutable readiness delta baseline. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt).
