# Part839 DTLS emulated option partitioning

The compiler emits the shared list and tuple dispatch in emulated_options/3. It preserves validation and replacement of emulated values, accumulates native options and emits one common error branch for the three rejected option keys.

The updated deterministic fixture matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_list_lookup_probe.erl`. The OTP probe compares native option ordering, replacement of duplicate emulated options, validation failures and all three rejected option keys. [Probe](behavior.eval), [results](behavior.txt) and [hashes](oracle.txt).

The full dtls_socket image loads in pinned OTP and now measures 23,784 bytes. After label normalization 28 of 30 functions match. The remaining instruction differences are listen/2 and check_active_n/2. AtU8, Code, ImpT, ExpT, LocT, Attr, Line and Type still differ. [Function comparison](module-comparison.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part839-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part839-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 1 Different, 195 SourceRejected and 1,079 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
