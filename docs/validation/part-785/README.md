# Part785 Observer module details release image

The C# compiler rebuilds the complete 15,436-byte `cdv_mod_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The details function emits the tagged result match and separate `badmatch` and `badrecord` paths. The callback list shares entries for references to the same declared function and emits the tuples and list cells from right to left. The page helpers preserve the parent across lookups and rendering, and the binary formatter supports the direct list conversion branch alongside formatting and flattening.

The pinned OTP `beam_dict.erl` and release disassembly served as references. The compiler reuses the closure table, call resolution and instruction writers. No new dependencies or notices were required.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part785-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part785.json`. Across 1,366 files it reported 62 Equal, 195 SourceRejected and 1,109 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_mod_cb` changed status from Part784. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the permanent release comparison. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
