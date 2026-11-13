# Part 1541 — Measure list arity before allocating a selector frame

The emitter evaluates length/1 in the entry registers before the selector call. It saves the key and the original list for the terminal alternatives. Successful length/1 proves the saved value is a proper list, so the dynamic-function guard can measure it with typed operands and test the selected function directly in X0. Both successful branches use tail calls. The unmatched case retains the original selector result and case location.

The complete Windows OTP 29.1.1 release image of shell_default matches byte for byte. A separate probe compares 24 values, errors and fixture stack observations. It exercises missing and wrong-arity functions, malformed argument lists, and successful dynamic application. The probe comparison covers execution, while the release regression checks the full image. The change reuses call scheduling, type encoding and tail-call emission with no new dependency. [Release hashes](sha256.txt) · [Instructions](shell-default.txt) · [Probe](beam_length_selector_apply.erl) · [Oracle](fixture.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1541-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1541-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1541-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1541.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
