# Part 1533 — Forward multiple tuple fields to terminal calls

The terminal tuple branch can now pass several fields of the same tuple to a tail call. The planner keeps the source tuple register live until the last extraction. When an earlier field targets that register, the existing argument move scheduler restores the call argument order after extraction.

The change reuses the tuple pattern validator, selector save plan and tail-call emitter. OTP 29.1.1 is the reference. No dependency was added.

The deterministic fixture covers two projected arguments and one saved argument. It matches OTP byte for byte and agrees on 48 values, errors and fixture stack observations. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_case_multi_projected_tail.erl) · [Hashes](sha256.txt) · [Oracle output](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1533-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1533-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1533-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1533-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
