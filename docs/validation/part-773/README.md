# Part773 terminal signal-handler release image parity

The BEAM compiler checks required atom keys before executing exact-match calls. The map remains live across those calls, and failure labels retain the corresponding assertion source line. A two-atom guard can select a send branch that extracts two fields from the state map, constructs the message and returns the original map. The debug writer preserves alias and exact map patterns.

The Windows OTP 29.1.1 prim_tty_sighandler image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,320 bytes. The reference source, opcode table and release disassembly establish the instruction shapes, register allocation and branch order. The existing C# operand, typed register and release metadata writers are reused. The pinned source and image are embedded in the permanent regression. Dependencies and notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part773-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part773.json`.

The next candidate is sasl_report_tty_h. Its guarded clause compares a tuple element with another function argument, then compares node/1 with node/0 before falling back to report formatting.

The full corpus run checked 1,366 release files and reported 50 Equal, 209 SourceRejected and 1,107 Unsupported. prim_tty_sighandler was the only status change from Part772. [Corpus report](otp-corpus.json).

All four targeted checks passed, including the permanent release comparison. Concurrent builds first collided on a shared intermediate assembly. The isolated test build succeeded. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.
