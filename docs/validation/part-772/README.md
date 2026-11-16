# Part772 kernel signal-handler release image parity

The BEAM compiler emits atom dispatch for clauses that call remote functions with constant arguments and return a tagged tuple carrying the original state. It saves state in Y0 across the calls. Exact result matches branch to badmatch with the original source line and result. The fallback returns without a frame. An empty legacy record can be normalized to a constant tuple in a function body. Attribute literals retain their existing rules.

The Windows OTP 29.1.1 erl_signal_handler image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,820 bytes. The reference source and release disassembly establish the branch order, frame and assertion. The profile reuses the kernel bootstrap compiler, include path and warning options. The original source and image are embedded in the permanent regression. Dependencies and notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part772-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part772.json`.

The next candidate is prim_tty_sighandler. The pinned code tests map fields before its checked calls and extracts fields before selecting a message-send branch.

All four targeted compiler checks passed, including the permanent release comparison. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 49 Equal, 209 SourceRejected and 1,108 Unsupported. erl_signal_handler was the only status change from Part771. [Corpus report](otp-corpus.json).
