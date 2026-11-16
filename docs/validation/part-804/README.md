# Part804 Atom checked progress tail calls

The C# compiler emits a direct atom comparison on the incoming register, then reuses the ordinary tail-call argument writer. The unconditional atom fallback returns directly. This eliminates the general emitter's stack frame for the matched two-clause shape.

The 796-byte fixture isolates display_progress/2 and destroy_progress/1 and preserves the upstream Apache-2.0 notice. The deterministic OTP 29.1.1 image and C# output match byte for byte. [Oracle script](oracle.eval) and [comparison](oracle.txt).

The complete cdv_detail_wx image is now 4,812 bytes, compared with 19,908 bytes in the release. The normalized disassembly matches all 12 instructions of display_progress/2 and all 10 of destroy_progress/1. A separate unnormalized comparison found only the missing source file name in the line instructions of those functions. Line numbers and labels agree. The release still differs in other code and metadata, including the absent separate list comprehension helper for create_pages/3. [Disassembly summary](disassembly-summary.txt) and [comparison script](compare.eval).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part804-tool` and run as `dotnet artifacts/beam-backend/part804-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part804.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part804-tests` and run with the exact cases listed in the focused report.

All four focused tests passed. Full runtime suite execution and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full 1,366-file corpus reported 68 Equal, 1 Different, 195 SourceRejected and 1,102 Unsupported. No status changed from Part803. ELDAPv3 remains Equal. The tool returned 1 because unmatched cases remain. [Corpus report](otp-corpus.json).
