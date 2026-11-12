# Part598 macro-presence conditionals

The C# BEAM preprocessor handles nested `-ifdef`, `-ifndef`, `-else`, and `-endif`, plus `-undef` for user-defined object macros. Inactive forms are excluded from macro expansion and parsing. The lexer still processes the entire file. Active `-if` and `-elif`, includes, parameterized macros, and undefinition of predefined macros remain outside the supported subset.

The reference is [OTP 29.1.1 epp.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/epp.erl). The conditional fixture was captured with the admitted OTP provider using `erlc +deterministic`. It exercises nested branches, undefinition and redefinition, skipped undefined references, and original `LINE` positions.

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. All five selected tests passed. The new conditional fixture matched the full original BEAM bytes. [Focused report](focused.json).

The corpus command was `dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part598.json`. It exited 1 with 1,180 source rejections, 185 unsupported modules after parsing, one missing source, and zero regenerated images across 1,366 Windows release images. Five entries advanced from source rejection to the emission guard. [Per-image report](otp-corpus.json).
