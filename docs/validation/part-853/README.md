# Part853 Tuple-key list filter and complete SSL transport image

The compiler extracts a local list-comprehension helper. It tests each element for a two-element tuple, selects matching atom keys, preserves order and repeats, and emits bad_generator for an invalid source or improper tail. The abstract form writer now serializes trailing comprehension filters.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_tuple_key_filter_probe.erl`. The behavior probe checks skipped elements, duplicates and exact exception stacks for improper inputs. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket image builds with the recorded SSL release profile and loads in OTP. CInf, Dbgi and Docs match byte for byte. Ten chunks still differ. The disassembly comparison normalizes branch labels, line operands and closure identity fields. It finds 37 matching instruction sequences, three differing functions and one reference-only comprehension helper. [Chunk differences](release-chunks.txt), [comparison script](release-analysis.eval) and [result](release-analysis.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part853-debug-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part853-debug-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, one Different, 195 SourceRejected and 1,078 Unsupported. Only inet_epmd_tls_socket changed status. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
