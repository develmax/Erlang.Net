# Part859 Protected output setup

The compiler emits the try frame, socket option match, shifted watermark and output-handler record. It emits the non-returning call layout after proving that all local continuation paths recurse or raise. Errors are reported with the original stack and raised again. Other exception classes use the raw propagation path.

The deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_output_setup_probe.erl`. Four behavior cases cover the watermark, badmatch reporting, throw and exit propagation, and exact stacks. [Fixture hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_socket build now reports output_handler/5 as its first unsupported function. [Pinned next disassembly](next-output-handler.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part859-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part859-tests`.

All four focused tests passed. The full 1,366-file corpus reported 93 Equal, 195 SourceRejected and 1,078 Unsupported, with no status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
