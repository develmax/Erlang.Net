# Part 1483 — Plan frames for branches with tuple parameters

The compiler routes tuple-pattern clauses with sequences, `case`, or `if` through its separate entry frame planner. The planner checks the tuple in entry registers, projects the fields, and saves the inputs each selected body uses. Simple list and tuple returns in the same dispatch use the existing register construction planner. An initial run found instruction mismatches in `asn1ct:save_gen_state/2` and the closure inside `rpc:handle_cast/2`. The expanded path now leaves record updates and anonymous functions with the existing planners, which carry record proofs and capture order. The pinned OTP compiler places frames and reserves registers in `beam_ssa_pre_codegen`. [Entry planner](../../../src/Compiler/Erlang.Compiler/Compilation/BeamModuleCompiler.EntryClauseFrames.cs) · [Baseline candidates](../part-1482/tuple-branch-ranking-after.txt).

The `tuple_entry/2` fixture keeps the nested selector in X and reduces the frame on the tagged branch from three slots to two. OTP also uses two slots and projects the field directly into Y after allocating the frame. The C# candidate projects into X before allocating. The OTP VM returns the same values for all three builds on the successful branches, a wrong tuple arity, and a different tag. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Instruction and result comparison](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1483-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1483-final-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1483-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1483-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1483-final-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1483-final-focused.json
```

The final run admits every source and generates all 1,366 Windows OTP 29.1.1 images. It records 385 Equal and 981 Different. `ELDAPv3` remains Equal after C# ASN.1 generation. OTP accepts both images in each differing pair. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The final candidate changes 228 images and 522 function bodies. Frame sizes move closer to OTP in 456 functions, including 51 that reach OTP's size. The first frame in `erl_error:mf_to_string/3` moves from five slots to four, compared with five in OTP. The aggregate absolute Code size gap drops by 3,017 bytes. The set of normalized instruction matches stays the same, including both matches lost in the initial candidate. The flat tuple ranking falls from 825 functions in 364 modules to 548 functions in 312 modules. [Frame totals](summary.txt) · [Preserved matches](preserved-matches.txt) · [Remaining tuple shapes](tuple-shapes-after.txt).

All five focused tests pass with the final corpus compiler binary. The preliminary candidate's `--block compiler` run passed 3,721 of 3,829 tests. All 108 failing cases reproduced on both the Part 1482 baseline and the final candidate in exact-name reruns. The complete .NET suite was not run on the final candidate. Canonical readiness and publication remain pending. The readiness comparison baseline is the immutable Part 561 snapshot. [Focused results](focused.json) · [Compiler hashes](compiler-hash.txt) · [Compiler block comparison](compiler-block-summary.txt).
