# Part751 adjacent string literals

The parser consumes consecutive ordinary string tokens as a single Erlang character list after preprocessing. This includes tokens separated by comments or introduced by macro expansion. The existing bit-syntax parser receives the combined string as one segment. Binary sigils remain separate expression tokens.

The lexer rejects a new double quote immediately after a string terminator. The oracle is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation follows `strings` in `lib/stdlib/src/erl_parse.yrl` and `scan_string_concat` in `lib/stdlib/src/erl_scan.erl`. It uses the existing token stream and Erlang string representation. Dependencies and licenses are unchanged.

The fixture `beam_adjacent_strings_probe.erl` uses a macro, an empty literal, a comment, Unicode text and a bit-syntax segment. OTP builds its expected image with `compile:file(Path,[deterministic,{outdir,Dir}])`. The release debug test checks the merged abstract string and the single binary element. The regression also checks rejection of `"a""b"`. The OTP scanner returned `string_concat` at line 1, column 4 for that input. [Oracle script](oracle.eval) and [output](oracle.txt).

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part751-final-tool`. Run its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part751-final.json`.

The final run checked 1,366 files and reported 37 Equal, 280 SourceRejected and 1,049 Unsupported. Exactly 208 modules moved from SourceRejected to Unsupported. All earlier equal images remain equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part751-tests`. All three selected compiler cases passed. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).

The next confirmed input gaps are named recursive funs and dynamic remote funs. Examples include `fun RF() -> ... end` in `dbg_ieval`, `fun SquashDuplicates(...) -> ... end` in `dialyzer_typesig` and `fun Driver:send/2` in `inet_tcp_dist`.
