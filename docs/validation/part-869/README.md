# Part869 List concatenation

The emitter lowers general `++` to the Erlang BIF with value and tail call instructions. A syntactically known proper prefix lowers to cons construction. Direct register cases preserve OTP call lines and avoid unnecessary frames. A tuple retaining an input across the append call saves it in one Y register.

The deterministic fixture covers direct append, literal prefixes, empty right lists and tuple results. Rebuild it with pinned `erlc.exe +deterministic beam_constructor_probe.erl`. The runtime probe covers value and tail calls, a non-list right tail and rejection of a non-list left operand.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and a report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The final corpus reported 99 Equal, 16 Different, 164 SourceRejected and 1,087 Unsupported. All 58 old append failures passed that check. One of them, httpd_file, now emits a different image, and 57 encounter subsequent blockers. Catch is the most frequent specific expression failure at 37 files. Three focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
