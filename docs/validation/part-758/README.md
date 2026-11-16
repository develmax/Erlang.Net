# Part758 local closures returned in tuples

The compiler emits a returned flat tuple containing one supported closure, with remaining operands drawn from parent arguments or simple literals. It combines the tuple and closure heap reservations. Registers still needed by the tuple remain live, and make_fun3 writes to the first available X register. The tuple uses the resulting closure register alongside the retained arguments.

The Windows OTP 29.1.1 compiler at ad05823719d77c8faee87348ea39513d4e2f99c5 is the oracle. The complete 820-byte fixture matches OTP. It covers a tagged accumulator closure and a tuple retaining x0 and x1 while writing the closure to x2. The oracle calls both closures and verifies 2,000 iterations and the captured value. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation reuses the closure, heap, compact operand and tuple instruction writers. Dependencies and licenses are unchanged. Multiple closures in the same tuple and nested constructors remain open. A separate pinned OTP probe of two closures uses one heap reservation and emits fun-1 before fun-0, keeping the capture of the second closure live during the first construction.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part758-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part758.json`.

The complete corpus checked 1,366 files and reported 37 Equal, 262 SourceRejected and 1,067 Unsupported. Every file retained its previous status. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part758-tests`. All four selected checks passed, including .NET invocation of both closure forms and preservation of the tuple arguments. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
