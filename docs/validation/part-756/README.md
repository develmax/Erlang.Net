# Part756 tail recursion in named BEAM closures

A returned named closure with an integer base pattern and one recursive subtraction clause lowers to an exact test, a GC subtraction BIF and a direct tail call to the lifted entry. The callable arity excludes captures. The lifted call includes them, keeps them in place and updates only the counter.

The reference is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation follows the pinned named fun lowering and observed BEAM instruction sequence. It reuses the C# lifted closure writer, arithmetic BIF operands and tail-call instruction. Dependencies and licenses are unchanged.

The fixture contains a countdown with no captures and a countdown that returns a captured value. The entire 828-byte image matches the OTP compiler. The oracle checks 2,000 iterations for both closures, the captured environment and badarith for an atom input. [Oracle script](oracle.eval) and [output](oracle.txt).

This lowering covers the two-clause countdown shape. Guarded clauses, multiple recursive arguments, non-tail calls and more general recursive bodies remain open. In a separate OTP accumulator probe, the decrement is staged in x3 while the original counter remains live as a typed operand for the addition. The compiler then moves the decrement to x0 before the direct tail call.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part756-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part756.json`.

All 1,366 release files were checked. The result was 37 Equal, 262 SourceRejected and 1,067 Unsupported. No module changed status. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part756-tests`. All four selected checks passed, including both recursive calls and the arithmetic error in the .NET BEAM runtime. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
