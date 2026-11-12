# Part784 Observer ETS details release image

The C# compiler rebuilds the complete 15,000-byte `cdv_ets_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. The installed file is the byte comparison oracle. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The details function returns the constant missing reply before allocating a frame. The success path reuses a single frame slot for the identifier and converted properties. The page initializer keeps the original list through the map test and clears its frame slot on the map path before appending the converted entries. Both paths share the tuple construction and remote tail call.

The pinned OTP `beam_ssa_pre_codegen.erl` and module disassembly served as references. The implementation reuses the compiler's instruction writers, Erlang constant evaluator and guard binding resolution. No new dependencies or notices were required.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part784-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part784.json`. Across 1,366 files it reported 61 Equal, 195 SourceRejected and 1,110 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_ets_cb` changed status from Part783. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the permanent release comparison. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
