# Part886 Division and list operators

The emitter lowers integer division, remainder and left shift through the garbage-collecting arithmetic BIF. Division in a guard has a failure label. Outside a guard, floating division converts operands to float registers, divides and boxes the result. List subtraction uses the Erlang external call. The existing exact clock delta lowering is preserved.

The direct fixture checks six functions, including typed integer division in a guard. Rebuild with `erlc +deterministic beam_div_list_ops_probe.erl`. The complete fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound regression checks integer and floating division, list subtraction, left shift, remainder and a guard rejection on division by zero. Both images returned `{{3,3.5,[2,1],28,1},rejected}` on pinned OTP. The compound bytecode still differs.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed with the final compiler assembly. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The 1,366-file corpus reported 100 Equal, 99 Different, 164 SourceRejected and 1,003 Unsupported. ct_repeat, http_response and mnesia_frag_hash now emit with differences. [Corpus report](otp-corpus.json).
