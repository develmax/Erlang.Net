# Part755 multiple BEAM closure captures

Returned local closures can construct a flat tuple from their parameters, constants and captured parent arguments. Captures are collected once in parent register order and appended after the callable parameters of the lifted function. The heap reservation, make_fun3 environment and FunT free-variable count use the same list.

The Windows OTP 29.1.1 oracle fixture captures x0 and x2 while using their values in reverse order in the tuple. It repeats one variable in the tuple and confirms it occupies one environment slot. The full 644-byte image matches the pinned release compiler. The existing zero- and one-capture fixture is preserved. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation reuses the C# tuple constructor, closure heap reservation and FunT writer. Dependencies and licenses are unchanged.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part755-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part755.json`.

All 1,366 release files were checked. There were 37 Equal, 262 SourceRejected and 1,067 Unsupported. No module changed status. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part755-tests`. All four selected checks passed, including .NET invocation and environment order. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).

The next scope is named recursive closures. A pinned OTP probe of a countdown closure uses an exact comparison, a subtraction GC BIF and a direct tail call to the lifted function.
