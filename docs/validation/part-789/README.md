# Part789 Binary formatting closures

The C# compiler emits all functions of `cdv_bin_cb` and reconstructs the pinned Windows OTP 29.1.1 release metadata for this module. The complete file still differs in `Attr`, `Code`, `ExpT`, `FunT`, `LitT` and `LocT`. After normalizing labels and the generated fun unique, the remaining instruction differences are in `get_details/2` and `init_bin_page/2`. [Oracle script](oracle.eval) and [comparison](oracle.txt).

The compiler emits two protected formatting closures with distinct success and catch paths. Unmatched exceptions retain their reason and stacktrace. The third closure formats a recursive digit list and builds its literal prefix as cons cells. The implementation reuses the existing closure table and OTP instruction writers. No new dependencies were required.

The 1,444-byte fixture includes all three closure bodies. It was compiled with `compile:file(Source, [binary,deterministic])` and matches the C# output byte for byte.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part789-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part789.json`. Across 1,366 files it reported 64 Equal, one Different, 195 SourceRejected and 1,106 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_bin_cb` changed status. Its generated image is 6,440 bytes and the release image is 6,120 bytes. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
