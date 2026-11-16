# Part800 Observer detail window initialization

The C# compiler emits the window setup, scaled dimensions, optional status bar and final state record. It proves the page callback's return arity before extracting the result fields. It also emits the start-link argument list with the resolved self BIF. The pinned OTP 29.1.1 disassembly guided stack liveness and register scheduling. No new dependencies were required.

The 1,768-byte standalone fixture isolates the upstream window initialization, keeps the Apache-2.0 notice and supplies local callback stubs. It was compiled with `compile:file(Source, [binary,deterministic])` and matches C# output byte for byte. The complete `cdv_detail_wx` image remains unsupported at `init_tab/4`. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part800-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part800.json`. Across 1,366 files it reported 68 Equal, 195 SourceRejected and 1,103 Unsupported. The tool returned 1 because unmatched cases remain. The `cdv_detail_wx` diagnostic advanced from `init/6` to `init_tab/4`. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
