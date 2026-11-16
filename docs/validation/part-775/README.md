# Part775 Mnesia logged apply and catch release image parity

The BEAM lowering preserves the four incoming values across logging, installs the exception handler after logging and calls apply/3 inside the protected region. It tests the exception class and selects a literal exit reason. Other failures build the stacktrace before notification. Both handlers preserve the original reason across the notification call and exit with it. The debug writer reconstructs the implicit wildcard in a class-and-reason catch pattern.

The Windows OTP 29.1.1 mnesia_sp image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,100 bytes. The reference source and disassembly establish the frame, live registers, trim, labels and call order. The implementation reuses the C# BEAM operand, tuple and line writers and the Mnesia release profile. The pinned source and image are embedded in the permanent regression. Dependencies and notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part775-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part775.json`.

The next candidate is inets_lib. The pinned image has 17 instructions in millisec_passed/1, 8 in formated_timestamp/0 and 55 in format_timestamp/1.

All four targeted checks passed, including the permanent release comparison. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 52 Equal, 209 SourceRejected and 1,105 Unsupported. mnesia_sp was the only status change from Part774. [Corpus report](otp-corpus.json).
