# Part 1545 — Order measured list frames and emit if_end

Measured entry sequences with terminal list branches now reserve the call result and its size in the low Y slots. Entry inputs occupy the remaining slots in reverse register order. The terminal branch planner can then relocate live values and trim with the same slot layout as OTP.

The parser records the source location of if. When all guards in a terminal list conditional fail, the emitter writes that location and emits if_end. The probe verifies 44 values, error reasons and process dictionary effects with OTP 29.1.1. The change adds no dependency. [Probe](beam_trim_list_branch.erl) · [Instructions](fixture-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1545-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1545-final-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1545-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1545-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
