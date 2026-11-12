# Part883 Length and function guards

The emitter lowers `length/1` through the garbage-collecting unary BIF. A failed length evaluation follows the guard failure target. The direct equality path emits OTP's bounded integer operand. The function predicate uses is_function for any arity, `is_function2` for a fixed supported arity and the binary BIF for dynamic arity.

The deterministic fixture checks direct length comparisons, a function without an arity test, and fixed and dynamic arity. Rebuild with `erlc +deterministic beam_length_fun_guard_probe.erl`. The direct images matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound test checks acceptance, a mismatched arity, a negative arity and an improper list. Both images returned `{{ok,1},rejected,rejected,rejected}` on pinned OTP. The compound image still differs byte for byte.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

The 1,366-file corpus reported 100 Equal, 88 Different, 164 SourceRejected and 1,014 Unsupported. wxPrintout and wxTaskBarIcon moved from Unsupported to Different. All 58 earlier length and function guard failures reached subsequent blockers or emitted images. [Corpus report](otp-corpus.json).

All three focused tests passed. The managed runtime probe uses a local function because its literal reader rejected the external fun form in the initial test. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).
