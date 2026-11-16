# Part882 Map expressions

The emitter evaluates the base, keys and values before constructing the result. It uses association and exact update instructions, orders literal keys by exact term order and emits dynamic keys individually. A missing exact key or non-map base branches to guard failure when used in a guard. Body expressions preserve badkey and badmap errors.

The direct fixture covers map creation, association update, exact update and an empty update. Rebuild with `erlc +deterministic beam_map_emit_probe.erl`. The direct fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The mixed fixture covers repeated dynamic keys, exact updates and guard fallback for missing keys or non-map input. Both images returned `{{ok,#{flag=>42,key=>42}},accepted,rejected,rejected}` on pinned OTP. Its instruction and register layout still differs.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation root and report path. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases. All three passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The 1,366-file corpus recorded 100 Equal, 86 Different, 164 SourceRejected and 1,016 Unsupported. beam_listing, erl_distribution, ssh_channel_sup and edlin_key moved from Unsupported to Different. All 26 prior Map expression blockers reached subsequent limitations or emitted images. The generated mixed image also returned the expected badmap and badkey exceptions in ordinary expression context. [Corpus report](otp-corpus.json).
