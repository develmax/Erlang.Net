# Part 1530 — Preserve saved inputs for terminal atom tail calls

Terminal tuple projection now plans the alternative's tail call using the original arguments it needs. The successful tuple branch can return the selected field directly in X0. The alternative can load its arguments from Y or build an argument tuple from Y before the tail call. The generic call planner resolves local calls and control transfers such as erlang:throw/1.

The deterministic fixture covers a local tail call, a constructed throw argument, a failed exact key match, and malformed input. Its complete image matches OTP 29.1.1, and 80 return values, exceptions and fixture stack observations agree. No dependency was added. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_case_tail_saved.erl) · [Hashes](sha256.txt) · [Oracle](compare-tail-branches.escript) · [Results](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1530-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1530-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1530-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1530-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
