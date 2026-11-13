# Part 1531 — Reuse stack-free throw analysis in terminal tuple alternatives

The terminal tuple case emitter now applies the existing throw analysis to its atom-tail call plan. When the analysis proves that no handler observes the stack, it emits erlang:raise/3 with an empty stack. The reason can come from a literal, a saved operand, or a constructed argument. Calls whose handlers observe the stack still emit erlang:throw/1.

The pinned OTP 29.1.1 beam_ssa_throw pass is the reference. The change reuses the compiler's handler analysis, saved arguments and argument construction planner. No dependency was added.

The oracle checks a handler that ignores the stack and a handler that returns it. Both have the expected instructions and match the OTP results and observable module frames. The probe's surrounding protected functions still differ in whole-image comparison. [Probe source](beam_case_tail_raise.erl) · [Oracle](compare-throw.escript) · [Results](probe.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1531-tool
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1531-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1531-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
