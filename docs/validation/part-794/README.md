# Part794 Observer allocator columns and area projection

The C# compiler emits every function in `cdv_mem_cb` using the pinned Windows OTP 29.1.1 release profile. The generated file differs in `AtU8`, `Attr`, `Code`, `ExpT`, `ImpT`, `Line`, `LocT` and `Type`. After normalizing labels, the instruction differences are in `fix_alloc/1` and its comprehension helper. [Oracle script](oracle.eval) and [comparison](oracle.txt).

The compiler emits the prepended constant column and a recursive helper that turns each column into a three-element tuple. It also emits area projection before loading the column definitions and checks BIF resolution for `tuple_to_list/1`. Both paths reuse the existing mapping and failure handling. The pinned OTP release disassembly guided register and helper order. No new dependencies were required.

The independent 1,076-byte fixture was compiled with `compile:file(Source, [binary,deterministic])` and matches the C# output byte for byte.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part794-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part794.json`. Across 1,366 files it reported 66 Equal, one Different, 195 SourceRejected and 1,104 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_mem_cb` changed status. Its generated file is 15,536 bytes and the release file is 15,324 bytes. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
