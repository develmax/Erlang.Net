# Part892 Header type documentation

The Docs writer preserves the OTP annotation for a type declared in an included file. The entry annotation is a list containing the header basename and the header-relative source location. The `source_anno` metadata contains the location tuple. Existing ordinary type annotations keep their previous encoding.

The deterministic fixture places the header in an include subdirectory and refers to its type from an exported function spec. The complete image matched OTP byte for byte, including compressed Docs. Rebuild with `erlc +deterministic beam_header_docs_probe.erl` from the fixture directory. [OTP documentation term](otp-docs.txt).

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused checks passed. The full runtime suite was outside this run and the readiness CLI rejected the partial report. [Focused report](focused.json).

The full 1,366-file corpus reported 100 Equal, 732 Different, 164 SourceRejected and 370 Unsupported. Fifty-nine modules previously stopped at the header type Docs check and now emit differing images. [Corpus report](otp-corpus.json).
