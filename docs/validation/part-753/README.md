# Part753 named recursive fun source execution

The parser accepts named fun clauses with a consistent name. The source binding pass adds that name to the clause scope and the capture pass excludes the self binding from the closure environment. Invocation adds the actual function value to the local scope. Parameter patterns that reuse the self name match that value, including on tail-recursive entry.

The implementation follows OTP 29.1.1 `fun_clause` and `build_fun` in `lib/stdlib/src/erl_parse.yrl`. It reuses the C# source closure recipe and continuation trampoline. Dependencies and licenses are unchanged.

The source fixture compares 2,000 tail calls, outer variable shadowing, captured environment sizes, returning the same function, capture from a nested closure and a parameter pattern referring to the function itself. [Oracle script](oracle.eval) and [output](oracle.txt).

The C# BEAM compiler still requires general closure emission for named funs. This part covers source execution and admission, without claiming stack frame or BEAM image parity for the new fixture.

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part753-final-tool`. Run its DLL with the Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part753.json`.

The complete corpus checked 1,366 files and reported 37 Equal, 262 SourceRejected and 1,067 Unsupported. Exactly 15 modules advanced from SourceRejected to Unsupported. Every previously equal image remains equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part753-tests`. All four focused checks passed, including named fun execution and three existing BEAM checks. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
