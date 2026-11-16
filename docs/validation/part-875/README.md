# Part875 Subtraction and unary minus

The emitter lowers binary subtraction and unary minus to the managed BEAM garbage-collecting BIF path. Direct returns use the required live X register count. The selected comparison guard uses the inferred numeric result type and guard failure label. General expressions reuse Y slot operands and the frame planner.

The deterministic fixture covers register operands, integer constants on both sides, unary minus and a typed guard. Regenerate it with pinned `erlc.exe +deterministic beam_subtract_probe.erl`. The runtime probe checks mixed numeric types and the guard fallback for a nonnumeric operand.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The deterministic fixture matched byte for byte and all three focused tests passed. The full corpus reported 100 Equal, 72 Different, 164 SourceRejected and 1,030 Unsupported. The 44 prior subtraction failures passed that operation, with two emitting different images and 42 reaching later blockers. Overall, three more modules emitted images with differences. Function references are now the most frequent specific expression blocker at 34 files. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
