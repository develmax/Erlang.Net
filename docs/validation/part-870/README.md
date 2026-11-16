# Part870 Catch expressions

The emitter installs a BEAM catch handler in a Y register and joins the normal and exceptional paths at `catch_end`. Direct call forms use the OTP frame size and argument moves. Constant operands fold without installing a handler. General expressions use the existing frame planner. The debug writer preserves the catch form.

The deterministic fixture covers a remote call, local call, constant and tuple result. Rebuild it with pinned `erlc.exe +deterministic beam_catch_probe.erl`. The dynamic runtime probe checks normal return, throw, exit and error including a stack trace.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the built tool with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The deterministic fixture matched byte for byte and all three focused tests passed. The full corpus reported 99 Equal, 18 Different, 164 SourceRejected and 1,085 Unsupported. Of the 37 prior catch failures, application_starter and snmp_verbosity emitted different images and 35 reached other unsupported checks. The remaining import directive check affects 102 files. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
