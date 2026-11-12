# Part833 DTLS option reads and socket statistics

The compiler emits listener record matching, option splitting, nested and simple UDP tuple projections and the dynamic fallback for getopts/3. It also emits the PID guard and UDP tuple cases in getstat/3. The setopts/3 emitter shares the listener record tests and dynamic tail call writer.

The deterministic fixture for both forms matches OTP 29.1.1 byte for byte. It is part of the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_socket_option_read_probe.erl`.

The pinned OTP behavior probe checks listener, PID, nested tuple, plain socket and dynamic branches. It also checks fallback when the nested address shape fails and compares the missing transport error stack. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full dtls_socket release source progresses past getopts/3 and getstat/3. The first unsupported function is peername/2. [Pinned disassembly](next-peername.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part833-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part833-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
