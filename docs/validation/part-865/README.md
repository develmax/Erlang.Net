# Part865 Expression diagnostics and wx release images

The corpus reports the unsupported expression kind or operator, identifies unsupported guard calls, and lists differing BEAM chunks by payload. The expression fallback split included 53 dynamic remote calls, 51 record constructions, 46 fun expressions, 44 appends, 29 catch expressions and 23 numeric equality operators.

The shared wx profile records the compiler version, build options and source and include paths from the pinned release. The debug writer keeps function documentation in Docs and omits the doc attribute from the abstract forms. Literal atom dispatch emits a tail call for a constant error and groups identical atom results with `select_val`.

Rebuild the deterministic dispatch fixture in its directory with pinned `erlc.exe +deterministic beam_dispatch_probe.erl`. Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the built tool with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The final corpus reports 98 Equal, 2 Different, 164 SourceRejected and 1,102 Unsupported. The five new equal images are wxGBSizerItem, wxGraphicsBrush, wxGraphicsFont, wxGraphicsPen and wxPreviewCanvas. The two newly emitted images, wxDataObject and wxSystemOptions, still differ. All three focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused results](focused.json).
