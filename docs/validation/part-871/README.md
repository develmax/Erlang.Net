# Part871 Imported functions

The emitter admits import directives, resolves unqualified calls by name and arity through the parsed bindings, and writes external calls to the resolved module. Local definitions and dynamic remote calls retain their existing paths. The abstract debug writer reconstructs each import declaration from its source tokens so it preserves the declaration, signature order and annotation.

The deterministic fixture covers imported calls with one and two arguments, plus an imported call whose caller retains an argument. Rebuild it with pinned `erlc.exe +deterministic beam_import_probe.erl`. The separate debug reference uses `erlc.exe +debug_info` and verifies the two import forms independently of file and build paths.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The deterministic fixture matched byte for byte, both import declarations matched the abstract reference, and all three focused tests passed. The full corpus reported 99 Equal, 22 Different, 164 SourceRejected and 1,081 Unsupported. Of the 102 old import failures, beam_z, cerl_clauses, xmerl_sgml and xmerl_xml emitted different images and 98 reached subsequent unsupported checks. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
