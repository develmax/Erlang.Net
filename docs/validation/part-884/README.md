# Part884 Bit construction

The emitter evaluates each segment value and size in source order and emits `bs_create_bin`. It handles integer, binary and bitstring, float and UTF segments, computes a construction unit, uses the string table for aligned constant segments and emits append for the initial whole binary. Direct forms reuse incoming registers.

The deterministic fixture checks six direct forms, including dynamic width, mixed segments, UTF, floating point and a string suffix. Rebuild with `erlc +deterministic beam_bit_emit_probe.erl`. The complete fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The nested fixture appends a dynamic-width integer and a string to a non-byte-aligned prefix. Both images returned `{ok,<<163,120>>}` on pinned OTP. General register allocation and merging adjacent constant segments still differ. Explicit string segments with dynamic widths remain outside this emitter.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The full 1,366-file corpus reported 100 Equal, 89 Different, 164 SourceRejected and 1,013 Unsupported. erl_posix_msg moved from Unsupported to Different. All 17 earlier Bits failures reached subsequent blockers or emitted an image. [Corpus report](otp-corpus.json).
