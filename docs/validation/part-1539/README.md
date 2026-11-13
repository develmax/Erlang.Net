# Part 1539 — Reuse the measured record field in binary construction

The binary emitter reads a bound record field once when a size measurement and the following binary segment use that same field. The original access still validates the record before the size instruction. The size instruction writes directly into its destination Y slot and the data segment uses the saved field. The rule matches the OTP use of a single field extraction for these adjacent operands. It reuses existing record access and GC BIF encoding with no new dependency.

The oracle covers two functions, binary and unaligned bitstring inputs, invalid record tags, and invalid payloads. All 12 observations match the previous compiler. Values and error reasons match OTP. Four invalid-record cases retain the existing difference in the source line of the stack frame. The probe still uses a stack frame and lacks the operand types emitted by OTP. [Source](beam_measured_record.erl) · [Instructions](fixture-instructions.txt) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1539-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1539-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1539-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1539.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
