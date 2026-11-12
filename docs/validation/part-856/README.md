# Part856 TLS server handshake and full release parity

The compiler emits the OTP server handshake frame, TLS option preparation, typed certificate check and failure cleanup. The verifier result type is admitted only for the recognized certificate check shape. The fixed logger apply is lowered to macro_log/4. Nested tuple-projection helpers follow lifted closure bodies, and forward compact labels are resolved after helper placement.

The extended deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_certificate_name_probe.erl`. The handshake probe checks six paths with transport order and exact exception stacks, with logger output disabled. [Fixture hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The complete 24,028-byte inet_epmd_tls_socket image matches the Windows OTP 29.1.1 release and loads in OTP. The permanent release comparison embeds the existing pinned source, image and kernel headers. [Release hashes](release-sha256.txt), [chunk comparison](release-chunks.txt), [function comparison](release-analysis.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part856-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part856-linked-tests`.

All four focused tests passed, including the newly embedded release comparison. The full 1,366-file corpus reported 93 Equal, 195 SourceRejected and 1,078 Unsupported. Only inet_epmd_tls_socket changed status. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
