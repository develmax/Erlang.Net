# Part831 DTLS socket close paths

The compiler emits the nested socket and configuration checks, listener registration, process monitor and timed receive in the close_listen/2 shape. The receive loop compares both the notification tag and the saved process, removes only the matching message and returns the timeout value on expiry. It also emits the close/2 shape with an atom-selected shortcut and a dynamic tail call for the remaining transports.

The deterministic fixture for both forms matches the pinned OTP 29.1.1 compiler byte for byte. It is part of the permanent regression and can be rebuilt using `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_timed_monitor_probe.erl`. The fixture's default address comes from a stub remote call so local constant propagation is outside this test.

The pinned OTP behavior probe uses actual process monitors and checks a matching DOWN, the timeout path, retention of unrelated messages, registration arguments, forwarded replies and the two close/2 branches. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past close_listen/2 and close/2. The first unsupported function is now socket/5. [Pinned disassembly](next-socket.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part831-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part831-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
