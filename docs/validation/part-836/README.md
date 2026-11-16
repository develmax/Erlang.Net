# Part836 DTLS listener opening

The compiler emits transport tuple validation, option concatenation and the dynamic open call. It resolves the assigned port on a zero-port request, preserves a fixed port and forwards the opened socket to the local startup helper. It normalizes the address-in-use reply and preserves other replies and bad matches.

The tagged record test is preserved for exported functions and for private functions with unproven inputs. It can be omitted when each use of the private function is a direct call supplying a variable bound by the matching tagged tuple pattern. The full dtls_socket source now advances to start_dtls_packet_demux/5. [Pinned disassembly](next-demux.txt).

The exported deterministic fixture matches OTP 29.1.1 byte for byte and is included in the permanent regression. Rebuild it with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_listener_start_probe.erl`.

The OTP behavior probe checks both port paths, address-in-use normalization, forwarded replies and exception stacks for malformed records, callbacks and port results. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part836-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part836-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
