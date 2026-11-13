# Part 1534 — Keep terminal list inputs out of the stack

When the first call consumes the last use of all function inputs and every subsequent list element is an independent call, the compiler passes the inputs from their entry X registers. It reserves Y slots for call results that survive a later call. The final result stays in X0. The emitter reuses existing call argument scheduling, frame initialization and line handling. The pinned OTP compiler is the reference and the implementation adds no dependency.

The complete deterministic fixture matches OTP byte for byte. The release example `httpc_sup:child_specs/1` now uses one stack slot and its instructions match OTP after normalization of label numbers. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_list_terminal_calls.erl) · [Hashes](sha256.txt) · [Release disassembly](httpc-sup.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1534-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1534-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1534-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1534.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
dotnet artifacts/beam-backend/part1534-tests/Erlang.Tests.dll --case compiler/beam/whole-module-matches-otp --case compiler/beam/call-result-case-keeps-register --case compiler/beam/rebuild-installed-otp-release-images
```

## Stack analysis

The baseline scan groups functions where both implementations allocate at entry with the same X live count but the C# compiler reserves more Y slots. In 976 functions across 381 modules, a slot saved before the first call has no later syntactic read. The count is a triage signal. It does not resolve slot identity through frame trimming or compare live ranges along each control-flow path. A larger group reads all its entry slots somewhere later, so distinguishing reads before and after each call is important for follow-up work. [Baseline ranking](entry-spills-before.txt) · [Scanner](entry-spill-ranking.escript).
