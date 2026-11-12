# Part840 DTLS active socket check

The compiler emits the active increment range checks, typed addition and passive notifications. It preserves the integer and atom branches and the source locations for invalid increments and malformed transport replies. The tagged argument proof now recognizes a record constructed before a direct local call as well as an argument bound in a tagged pattern.

The deterministic active socket fixture matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_active_socket_probe.erl`. The OTP behavior probe covers the range boundaries, passive messages, overflow and malformed replies. [Probe](behavior.eval), [results](behavior.txt) and [hashes](oracle.txt).

The generated dtls_socket image loads in pinned OTP and measures 23,632 bytes. After label normalization 29 of 30 functions match. Only listen/2 still differs. AtU8, Code, ImpT, ExpT, LocT, Attr, Line and Type remain different. [Function comparison](module-comparison.txt), [sections](chunks.txt) and [listener instructions](next-listen.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part840-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part840-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 1 Different, 195 SourceRejected and 1,079 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
