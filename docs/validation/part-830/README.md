# Part830 DTLS socket connection setup

The C# compiler checks the input record, selects its transport callback, concatenates socket options and emits the dynamic open call. It preserves the callback, timeout, record, port and host across the calls, builds the nested address and option tuples for the success branch and returns a tagged transport error unchanged.

The complete deterministic fixture matches the OTP 29.1.1 compiler byte for byte and is included in the permanent regression. It can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_record_transport_probe.erl`.

The OTP behavior probe uses a stub transport and FSM and compares all eight success arguments, the forwarded error and stacks for malformed configuration, invalid callback and unexpected replies. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past connect/4 and first stops at close_listen/2. [Pinned close listener disassembly](next-close-listen.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part830-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part830-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
