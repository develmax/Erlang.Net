# Part597 source preprocessing and typed forms

The C# BEAM entry point expands object-like definitions and the `MODULE` and `LINE` built-ins on its token stream. The implementation preserves source locations and bounds expansion. It covers the OTP warning option `{nowarn_possibly_unsafe_function,{Module,Function,Arity}}`. Other admitted functional options remain outside BEAM emission. The parser preserves basic type, opaque, nominal, spec, callback, export_type, and optional_callbacks forms separately from runtime attributes. Parameterized macros, includes, conditionals, constrained specs, type lint, and BEAM type metadata remain open.

Reference sources are [epp.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/epp.erl) and [erl_parse.yrl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/erl_parse.yrl). The typed source control was accepted by pinned OTP. Two new fixture images were captured with OTP `erlc +deterministic` and matched the C# compiler's output byte for byte.

- `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. [Four passing focused tests](focused.json).
- `dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part597-final.json` inspected 1,366 Windows release images and exited 1. [Per-image results](otp-corpus.json).

| Corpus status | Part596 | Part597 |
| --- | ---: | ---: |
| Source rejected | 1,307 | 1,185 |
| Reached backend and was unsupported | 58 | 180 |
| Missing source | 1 | 1 |
| Release image regenerated | 0 | 0 |

Full regression, readiness regeneration, and publication remain outstanding.
