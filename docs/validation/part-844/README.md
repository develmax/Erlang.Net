# Part844 Socket and TLS connection setup

The C# compiler emits socket creation and options, bind and peer maps, TLS option assembly and the tail call to the controller from the source maybe expression. Each failed conditional match routes its original value to the shared else path.

The deterministic fixture matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_maybe_socket_connect_probe.erl`. The behavior probe verifies call arguments and order on success, an error return from each of the five conditional calls, and the exact else_clause exception and stack for unexpected replies. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket source advances to start_dist_ctrl/2. Its reference BEAM allocates eleven closures with captures and constructs the hs_data record. [Pinned disassembly](next-start-dist-ctrl.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part844-final-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part844-final-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported. No module status changed. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The broader compiler block reproduced the dynamic apply oracle failure. The Part843 test binary reproduces the same failure on its own. [Baseline report](baseline-dynamic-apply.json).

The full test suite was still running at the checkpoint. Canonical readiness requires a completed passing full report and was not regenerated.
