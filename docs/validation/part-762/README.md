# Part762 targeted inline stack reduction

The targeted inline pass recognizes a reduction that matches one stack cons and reconstructs it from the same head and tail. It keeps the match, returns the original term and avoids allocating a new cons. The BEAM sequence checks is_nonempty_list and emits a badmatch at the original match's file and line. It uses the caller's argument register, including x1 when the stack is the second argument.

The reference is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The 608-byte image matches OTP byte for byte. The oracle confirms that an improper cons is returned as-is and an empty list produces badmatch at grammar.yrl:102. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation reuses the C# inline rewrite and BEAM list, move and badmatch instructions. Dependencies and licenses are unchanged. The lowering covers direct calls from single unconditional clauses. Multi-element reductions and arbitrary semantic actions remain open.

A direct compile of the installed yeccparser source stops in BeamSourceForms.FunctionLines before targeted inline lowering. Its first unsupported form at this stage is the yecc_ret type declaration. This blocks admission independently of the remaining reduction bodies.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part762-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part762.json`.

All 1,366 release files were checked. The report has 37 Equal, 209 SourceRejected and 1,120 Unsupported. No release module changed status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part762-tests`. All four focused checks passed, including the .NET badmatch location. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
