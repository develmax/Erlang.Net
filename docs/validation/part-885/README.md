# Part885 Head, number and map guards

The emitter uses the unary head BIF with a guard failure target. Number and map predicates use the BEAM type tests. The direct paths preserve the incoming register layouts and exact guard return branches.

The deterministic fixture checks direct number and map predicates, a boolean head predicate and an exact comparison against the head. Rebuild with `erlc +deterministic beam_number_map_head_guard_probe.erl`. The complete fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound test accepts a numeric head of an improper list. It rejects a non-map, an empty list and a nonnumeric head. Both images returned `{accepted,rejected,rejected,rejected}` on pinned OTP. Its instruction layout still differs.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The 1,366-file corpus reported 100 Equal, 96 Different, 164 SourceRejected and 1,006 Unsupported. Seven wx modules moved from Unsupported to Different. All 42 earlier guard failures reached subsequent blockers or emitted an image. [Corpus report](otp-corpus.json).
