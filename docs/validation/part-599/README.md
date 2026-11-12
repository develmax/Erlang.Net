# Part599 parameterized macros and expression conditionals

The BEAM preprocessor supports parameterized macro definitions and arity overloads, including zero arguments. It separates arguments across nested punctuation and Erlang blocks, substitutes tokens while preserving argument locations, and bounds expansion. It also evaluates a subset of if and elif conditions with literals, defined, OTP_RELEASE, comparisons, not, and short-circuit conjunction and disjunction.

The reference is [OTP 29.1.1 epp.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/epp.erl). The admitted OTP provider built `beam_macro_args_probe` and `beam_if_probe` with `erlc +deterministic`. The fixtures cover overloads, nested invocations, multiline LINE, and a selected elif arm followed by an inactive undefined macro. Malformed argument declarations, arity mismatch, and invalid condition forms are checked separately.

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. All five selected tests passed, including nine whole-image comparisons. [Focused report](focused.json).

The corpus command was `dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part599-expr.json`. It exited 1 with 1,175 source rejections, 190 unsupported modules after source admission, one missing source, and zero regenerated images. Five Windows release entries moved from source rejection to the backend guard. [Per-image report](otp-corpus.json).

Macro stringification, includes, full conditional guard support, broader lowering, full regression, and canonical readiness regeneration remain open.
