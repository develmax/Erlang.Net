# Part 1532 — Forward tuple fields into terminal tail calls

The terminal tuple-case emitter now recognizes a tail call that reads one projected field and any required original arguments. The selector saves only the inputs read by the arms or the pattern checks. After the checks, the projection stays in X and the tail call loads any saved arguments directly from Y.

The change reuses the existing entry call, tuple projection and tail-call emitters. The reference is OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency was added.

The deterministic fixture covers recursive lookup, the original-argument atom alternative, malformed tuples and invalid input. The complete fixture matches OTP and 24 result, error and fixture-stack observations agree. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_case_projected_tail.erl) · [Oracle](compare-projected-tail.escript) · [Hashes](sha256.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1532-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1532-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1532-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1532-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
