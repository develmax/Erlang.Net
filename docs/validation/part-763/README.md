# Part763 type declarations in BEAM compilation

The BEAM frontend admits spec, type, opaque and nominal forms when assigning function locations. Release debug metadata retains their OTP abstract forms. A nominal declaration also remains in Attr in source order. Type annotations follow explicit file remapping, including nested types and declaration variables.

The reference is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The fixture combines a parameterized union type, an opaque type, a nominal type and a spec after an explicit file directive. Its 608-byte standalone image and all ten abstract forms match OTP. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation reuses the C# type parser, spec writer and attribute writer. Dependencies and licenses are unchanged. General callbacks and type documentation remain open. A separate OTP probe showed that Docs includes private types referenced by exported specs. Modules combining documentation with type definitions are explicitly rejected until those entries are supported.

A direct compile of the installed yeccparser now passes the yecc_ret type form and reaches targeted inline lowering. The first unsupported target is yeccpars2_6_/1, which replaces the head of the stack with symbol(Head) and retains the tail.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part763-verified-tool`. Ran its DLL with the Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part763-verified.json`.

All 1,366 release files were checked. The report has 37 Equal, 4 Different, 209 SourceRejected and 1,116 Unsupported. Four previously unsupported modules now emit images. No previously equal module changed status. [Corpus report](otp-corpus.json) and [chunk differences](chunk-differences.txt).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part763-tests`. All four focused checks passed. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
