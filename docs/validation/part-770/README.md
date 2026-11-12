# Part770 Mnesia checkpoint supervisor release image parity

The BEAM compiler recognizes a nil-headed clause that binds a tuple containing a remote call result, evaluates a second call chain and returns a nested tuple and list. The first tuple lives in Y0 across the calls. The worker tuple, list and enclosing result use one heap check. The lowering accepts only fresh bindings and constant fields apart from the explicitly carried call results. The original Erlang forms remain in Dbgi.

The Windows OTP 29.1.1 image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,300 bytes. The pinned source and release disassembly establish the shape, stack slot, call order and heap reservation. The release profile reuses the Mnesia bootstrap compiler, warning options and vsn insertion. Existing original source and BEAM assets match the installed oracle and are embedded in the release regression. No dependency or license changed. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part770-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part770.json`.

The next candidate, ssl_app, uses a zero-slot frame for consecutive calls. Its logger setup passes a constant map and a tuple containing an external function reference.

All four targeted compiler checks passed, including the permanent release comparison. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 47 Equal, 209 SourceRejected and 1,110 Unsupported. mnesia_checkpoint_sup was the only status change from Part769. [Corpus report](otp-corpus.json).
