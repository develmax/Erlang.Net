# Part889 General body fallback

For a single unconditional clause otherwise assigned to specialized lowering, the planner first tries the existing path in an isolated compiler. Successful specialized emission remains selected. A rejection permits the general emitter to attempt the same function. If general lowering also rejects, the original path reports its diagnostic.

The direct unary BIF path handles type tests, head, node, tuple size, byte size, length and absolute value with the OTP unary or garbage-collecting unary instruction. Rebuild the direct fixture with `erlc +deterministic beam_body_fallback_probe.erl`. Its six functions matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The general regression checks a call with a computed argument and a tuple with a computed element. Both images returned `{<<"42">>,{41,42}}` on OTP. Their bytecode still differs.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

The full 1,366-file corpus reported 100 Equal, 485 Different, 164 SourceRejected and 617 Unsupported. A total of 385 files moved from Unsupported to Different. All 485 Different files differ in Code. Three focused tests passed. Full runtime and canonical readiness checks remain pending because the readiness CLI rejects partial test reports. [Focused report](focused.json) and [corpus report](otp-corpus.json).
