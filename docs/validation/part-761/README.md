# Part761 targeted inline options

The parser accepts the single-signature and list forms of the targeted inline option and retains their declarations. The BEAM pass replaces direct local calls to unguarded constant and argument-returning functions when every argument is a variable or a compile-time constant. It drops the private inline targets from emitted code and retains their original abstract forms. Calls and targets outside the supported lowering are rejected explicitly.

The pinned Windows OTP 29.1.1 compiler is ad05823719d77c8faee87348ea39513d4e2f99c5. The complete 612-byte fixture and all ten abstract forms match OTP. The oracle verifies the selected argument and constant return. The regression also checks rejection when an argument would produce a side effect. [Oracle script](oracle.eval) and [output](oracle.txt).

The implementation follows the targeted option forms in sys_core_inline.erl and reuses the C# compile attribute, constant folding and function emission infrastructure. Dependencies and licenses are unchanged. Arbitrary body substitution, nested call inlining and effectful arguments remain open.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part761-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part761.json`.

All 1,366 release files were checked. The report has 37 Equal, 209 SourceRejected and 1,120 Unsupported. Exactly 53 modules changed from SourceRejected to Unsupported. No earlier equal image regressed, and no targeted inline option rejection remains. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part761-tests`. All four focused checks passed. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).

The next scope is the generated parser reduction that matches a stack cons and reconstructs it. The pinned OTP probe emits an is_nonempty_list test, a return of the original stack and a source-located badmatch branch.
