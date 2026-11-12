# Part835 DTLS option assembly and port resolution

The compiler emits the local calls, tuple result projections and list concatenation for emulated_options/1. It proves the tuple shape through local clauses and excludes resolved throw/1 branches from normal returns. It also emits the record option calls in field order and the guarded port lookup with its nested result match.

The deterministic fixture for all three forms matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_tuple_result_append_probe.erl`.

The OTP behavior probe compares option assembly, helper call order, record defaults, guarded port lookup and exception stacks. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past emulated_options/1, emulated_socket_options/2 and get_real_port/2. The first unsupported function is start_new_listener/4. [Pinned disassembly](next-listener.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part835-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part835-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
