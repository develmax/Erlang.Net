# Part759 multiple closures in returned tuples

The compiler plans all closures in a flat returned tuple before writing instructions. It reserves heap space for the complete tuple and its closures in one instruction. A register remains live until the last closure that captures it has been created. Tuple operands and previously created closure registers stay live until tuple construction.

The Windows OTP 29.1.1 compiler at ad05823719d77c8faee87348ea39513d4e2f99c5 is the oracle. The 1,164-byte fixture matches OTP byte for byte and includes six closures across independent, shared and mixed recursive captures. Anonymous ordinals run in reverse construction order and recursive ordinals run forward within their own sequence. An additional local probe matched three recursive and two anonymous closures. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation reuses the C# closure planner, heap allocation and tuple writers. Dependencies and licenses are unchanged. Nested constructors and general closure expressions remain open.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part759-final-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part759-final.json`.

The complete 1,366-file corpus reported 37 Equal, 262 SourceRejected and 1,067 Unsupported. No release module changed status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part759-tests`. All four selected checks passed, including invocation of all six emitted closures. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).

The next corpus input gap is explicit file directives. The preceding report contains 14 input rejections for that attribute, including generated parser modules.
