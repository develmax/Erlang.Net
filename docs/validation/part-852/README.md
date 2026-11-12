# Part852 Socket controller transfer

The compiler saves the socket while calling the controller transfer operation. The success path returns the saved socket. An error tuple becomes an exit with its reason, and other results raise else_clause. The emitted frame, tuple allocation and failure lines follow OTP.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_socket_controller_probe.erl`. The behavior probe compares call arguments, return values and exact exception stacks for success, transfer errors and malformed replies. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket build now reports setopts_filter/1 as its first unsupported function. [Pinned next disassembly](next-setopts-filter.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part852-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part852-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
