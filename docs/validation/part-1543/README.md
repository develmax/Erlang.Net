# Part 1543 — Trim stack frames within terminal list branches

The emitter plans each terminal if branch that returns a singleton list after ordinary calls. After preparing the next call, it retains only the stack values needed by subsequent calls or the list constructor. It moves live values out of the removed prefix, emits trim, and returns with the branch's remaining frame. The split_binary projection still stays in X0. Branches outside the supported forms use the existing expression emitter. The change reuses call and pattern validation and adds no dependency.

In prim_buffer:read_iovec/2, four trim instructions now appear with the same operands as the corresponding OTP instructions. The recursive cons branch still uses the generic emitter. The probe covers different branch lifetimes, invalid sizes and process dictionary effects, and compares 28 observations against OTP and the previous C# compiler. [Release instructions](prim-buffer.txt) · [Probe](beam_trim_list_branch.erl) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1543-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1543-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1543-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1543.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
