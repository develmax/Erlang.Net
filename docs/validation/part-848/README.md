# Part848 Guarded statistics reply

The compiler emits the statistics call, matches a two-element tagged tuple, extracts the statistics into the first argument register and tail-calls the private helper with the initial accumulator values. Other replies pass through. The local reachability walk retains helpers referenced by this guarded callback shape.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_stat_probe.erl`. The behavior probe checks call arguments, successful forwarding, malformed and error replies, and exact guard failure stacks without transport effects. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The first unsupported function in the full inet_epmd_tls_socket source is start_dist_ctrl/2. The option-filter case, parent record match, stack captures and hs_data construction remain open. [Pinned callback disassembly](../part-845/start-dist-ctrl-callbacks.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part848-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part848-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
