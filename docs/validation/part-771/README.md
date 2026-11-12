# Part771 SSL application release image parity

The BEAM lowering emits static calls whose arguments are constant in a zero-slot stack frame. Intermediate results are discarded in source order. The final expression can return a constant or tail-call the next function. Constant tuples may contain external function references, which use the existing ETF export encoding. Local calls are included in the reachability walk.

The Windows OTP 29.1.1 ssl_app image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,632 bytes. The pinned source and release disassembly establish call order, frame sizes and literal contents. The release profile reuses the SSL bootstrap compiler, includes, warnings and VSN definition. The pinned source and image are embedded in the release regression. Dependencies and upstream licenses are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part771-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part771.json`.

All four targeted compiler checks passed, including the permanent release comparison. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 48 Equal, 209 SourceRejected and 1,109 Unsupported. ssl_app was the only status change from Part770. [Corpus report](otp-corpus.json).
