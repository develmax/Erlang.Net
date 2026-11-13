# Part 1544 — Trim frames for terminal list tails with computed arguments

The terminal list branch planner now accepts a saved head and a two-argument tail call whose second argument subtracts two saved values. It tracks reads inside the subtraction, writes the result directly to X1, and preserves only the head across the call. The emitter builds the final cons from the saved head and the returned X0 tail. It reuses arithmetic and stack trimming instructions. OTP 29.1.1 remains the reference and the change adds no dependency.

The probe exercises recursive cons construction, numeric and invalid arguments, and process dictionary effects. All 44 observations match both OTP and the previous compiler. [Probe](beam_trim_list_branch.erl) · [Instructions](fixture-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1544-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1544-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1544-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1544.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
