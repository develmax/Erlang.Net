# Part776 Inets timestamp utility release image parity

The BEAM compiler emits a typed clock delta, clock-unit conversion and integer division. A zero-argument remote call can forward its result directly to a local tail call. The timestamp formatting flow checks tuple shapes, preserves the original input, emits float division and rounding, and builds the formatting list in one heap reservation. Failure labels preserve the value and source line for badmatch.

The Windows OTP 29.1.1 inets_lib image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,708 bytes. The reference source, opcode table and release disassembly establish operand types, register scheduling and metadata. The implementation reuses the C# call and literal writers and Inets release profile. The existing original source and image are embedded in the permanent regression. Dependencies and notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part776-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part776.json`.

The next candidate is ssl_admin_sup. The pinned init/1 has 23 instructions, and manager_opts/0 has 41.

All four targeted checks passed, including the permanent release comparison. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 53 Equal, 209 SourceRejected and 1,104 Unsupported. inets_lib was the only status change from Part775. [Corpus report](otp-corpus.json).
