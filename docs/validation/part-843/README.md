# Part843 Listener bind and address extraction

The compiler emits the address map, bind and listen calls, tagged socket address match and map field extraction in listen_port/3. The conditional matches share the existing maybe else path.

The deterministic fixture matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_maybe_listener_bind_probe.erl`. The pinned OTP probe checks successful bind, call order, short circuit failures and else_clause for missing fields or malformed addresses. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full inet_epmd_tls_socket source now advances to connect/3. Full release image byte equality remains open. [Pinned disassembly](next-connect.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part843-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part843-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
