# Part881 Guard element, boolean negation and multiplication

The emitter uses the binary element BIF with a guard failure target, emits the unary boolean not BIF in value context and tests for the atom false in guard predicate context. Multiplication uses the existing garbage-collecting arithmetic BIF path. Direct forms reuse the incoming argument registers.

The direct fixture checks negation in body and guard contexts, multiplication and an element guard with a dynamic index. Rebuild it with `erlc +deterministic beam_not_product_probe.erl`. The deterministic images matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound fixture checks the selected false field, a true field, a non-boolean field and an out-of-range index. Both images returned `{{ok,42},rejected,rejected,rejected}` on pinned OTP. The compound image still differs byte for byte.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation root and report path. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The full 1,366-file run produced 100 Equal, 82 Different, 164 SourceRejected and 1,020 Unsupported. local_udp, megaco_timer, wxAuiDockArt, wxAuiTabArt, wxCalendarDateAttr and wxGridCellAttr moved from Unsupported to Different. All 93 earlier failures for the three operations reached subsequent blockers or emitted images. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json) and [corpus report](otp-corpus.json).
