# Part847 Guarded option calls and binary replies

The managed compiler keeps the receiver in a stack slot while evaluating a remote option producer. It builds the one-element list and tail-calls the options function. The guarded reply path checks the result tag and binary payload, converts the payload with binary_to_list/1 and preserves the original term on the fallback path.

The permanent deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_reply_probe.erl`. The behavior probe compares the converted reply, pass-through values, call arguments and order, exact guard failure stacks and absence of effects after a failed guard. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket source still reaches start_dist_ctrl/2 as the first unsupported function. The statistics and option-filter cases, parent record match, stack captures and hs_data construction remain open. [Pinned callback disassembly](../part-845/start-dist-ctrl-callbacks.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part847-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part847-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
