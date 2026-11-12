# Part845 Guarded closures with captured values

The C# compiler includes a parent argument in the closure environment when only the guard reads it. It emits exact equality and the local function_clause fallback with the closure's original call arity. The failure functions appear after the lifted bodies in Erlang atom order, as in OTP's create_fc_stubs/2. Compact forward label operands are resolved after those functions receive their labels.

The permanent fixture covers constant, variable and tuple returns as well as a guard on the second argument. It matches OTP 29.1.1 byte for byte. Rebuild the oracle with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_capture_probe.erl`. The OTP behavior probe compares successful results and full exception stacks for integer-versus-float mismatches. [Image hash](oracle.txt), [probe](behavior.eval) and [result](behavior.txt).

The pinned SSL callbacks show the remaining support needed for start_dist_ctrl/2. Several closures need tail calls, sends or case branches. The parent needs record matching, captures from stack slots and handshake record construction. [Pinned callback disassembly](start-dist-ctrl-callbacks.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part845-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part845-tests`.

All four focused tests passed. The full 1,366-file corpus reported 92 Equal, 195 SourceRejected and 1,079 Unsupported, with no module status changes. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
