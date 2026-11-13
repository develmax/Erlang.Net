# Part 1546 — Type ranges and guard locations

The compiler carries the lower bound from an entry guard into the type annotations of measured terminal list branches. It encodes that bound even where Erlang's disassembler displays the operand type as any. Debug forms for if clauses now take their location from the first token of the guard expression after opening parentheses.

The Windows OTP 29.1.1 release image for prim_buffer matches the C# output byte for byte. The release-image regression test regenerates it from the embedded Erlang source. [SHA-256](prim-buffer.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1546-final-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1546-final-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1546-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1546-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
