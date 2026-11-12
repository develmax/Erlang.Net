# Part858 Plain socket connection

The compiler matches the network address and peer tuple, retains the original socket and options across calls, builds the peer map in OTP key order and tail-calls the distribution controller. The three conditional failures use the shared maybe else path.

The deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_plain_socket_connect_probe.erl`. Seven behavior cases check the peer map, transport order, short circuits and exact failure stacks. [Fixture hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_socket build now reports output_handler_start/2 as its first unsupported function. [Pinned next disassembly](next-output-handler.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part858-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part858-tests`.

All four focused tests passed. The full 1,366-file corpus reported 93 Equal, 195 SourceRejected and 1,078 Unsupported, with no status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
