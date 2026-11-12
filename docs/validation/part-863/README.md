# Part863 Typed documentation

The managed documentation writer emits type entries before function entries. It keeps exported types and local types reachable from exported types and function specs. Each type entry includes its source annotation, signature, documentation value, and export flag.

The deterministic fixture covers an exported parameterized type, an exported opaque type, a reachable private type, and an unused private type. Rebuild its reference in the fixture directory with pinned `erlc.exe +deterministic beam_docs_probe.erl`.

The 1,366-image run reported 93 Equal, 164 SourceRejected, and 1,109 Unsupported. All 419 images that previously stopped at the type guard reached a subsequent check. Of these, 325 hit the documentation text check. The same text check is now the largest individual failure site at 359 images. Included type annotations, record-mediated reachability, and the remaining documentation forms still require parity work. [Full corpus](otp-corpus.json).

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the resulting executable with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

All three selected tests passed. The complete runtime suite and canonical readiness regeneration were outside this run. [Focused results](focused.json).
