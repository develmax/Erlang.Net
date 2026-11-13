# Part 1538 — Keep the final binary segment call result in X0

When the final binary segment reads a call result and has a literal size, the compiler passes X0 directly to bs_create_bin. The heap check sees one live X register. The other segments retain their evaluation order and existing operand handling. The path applies to ordinary binary construction. OTP 29.1.1 supplies the reference and the change adds no dependency.

The probe verifies the register operand and agrees on five results, errors and fixture stack observations. The probe still has an extra stack slot for its original argument and a repeated line instruction. [Source](beam_final_binary_call.erl) · [Instructions](fixture-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1538-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1538-tests-final
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1538-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1538-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

The final compiler also releases temporary slots belonging to the size operand once the size instruction has consumed it. Bindings still visible to the surrounding expression keep their slots through the existing operand release helper.
