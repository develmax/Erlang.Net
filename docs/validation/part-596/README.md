# Part596 OTP documentation source forms

The lexer handles unprefixed triple-quoted strings using the delimiter width and indentation rules of pinned OTP 29.1.1. The parser admits `doc` and `moduledoc` with strings, booleans, or `hidden`. It keeps that metadata separate from runtime attributes. Documentation maps, binary values, and BEAM Docs chunk emission remain open.

The source fixture compares two verbatim strings with values captured through OTP `erl_scan:string/1`. It also checks documentation storage and the BEAM guard. References are [erl_scan.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/erl_scan.erl) and [erl_parse.yrl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/erl_parse.yrl).

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. The [focused harness report](focused.json) records 3/3 passing tests, including both existing BEAM emission checks.

The Windows corpus command was `dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part596.json`. It exited 1 as a failed byte-parity gate. The [per-image report](otp-corpus.json) contains 1,307 source rejections, 58 unsupported modules after parsing, one missing source, and zero regenerated release images. Part595 reported 1,364 source rejections and one unsupported module. Full regression and canonical readiness regeneration remain outstanding.
