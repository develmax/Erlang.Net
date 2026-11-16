# Part757 accumulator recursion in named BEAM closures

The supported two-clause named closure updates a counter and an accumulator and adds a captured offset in its integer base branch. It stages the decrement in the next free X register. The addition consumes the original counter as a typed number after subtraction has validated it. The decrement moves to x0 before the direct lifted tail call. The captured offset stays in place.

The pinned Windows OTP 29.1.1 compiler is ad05823719d77c8faee87348ea39513d4e2f99c5. The entire 700-byte fixture matches OTP, including Code, FunT and Type. The oracle checks 2,000 iterations, the zero case, capture and badarith for an invalid accumulator. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation reuses C# closure, arithmetic and typed register writers. Dependencies and licenses are unchanged. The supported accumulator lowering covers the shown addition and decrement pattern. More general recursive bodies and closures embedded in returned data remain open.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part757-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part757.json`.

The corpus checked all 1,366 files and reported 37 Equal, 262 SourceRejected and 1,067 Unsupported. Every module retained its prior status. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part757-tests`. All four selected checks passed, including accumulator invocation, the zero case and badarith in .NET. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
