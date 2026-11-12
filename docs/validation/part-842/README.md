# Part842 Listener setup with maybe

The compiler emits the listener setup with a tagged conditional match and an atom conditional match. It forwards error tuples through the else clause and raises else_clause for other unmatched values. The parser preserves the maybe location and the release debug writer supports maybe, maybe_match and else forms.

The deterministic listener fixture matches OTP 29.1.1 byte for byte. It is in the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_maybe_listener_probe.erl`. The OTP probe covers both conditional failures, unmatched else errors, backlog values and call order. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The full inet_epmd_tls_socket source now advances to listen_port/3. The full release image and its debug section remain unverified. [Pinned disassembly](next-listen-port.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part842-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part842-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
