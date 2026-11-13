# Part 1535 — Reuse terminal list input slots after the final read

The terminal list planner records the last call that reads each entry argument. It keeps those arguments in Y across earlier calls and reserves slots for earlier results that remain live until the list is built. When an input's last read is its only use in the next call and supplies X0, the emitter swaps it with the previous result. Both values reach their required locations in one instruction.

The emitter reuses the existing call operand scheduler. It uses the existing generic emission path when the calls require operands or slot relocations this planner cannot represent. OTP 29.1.1 is the reference and the implementation adds no dependency.

The fixture covers two and three calls, inputs reused after multiple calls and two saved inputs. The complete C# image matches OTP byte for byte. [Source](../../../tests/Erlang.Tests/Resources/Beam/beam_list_live_inputs.erl) · [Hashes](sha256.txt) · [Instructions for repeated reads](last-use.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1535-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1535-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1535-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1535.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1535-tests/Erlang.Tests.dll --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-case-keeps-register --case compiler/beam/rebuild-installed-otp-release-images
```
