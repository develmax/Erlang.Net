# Part846 Guarded callback calls and sends

The managed emitter handles static remote tail calls and message sends with constant payloads after the closure guard. It collects variables referenced by the effect, reuses the argument move scheduler and can leave an already equal argument in its destination register. The send path emits the frame and source location used by OTP.

The permanent deterministic fixture covers a call with a captured socket, a call that can reuse the checked controller argument and a send to the captured controller. It matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_call_probe.erl`. The behavior probe compares successful results, argument values, mailbox effects and the complete guard failure stacks. It also verifies that guard failure triggers no effect. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The full inet_epmd_tls_socket source still reaches start_dist_ctrl/2 as the first unsupported function. Nested calls and cases in its callbacks, plus record construction and stack captures in the parent, remain open. [Pinned callback disassembly](../part-845/start-dist-ctrl-callbacks.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part846-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part846-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
