# Part857 Plain socket listener and controller transfer

The compiler reuses the listener frame and error path for an options list that prepends a computed default after two deletions. The controller transfer accepts a constant second argument and moves the controller to the third argument register.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_listener_delete_probe.erl`. The probe checks backlog defaults, option values, call order, transfer arguments and exact failure stacks. [Fixture hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_socket build now reports connect/3 as its first unsupported function. [Pinned next disassembly](next-connect.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part857-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part857-tests`.

All four focused tests passed. The full 1,366-file corpus reported 93 Equal, 195 SourceRejected and 1,078 Unsupported, with no status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
