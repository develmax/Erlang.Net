# Part735 HTTP acceptor supervisor call and constructor lowering

The compiler checks a three-element list prefix in registers, retains the original list across the calls and trims the frame before the last external call. The singleton list constructor path also supports a local call whose result is wrapped in a list and used by the return constructor chain. It reserves the complete result heap after the call. Other source shapes retain existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The acceptor uses the existing HTTP server release profile. The compiler reuses the list matcher instructions, constructor chain and constant and call writers. Dependencies and licenses are unchanged.

OTP disassembly matches for all 30 instructions of start_link/1 and all 17 instructions of init/1. The generated image loaded in OTP and matched the reference init result in both ListenInfo branches. The two entry points rejected the selected malformed inputs with function_clause. The remaining chunks are AtU8, Code, ExpT, LocT and Attr. The permanent partial comparison covers ImpT, LitT, CInf, Dbgi, Docs, Line and Type. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part735-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part735.json.

The next step is child_spec/1. The current code emits 109 instructions while OTP emits 94. Align its stack slot allocation, construction order in both branches and resulting atom and label tables.

The corpus checked 1,366 files and reported 29 Equal, 8 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part735-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
