# Part 1536 — Construct results after a singleton-list entry match

The C# compiler checks that the argument is a one-element proper list, passes its head directly to a call, and constructs the result from X0 and folded constants. The stack frame has zero Y slots. Both tests branch to the function entry failure label so malformed arguments retain the original function-clause error and arguments. The change reuses the existing call-result constructor and literal folding. OTP 29.1.1 remains the reference and the change adds no dependency.

The deterministic fixture covers a bound call result with nested constant tuples and a call inside a tuple. The entire image matches OTP byte for byte. Fourteen observations of results, errors and fixture stack frames also match. [Source](../../../tests/Erlang.Tests/Resources/Beam/beam_singleton_call_construct.erl) · [Hashes](sha256.txt) · [Oracle](fixture.txt) · [Instructions](fixture-instructions.txt).

## Reproduce

```powershell
dotnet build tools/Validation/Erlang.BeamCorpus -m:1 -o artifacts/beam-backend/part1536-tool
dotnet build tests/Erlang.Tests -m:1 -o artifacts/beam-backend/part1536-tests
$otp = Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1536-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1536.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
```

## Release example

The complete Windows release image for `httpc_sup` now matches byte for byte. Its `init/1` reads the list head in X1, checks the tail in X2 and passes the head to `child_specs/1` without reserving a Y slot. The tuple construction consumes the call result from X0. [Image hashes](httpc-sha256.txt) · [Instructions](httpc-init.txt) · [Chunk comparison](httpc-chunks.txt).
