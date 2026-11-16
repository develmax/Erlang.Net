# Part797 Observer protected term formatting

The C# compiler emits every function in `cdv_term_cb` using the pinned Windows OTP 29.1.1 release profile. The generated file differs in `Attr`, `Code`, `ExpT`, `FunT`, `Line`, `LitT` and `LocT`. After normalizing labels and the fun unique, the remaining instruction differences are in `get_details/2` and `init_term_page/2`. [Oracle script](oracle.eval) and [comparison](oracle.txt).

The term formatting closure captures four arguments and emits two exception scopes. It preserves the success or handled result across cleanup and runs cleanup before re-raising an unhandled exception. Release debug forms retain the after body. The compiler reuses its closure table and exception instruction writers. No new dependencies were required.

The 1,208-byte fixture isolates the upstream closure and retains its Apache-2.0 notice. It was compiled with `compile:file(Source, [binary,deterministic])` and matches C# output byte for byte.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part797-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part797.json`. Across 1,366 files it reported 67 Equal, one Different, 195 SourceRejected and 1,103 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_term_cb` changed status. Its generated image is 5,800 bytes and the release image is 5,564 bytes. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
