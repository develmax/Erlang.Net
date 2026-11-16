# Part879 Guarded record field access

The general emitter uses the current guard failure label if the input fails the record tag or arity test. The field is loaded only after the check succeeds. The ordinary expression path continues to raise `badrecord`. The direct predicate layouts use the tagged tuple test, field extraction and exact atom test from OTP.

The deterministic fixture compares a boolean field guard and an exact atom comparison. Rebuild it with `erlc +deterministic beam_record_access_guard_probe.erl`. [OTP assembly](otp-direct-disassembly.S).

The general regression checks a valid record, a wrong tag, a wrong arity and a succeeding alternative after an invalid record input. Both the OTP and C# images returned `{accepted,rejected,rejected,accepted}` on the pinned OTP runtime. Their general guard layouts differ.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation root and report path. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The direct fixture matched byte for byte. The full 1,366-file run produced 100 Equal, 76 Different, 164 SourceRejected and 1,026 Unsupported. All 28 earlier RecordAccess failures reached subsequent blockers. The most frequent specific blockers are is_pid/1 in 36 files, tuple_size/1 in 31, unary not in 27, and multiplication in 26. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json) and [corpus report](otp-corpus.json).
