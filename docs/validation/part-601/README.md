# Part601 header line metadata and BEAM lowering

The deterministic profile now retains the original filename and line of supported functions declared in headers. It writes file transitions and basename entries in the BEAM Line chunk. Supported source forms are still constrained by the backend.

New lowering covers one atom or integer pattern followed by an unconditional fallback, both with constant results. Local call arguments can be variables and atom or integer literals. Register moves preserve dependencies and swaps implement cycles. An ordinary local call can be followed by a constant or by a single incoming argument restored from a Y slot. General pattern trees, guarded clauses, external calls, and wider live frames remain outside this profile.

Three fixture modules were compiled using the admitted OTP 29.1.1 provider with `erlc +deterministic`. The C# outputs matched all bytes and ran in the original VM. The fixture assertions covered the included function, both pattern branches including rejection of 42.0 for pattern 42, register cycles and duplicates, and ordinary calls that return a constant or preserve an incoming argument. Reference sources are [beam_asm.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/beam_asm.erl) and [genop.tab](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/genop.tab).

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. Five focused cases passed, including fifteen complete image comparisons. [Focused report](focused.json).

`dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part601-final.json` exited 1. Of 1,366 installed Windows images, 1,141 source files were rejected, 224 modules reached the backend and were unsupported, and one image had no matching source. No release images were regenerated. Full regression, canonical readiness regeneration and publication remain open. [Per-image report](otp-corpus.json).
