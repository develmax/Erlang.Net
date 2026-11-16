# Part860 Output loop dispatch

The compiler emits the three guarded tail calls. It retains the tagged record check for inputs without proven shape. It omits that check and uses typed operands only when the local call graph proves the record and numeric arguments. The exported fixture exercises the untyped path.

The deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_output_dispatch_probe.erl`. Six behavior cases cover each continuation, invalid records and Erlang term ordering. [Fixture hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_socket build now reports output_handler_wait/6 as its first unsupported function. [Pinned next disassembly](next-output-wait.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part860-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part860-tests`.

All four focused tests passed. The full 1,366-file corpus reported 93 Equal, 195 SourceRejected and 1,078 Unsupported, with no status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
