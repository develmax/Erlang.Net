# Part855 Certificate name verification

The compiler emits the OTP frame and register layout for the allowed-node and peer certificate checks. It calls the extracted host projection, builds the IP-prefixed reference list, dispatches the verification result and lowers the fixed logger apply to macro_log/4. The success, pass-through and failure branches preserve the source lines.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_certificate_name_probe.erl`. The behavior probe checks eight paths with transport order, arguments and exact exception stacks, with logger output disabled. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket image loads in OTP. After normalizing branch labels, line operands and closure identity fields, 40 of 41 function bodies match. The remaining body is accept_handshake/4. Ten chunks still differ. [Chunk differences](release-chunks.txt), [comparison script](release-analysis.eval), [result](release-analysis.txt) and [remaining disassembly](remaining-disassembly.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part855-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part855-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, one Different, 195 SourceRejected and 1,078 Unsupported, with no status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
