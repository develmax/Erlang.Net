# Part829 DTLS socket dispatch and receive

The compiler emits the nested tuple tests, field projections and dynamic tail call used by send/3. The swap operand order follows the pinned OTP compiler. It also emits a selective receive for a tuple whose first field equals the sender captured from the accept call and whose second field matches the message tag.

The complete deterministic fixture, including both function shapes, matches the OTP 29.1.1 compiler byte for byte. The fixture is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_nested_dynamic_tail_probe.erl`.

The pinned OTP behavior probe compares argument routing, malformed tuple and missing transport errors, successful receive, preservation of unmatched messages and failure replies. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past send/3 and accept/3. The first remaining unsupported function is connect/4. [Pinned accept disassembly](next-accept.txt) and [pinned connect disassembly](connect.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part829-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part829-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
