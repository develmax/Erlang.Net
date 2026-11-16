# Part 1482 — Evaluate entry type and equality guards in X

The OTP 29.1.1 disassembly shows 965 functions across 351 modules whose source starts with a type guard on variable parameters and whose generated code allocates a frame where OTP performs a test. This count covers candidate source shapes. The compiler applies its entry frame planner when all clause parameters are unconditional and the existing guard planner accepts each guard. It emits simple terminal tuple and list constructions directly from entry registers. The OTP compiler places frames and reserves registers in `beam_ssa_pre_codegen`; this change reuses the C# entry guard and construction planners. [Baseline frame ranking](frame-ranking-before.txt) · [Type-guard candidates](type-guard-ranking-before.txt).

The permanent `guarded_shape/2` fixture exercises a conjunction of type guards, exact equality, and fallback. Its reference and current instruction sequences match after normalizing labels. The previous build allocated two Y slots before the first type test. The pinned OTP VM returns the same values from all three fixture images, including the exact comparison of an integer and a float. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_entry_result_case.erl) · [Probe](probe.txt).

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1482-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1482-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1482-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1482.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1482-tests/Erlang.Tests.dll --case compiler/beam/documentation-payload-matches-otp --case compiler/beam/rebuild-installed-otp-release-images --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-bif-keeps-register --case compiler/beam/call-result-case-keeps-register --report artifacts/beam-backend/part1482-focused.json
dotnet artifacts/beam-backend/part1482-tests/Erlang.Tests.dll --report artifacts/beam-backend/part1482-full-tests.json
```

All 1,366 Windows OTP 29.1.1 sources are admitted and generate a BEAM. The comparison records 385 Equal and 981 Different, including an Equal `ELDAPv3` from the C# ASN.1 generator. No image changes its equality status. OTP accepts both images in all 981 differing pairs. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader](loader-and-code.txt).

The change affects 365 release images. Another 205 function bodies across 128 modules reach normalized instruction equality, and all previous normalized matches are retained. For example, `xmerl_xsd_type:conv_list_to_binary/1` now tests binary and list in X and emits the external tail call. The ranked type-guard mismatch falls from 965 functions in 351 modules to 224 in 128. [Normalized delta](normalized-summary.txt) · [Release example](xmerl-guard.txt) · [Remaining candidates](type-guard-ranking-after.txt).

Of the 992 changed instruction bodies, 298 have a measured frame size closer to OTP, including 283 that reach OTP's size. The first reported frame moves farther from OTP in `sys_core_fold:update_let_types/3` (OTP 2, previous 3, current 0). The aggregate absolute Code size gap falls by 7,198 bytes. [Frame totals](summary.txt) · [Per-function frames](frame-delta.txt).

All five focused .NET checks pass with the same compiler binary as the corpus tool. The full .NET run is in progress at this checkpoint. Canonical readiness and publication remain pending. The readiness comparison baseline is the immutable Part 561 snapshot. [Focused tests](focused.json) · [Compiler hashes](compiler-hash.txt).
