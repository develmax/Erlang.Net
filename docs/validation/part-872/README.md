# Part872 Documentation metadata maps

The writer merges module and entry metadata with their separate text declarations. It normalizes selected metadata values to UTF-8, inherits module since values, preserves explicit entry overrides, and supports equivalence references and simple call expressions. It keeps source annotations and export flags in the generated metadata. Private documentation is consumed without creating public entries.

The deterministic fixture covers authors, format, module and entry since, group, both equiv forms and arbitrary metadata. Regenerate it with pinned `erlc.exe +deterministic beam_docs_probe.erl`.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The deterministic fixture and a separate metadata-only example matched byte for byte. All three focused tests passed. The full corpus reported 99 Equal, 33 Different, 164 SourceRejected and 1,070 Unsupported. Eleven more modules emitted different images. Ten came from the previous documentation text-type group, and the other 165 modules in that group remain unsupported. The most frequent remaining specific guard failure is `erlang:is_record/3` at 141 files. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
