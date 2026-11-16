# Part887 Try after

The emitter installs a cleanup handler around the body and any of or catch clauses. The normal path preserves the value across cleanup. The exceptional path preserves the raw trace and reason, executes cleanup, then reraises the original exception. Cleanup runs after removing its handler, so its own exception takes precedence.

The direct fixture covers zero-argument remote calls in the protected body and cleanup. Rebuild with `erlc +deterministic beam_try_after_one_line.erl`. The complete fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound regression verifies the normal result, rethrow, a cleanup exception and cleanup after a caught error. Both images returned `{{value,ok},{original,ok},{cleanup_failed,fail},{handled,handled}}` on pinned OTP. The general register layout still differs.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The 1,366-file corpus reported 100 Equal, 99 Different, 164 SourceRejected and 1,003 Unsupported. All 11 previous Try failures reached subsequent blockers. [Corpus report](otp-corpus.json).
