# Part850 Linked record and stack-captured callbacks

The compiler matches a tagged socket record, extracts the controller, saves the socket, address and controller in the OTP stack layout and emits the link call. The existing closure planner supplies the captures and lifted bodies. The record declaration supplies the tuple field order, callback order and default values.

The permanent deterministic fixture declares callback fields in a different order from their assignments. It matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_linked_record_closures_probe.erl`. The behavior probe checks the link, returned record and capture values, invalid input and exact guard failure stacks. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket build now reports accept_open/2 as its first unsupported function. This run establishes byte equality for the fixture. [Pinned next disassembly](next-accept-open.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part850-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part850-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
