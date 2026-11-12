# Part 1495 — Reserve a slot for fields live across an inner selector

The entry register planner can reserve a leading stack slot for an outer field used by the inner branches. It replans the saved operands against the shifted frame and projects the field directly into Y0 on the selected path. The nested selector can also construct tuple and list arguments using the existing register constructor planner. The original branch alternatives keep their inputs and result paths. Pinned OTP SSA liveness and frame layout provide the reference. [Compiler](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.GeneralCatch.cs#L1131).

Both controls now use the OTP frame size and project the retained field directly into Y0. The new computed argument control uses the field in a tuple passed to the inner selector and returns the field on the success path. The pinned OTP VM and both C# builds agree on the tested values. The fixture still differs in the lookup tuple proof and the placement of the heap check before the field projection. [Fixtures](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl#L157) · [Disassembly and execution](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1495-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1495-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1495-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1495.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1495-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1495-focused.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The byte comparison records 385 Equal and 981 Different, including an Equal `ELDAPv3` from the C# ASN.1 generator. OTP accepts both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).

Eleven release images and 12 function bodies change. The frames in the `dialyzer_utils` helper, `snmpm_config:verify_user_behaviour/1` and `c:c/4` reach the OTP size. `dbg_ieval:rec_clauses/3` and `erl_eval:match_clause/5` acquire one extra slot. Their entry lowering projects fields from the input clause tuple early. OTP keeps the original tuple across the call and projects its fields as needed. The aggregate absolute Code size gap falls by 40 bytes. No normalized instruction match changes. [Frames](frame-delta.txt) · [Previous instructions](cases-before.txt) · [Current instructions](cases-after.txt) · [Totals](summary.txt).

Five focused checks pass with the same compiler binary as the corpus tool. The full .NET suite was not rerun. Canonical readiness and publication remain pending, with Part 561 as the immutable readiness delta baseline. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).
