# Part602 literal dispatch, static external calls, and typed source forms

The backend emits `select_val` for distinct atom or integer keys of one type with distinct constant outcomes and an unconditional fallback. It emits static external tail calls and ordinary calls with indexed imports and call-site line locations. Specialized erlang BIF instructions, dynamic calls, general pattern trees, and broader optimization remain outside this profile.

The source parser retains classic typed record fields and variable type constraints in specs. The C# BEAM preprocessor adds `FILE`, `MODULE_STRING`, `FUNCTION_NAME`, and `FUNCTION_ARITY`. Function context macros require a directly parseable head without macro expansion. The corpus runner searches the wx source directory and the SNMP misc directory for release headers.

Reference sources are [genop.tab](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/compiler/src/genop.tab), [erl_parse.yrl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/erl_parse.yrl), and [epp.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/epp.erl). The admitted OTP compiler accepted the extended typed source control and produced both new whole-image fixtures with `erlc +deterministic`.

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. Five focused cases passed, including seventeen whole-image fixtures. The two new C#-generated images matched OTP bytes and executed successfully on the original VM. [Focused report](focused.json).

`dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <admitted-OTP-root> artifacts/beam-backend/otp-corpus-part602-final.json` exited 1 after checking 1,366 Windows images. It reported 944 source rejections, 421 modules outside the backend profile, one missing source, and zero regenerated release images. The previous run had 224 modules at the backend guard. Full regression, canonical readiness regeneration, and publication remain open. [Per-image report](otp-corpus.json).
