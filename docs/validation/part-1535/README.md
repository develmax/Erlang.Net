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

## Next release candidate

The normalized comparison identifies `httpc_sup:init/1` as the only instruction mismatch in its module. OTP checks the singleton list at entry, then calls with the extracted head and builds two nested tuples from a constant and the X0 result in a zero-slot frame. The current compiler saves the list, its head and tail, and the three literal bindings. This is a candidate for extending the existing call-result construction path with a singleton-list entry pattern and a prepared call operand. [Corpus proximity ranking](nearest-before.txt) · [Instruction listing](httpc-init.txt).

## Results

All 1,366 release sources were accepted and generated. There are 387 byte-identical images and 979 different images. ELDAPv3 remains byte-identical from C# ASN.1 generation. OTP loads all 979 differing images. [Corpus](otp-corpus.json) · [Loader and Code](loader-and-code.txt).

Two release images changed. Three functions reach the OTP frame size, two reach normalized instruction equality, and the aggregate Code size gap drops by 41 bytes. Three focused compiler checks and 64 value comparisons passed. The full runtime suite and canonical readiness were not rerun. [Frames](frame-delta.txt) · [Functions](normalized-functions.txt) · [Focused tests](focused.json) · [Oracle](fixture.txt).
