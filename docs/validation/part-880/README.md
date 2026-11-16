# Part880 PID and tuple size guards

The general emitter uses the PID test for a predicate and the unary `tuple_size` BIF for an operand. The BIF branches to guard failure for non-tuples. An exact comparison with an in-range integer arity emits a tuple test and `test_arity`, matching the direct OTP fixture.

Rebuild the deterministic fixture with `erlc +deterministic beam_guard_pid_size_probe.erl`. The original and C# images matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound guard checks an actual PID and tuple, a non-PID, a non-tuple and a tuple that is too small. Both images produced `{accepted,rejected,rejected,rejected}` on pinned OTP. The compound image still differs in register allocation and typed comparison operands.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation root and report path. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

Three focused tests passed. The 1,366-file run produced 100 Equal, 76 Different, 164 SourceRejected and 1,026 Unsupported. All 36 earlier is_pid and 31 earlier tuple_size failures reached subsequent blockers. The most frequent specific blockers are element/2 in 36 files, unary not in 31 and multiplication in 26. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json) and [corpus report](otp-corpus.json).
