# Part854 Tuple projection helper and tagged accumulator

The compiler extracts the closed tuple projection into a recursive comprehension helper. The helper constructs a tagged tuple for each item and reports the invalid remainder for an improper generator. The tagged accumulator dispatches by the tuple key and updates one argument register before its tail call.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_tuple_projection_probe.erl`. The behavior probe checks order, duplicates, counter replacement and exact failure stacks. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket image loads in OTP and has all 41 reference functions. The projection helper and split_stat/4 now match after normalizing label numbers, line operands and closure identities. Only accept_handshake/4 and check_allowed_nodes/2 differ in that comparison. Ten chunks still differ. [Chunk differences](release-chunks.txt), [comparison script](release-analysis.eval) and [result](release-analysis.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part854-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part854-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, one Different, 195 SourceRejected and 1,078 Unsupported, with no status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json), [focused report](focused.json) and [remaining function disassembly](remaining-disassembly.txt).
