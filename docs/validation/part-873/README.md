# Part873 Traditional record guards

The emitter lowers record tests with a known atom tag and positive tuple arity to `is_tagged_tuple`. A guard jumps directly on failure. A value expression returns a boolean. Direct predicate functions use the OTP branch layout, including the tested record-or-integer case.

The debug writer retains the original two- or three-argument source call. The deterministic fixture checks guard, value and disjunction forms. Rebuild it with pinned `erlc.exe +deterministic beam_record_guard_probe.erl`. The separate debug reference uses `erlc.exe +debug_info`. The runtime probe checks matching tags, wrong arity and an integer alternative.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The deterministic image and abstract function forms matched their respective OTP references. All three focused tests passed. The full corpus reported 99 Equal, 67 Different, 164 SourceRejected and 1,036 Unsupported. All 141 prior record guard failures moved past that check, with 34 newly emitted images and 107 subsequent unsupported checks. Numeric equality is now the most frequent specific expression failure at 56 files. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
