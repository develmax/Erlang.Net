# Part832 DTLS socket records and option dispatch

The managed compiler emits the singleton PID list match, nested UDP socket check and exact table guard for socket/5. It builds the record from the source field layout. It also emits the setopts/3 paths for a listener record, a plain UDP socket and a dynamically selected transport, including the error when the option split returns a malformed value.

The full deterministic fixture for both forms matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_socket_probe.erl`.

The OTP behavior probe compares record construction, all three option routes, the active check arguments and the error stacks for an invalid option split, table and process list. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past socket/5 and setopts/3. The first unsupported function is getopts/3. [Pinned disassembly](next-getopts.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part832-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part832-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
