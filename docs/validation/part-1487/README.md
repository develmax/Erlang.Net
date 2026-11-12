# Part 1487 — Select tuple tags after a shared arity check

The entry frame planner groups consecutive flat tuple clauses that match distinct atom tags in the same argument and at the same arity. It checks the tuple and its arity, extracts the first field, and selects the arm through `select_val`. Each arm projects the fields required by its own pattern. A shape mismatch, missing tag, or failed guard proceeds to the next clause after the group. A repeated tag ends the group, preserving later matches. The pinned OTP `beam_core_to_ssa` and the release disassembly provide the reference. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Baseline tag selectors](../part-1486/tuple-tag-candidates.txt).

The `tuple_tag/2` fixture uses different projections in the two arms, an alias needed after a call, a failed guard followed by a repeated tag, and a tuple of the wrong arity. The pinned VM agrees on all tested values. OTP also shares some payload projections and can jump from the guard to the reused projection. C# projects payloads per arm and rechecks the repeated tag with the original argument. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Instruction and result comparison](probe.txt).

The complete `asn1ct_check:parameter_name_style/1` instruction sequence matches after label normalization. In `ct_ssh:handle_msg/2`, two tag selectors replace four tagged tuple tests. The remaining blocks include clauses with a second tuple argument and mixed tuple arities. [ASN.1 compiler disassembly](asn1ct-tag-selection.txt) · [Common Test dispatch counts](ct-ssh-dispatch.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1487-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1487-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1487-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1487.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1487-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1487-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 80 release images and 120 instruction bodies. A further 83 tag selectors across 54 modules match OTP by tuple arity, selector field and key set. One complete function reaches normalized instruction equality and no previous normalized matches are lost. Forty-nine measured frame sizes reach the OTP size. The aggregate absolute Code size gap increases by 314 bytes. The remaining tag selector count drops from 1,768 to 1,685. [New selectors](new-tuple-selectors.txt) · [Remaining tag selectors](tuple-tag-candidates.txt) · [Normalized functions](normalized-functions.txt) · [Frame totals](summary.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not run on this build. Canonical readiness and publication remain pending. The readiness comparison baseline remains the immutable Part 561 snapshot. [Focused checks](focused.json) · [Compiler hashes](compiler-hash.txt).
