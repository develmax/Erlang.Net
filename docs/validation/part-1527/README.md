# Part 1527 — Preserve the atom alternative's returned input

Terminal tuple projections can return an original argument when the call returns the alternative atom. The projection planner adds that argument to the saved inputs, keeps the selected tuple field in X, and loads the saved input only on the alternative path. The tuple and atom checks continue to enforce the source pattern. Existing atom type proofs annotate saved equality operands in normal function entries.

The fixture checks a caller-supplied default, exact integer and float key comparisons, and error behavior. Both complete images match, and the OTP oracle agrees on 160 values, exceptions and fixture stack observations. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_case_saved_default.erl) · [Hashes](sha256.txt) · [Oracle](fixture.txt).

## Commands

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1527-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1527-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1527-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1527-final.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```
