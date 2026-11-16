# Part841 DTLS socket release image parity

The C# compiler emits listener lookup and reuse, including the tagged reply test, option update, registration and socket construction. It preserves the local tail call for an absent listener and forwards other replies.

The complete 23,548-byte dtls_socket image matches Windows OTP 29.1.1 byte for byte and is part of the permanent release regression. The pinned OTP probe compares the reuse and new-listener routes, argument and event order, address-in-use handling and the malformed record error stack. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part841-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part841-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported. Only dtls_socket changed status, from Different to Equal. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next SSL target is the maybe match and listener setup in inet_epmd_tls_socket:listen_open/2. [Pinned disassembly](next-listen-open.txt).
