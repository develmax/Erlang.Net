# Part 1537 — Emit bitstring size as a GC BIF in function bodies

The general expression emitter lowers resolved erlang:byte_size/1 and erlang:bit_size/1 to gc_bif. A saved operand can stay in Y, and a computed operand stays in X0. Local functions with the same name still use the normal call path. The emitter reuses the OTP-compatible instruction encoder from guard emission. OTP 29.1.1 is the reference and the change adds no dependency.

The probe compares 15 return values, errors and fixture stack observations, including an unaligned bitstring and a shadowing local function. The probe still differs from OTP in frame placement and operand type annotations. [Source](beam_size_value.erl) · [Instructions](size-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1537-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1537-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1537-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1537.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
