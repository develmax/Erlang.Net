# Part888 Node, port, float and byte size guards

The emitter uses the port and float type tests, the unary node BIF with a failure target, and the zero-argument node and self BIFs. Byte size reuses the garbage-collecting size path with a failure label. Direct comparisons use the typed node and size operands from OTP.

The direct fixture covers five functions with node/0, node/1, is_port/1, is_float/1 and byte_size/1. Rebuild with `erlc +deterministic beam_node_size_guards.erl`. The complete fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound guard verifies a local PID, a non-node-bearing atom, a non-bitstring and a non-float. Both images returned `{accepted,rejected,rejected,rejected}` on pinned OTP. The compound layout still differs.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The 1,366-file corpus reported 100 Equal, 100 Different, 164 SourceRejected and 1,002 Unsupported. ram_file moved from Unsupported to Different. The largest remaining emission sites are EmitBody with 210 and EmitCall with 179. These are diagnostic groups with multiple causes. [Corpus report](otp-corpus.json).
