# Part754 local closure BEAM emission

The compiler emits a local closure returned by an unconditional function when the closure has one unguarded clause that returns a constant, a formal argument or one captured parent argument. The lifted function follows the source functions and module metadata functions. The caller emits a heap allocation and `make_fun3`, and `FunT` links the closure index to its lifted label. The module digest uses zeroed old uniqueness fields before finalizing them with OTP's 27-bit prefix.

The pinned reference is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation follows `beam_asm.erl`, `beam_dict.erl` and `genop.tab`. It reuses the C# operand encoder, container writer and runtime closure loader. Dependencies and licenses are unchanged.

The fixture includes a named fun without self references and an anonymous fun that captures a parent argument. Its complete 756-byte standalone image matches OTP, and both release-profile abstract function forms match separately. A local probe also matched an image capturing the second parent register with nonzero callable arity. [Oracle script](oracle.eval) and [output](oracle.txt).

The remaining closure work includes self recursion, multiple captures, patterned and guarded clauses and nested closure creation. A self reference in a parameter pattern stays outside the supported emitter shape.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part754-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part754.json`.

All 1,366 files were checked. The corpus reported 37 Equal, 262 SourceRejected and 1,067 Unsupported. Every file retained its prior status. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part754-tests`. All four selected checks passed, including invocation of the emitted closures through the prepared module registry. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
