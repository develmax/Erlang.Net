# Part861 Corpus source admission

The Windows OTP 29.1.1 corpus tool supplies include directories for relocated sources and examples. The compiler now reads the expanded head when a body or guard references `?FUNCTION_NAME` or `?FUNCTION_ARITY`. The regression fixture expands both a function name and a parameter list.

The include-only run covered all 1,366 release images. The include diagnostic count fell from 71 to 50, and 17 modules moved from SourceRejected to Unsupported. The include-only run reported 93 Equal, 178 SourceRejected and 1,095 Unsupported.

With both changes, 93 images are Equal, 172 are SourceRejected and 1,101 are Unsupported. The old function-context diagnostic disappeared from all 18 affected modules. Six of those reached BEAM emission and the other twelve exposed a subsequent source rejection. The net change from Part860 is 23 more modules reaching emission and zero additional exact matches. There are 710 emission failures with a generic diagnostic and no function location.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Pass the pinned installation and a report destination to the built executable. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`. The four selected cases passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

Rebuild the test oracle in the fixture directory with pinned `erlc.exe +deterministic beam_predefined_probe.erl`.
