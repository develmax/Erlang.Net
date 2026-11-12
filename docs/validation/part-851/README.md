# Part851 Socket acceptance and conditional address matches

The compiler emits the tagged acceptance dispatch, local and peer map checks, address argument moves and tail call to the handshake. Errors from the address queries close the accepted socket before exit. The unexpected result paths preserve the case_clause and else_clause exceptions.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_socket_accept_probe.erl`. The behavior probe checks seven paths with exact transport order, arguments and exception stacks. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket build now reports accept_controller/3 as its first unsupported function. [Pinned next disassembly](next-accept-controller.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part851-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part851-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
