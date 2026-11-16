# Part793 Observer memory field formatting

The C# compiler emits the split, map and join sequence in `upper/1` using the existing list mapping helper. It also emits the tagged memory reply, the recursive traversal of memory keys and the nested field tuples. The pinned OTP 29.1.1 disassembly guided the register layout and call order. No new dependencies were required.

The independent 1,100-byte fixture was compiled with `compile:file(Source, [binary,deterministic])` and matches C# output byte for byte. The complete `cdv_mem_cb` image remains unsupported at `alloc_columns/1`. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part793-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part793.json`. Across 1,366 files it reported 66 Equal, 195 SourceRejected and 1,105 Unsupported. The tool returned 1 because unmatched cases remain. The `cdv_mem_cb` diagnostic advanced from `upper/1` to `alloc_columns/1`. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
