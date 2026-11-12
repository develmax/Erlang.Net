# Part864 Unicode documentation text

The managed writer accepts Unicode character lists and UTF-8 documentation binaries, including empty strings. It trims OTP pattern whitespace at the edges, preserves internal line endings and indentation, and extracts a leading signature when it parses as a local call to the documented name and arity.

The deterministic fixture covers supplementary Unicode, CRLF, directional whitespace, an empty document, and explicit signatures for a function and a type. Rebuild it in the fixture directory with pinned `erlc.exe +deterministic beam_docs_probe.erl`.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the resulting executable with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

All three focused checks passed. The full corpus reported 93 Equal, 5 Different, 164 SourceRejected and 1,104 Unsupported. The five new images are wxGBSizerItem, wxGraphicsBrush, wxGraphicsFont, wxGraphicsPen and wxPreviewCanvas. All have byte differences. The largest individual first failure is now the general expression fallback at 319 images. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused results](focused.json).
