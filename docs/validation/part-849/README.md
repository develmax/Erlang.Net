# Part849 Guarded option-filter callback

The compiler saves the socket and original options across the local filter call. An empty result tail-calls the remote options function with the original options. Other results produce the nested error tuple. The nil test includes the OTP list type operand when the local filter body proves a proper list result.

The permanent fixture also exercises the frame-free emission of a helper that returns the empty input as-is and wraps other values in a singleton list. The deterministic fixture matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_filter_probe.erl`. The behavior probe checks option forwarding, rejection, full guard failure stacks and absence of unexpected transport calls. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The first unsupported function in the full inet_epmd_tls_socket source is start_dist_ctrl/2. The parent record match, stack captures, callback order and hs_data construction remain open. [Pinned parent disassembly](../part-844/next-start-dist-ctrl.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part849-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part849-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
