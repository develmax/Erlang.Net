# Part825 SSL session cache upgrade supervisor

The managed compiler emits the worker count dispatch in start_child/1. It validates and reuses an existing child, or constructs arguments for a new child. It preserves a successful response, normalizes already_started to a successful response containing the existing process and emits the match and case failures with OTP source locations.

The complete 12,168-byte image matches the Windows OTP 29.1.1 release. The behavior probe used the real OTP supervisor and stub cache and configuration modules. It compared new child creation, reuse, an already_started response and the error stacks for unexpected worker count and child start results. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part825-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part825-tests`. The first test build hit a shared intermediate DLL lock during the parallel build. The sequential retry succeeded.

The 1,366-file corpus reported 88 Equal, 195 SourceRejected and 1,083 Unsupported. Only ssl_upgrade_server_session_cache_sup changed status. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next target is ssl_dist_sup. Its launch path matches the boot argument, reads distribution options and initializes ETS. [Pinned disassembly](next-distribution-supervisor.txt).
