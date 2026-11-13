# Part 1528 — Plan effects in both branches of terminal tuple cases

The register call-sequence planner now handles a branch that performs calls and returns a constant. The matched tuple can project its field directly into the next call's argument register when the field is dead after that call. When a call leaves one saved value live and the sequence has no spills, the emitter can relocate the value to the last slot and trim the unused prefix.

The change reuses the existing tuple checks, call scheduling and frame emission. The reference is OTP 29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added. The release examples are disk_log_1:compress_data/3 and logger_std_h:compress_data/3.

The deterministic fixture reproduces both branches, including recursive transfer and the final effects before a constant return. The oracle checks empty, short and multichunk input by comparing output after decompression. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_tuple_case_effects.erl) · [Oracle](effects-oracle.escript).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1528-release-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1528-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1528-release-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1528-release.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
