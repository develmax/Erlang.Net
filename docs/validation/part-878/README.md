# Part878 Function-value calls

The C# emitter evaluates the function expression before the arguments, retains the function value across argument evaluation, then emits `call_fun` with the function in `X(arity)`. The direct parameter and literal forms reuse the parallel move scheduler and allocate a zero-slot frame, matching the reference fixture. Named recursive funs retain their existing lowering.

The deterministic fixture compares five direct forms, including zero arguments, a register swap, an argument already in place, a literal and a repeated argument. Rebuild it with `erlc +deterministic beam_apply_value_probe.erl`. [OTP assembly](otp-direct-disassembly.S).

The computed-target test invokes a closure produced by one local call with the result of a second local call. The reference and the managed image both returned `{ok,{17,{argument,17}}}` on OTP 29.1.1. The computed-target image differs byte for byte.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`, then pass the pinned installation and report paths to its DLL. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The full 1,366-file run produced 100 Equal, 76 Different, 164 SourceRejected and 1,026 Unsupported. ct_rpc and edoc_refs moved from Unsupported to Different. [Corpus report](otp-corpus.json).
