# Part760 explicit file directives

The BEAM preprocessor accepts explicit file directives after macro expansion, including concatenated filename strings. It remaps subsequent tokens, updates FILE and LINE macros, and preserves the directive itself as a generated file form in release debug metadata. Same-line tokens use the scanner's reset column. A return to the original filename uses the default Line table file.

The pinned oracle is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The 820-byte deterministic image matches byte for byte. All eleven abstract forms match the OTP debug oracle, including generated annotations, remapped columns and EOF. A separate multiline directive with a macro line number and concatenated filename also matched its abstract forms. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation follows epp.erl file remapping and reuses the C# token, source line and release metadata infrastructure. Dependencies and licenses are unchanged. Combined include and explicit file transition debug parity remains open. The generated C# source frontend retains its existing preprocessing scope.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part760-tool`. Ran its DLL with the Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part760.json`.

All 1,366 files were checked. Status counts remain 37 Equal, 262 SourceRejected and 1,067 Unsupported. All 14 earlier file-attribute errors cleared. Thirteen generated parsers now stop at targeted inline compile options, and erl_parse stops at an expected-atom diagnostic. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part760-tests`. All four focused checks passed. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
