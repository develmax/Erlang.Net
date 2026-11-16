# Part834 DTLS address and port dispatch

The compiler emits the missing-socket reply and nested UDP address selection in peername/2. It also emits the PID guard and tuple fallbacks in sockname/2 and the UDP tuple projection in port/2. All three paths reuse the unary dynamic tail call writer.

The deterministic fixture for all three forms matches OTP 29.1.1 byte for byte. It retains the upstream Apache-2.0 notice, uses a stub in place of the static inet calls, and is part of the permanent regression. Rebuild it with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_socket_address_probe.erl`.

The OTP behavior probe checks the missing-socket result, UDP and dynamic routes, fallback when the listener is not a PID and the missing transport stack. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past peername/2, sockname/2 and port/2. The first unsupported function is emulated_options/1. [Pinned disassembly](next-emulated-options.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part834-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part834-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
