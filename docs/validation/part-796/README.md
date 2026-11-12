# Part796 Observer recursive term expansion

The C# compiler emits the tagged binary reference, guarded raw binary conversion, recursive list and tuple branches in `expand/2`. It checks the resolved guard BIFs, retains the tagged reply failure, and distinguishes the proper-list tail from a non-list result. The pinned OTP 29.1.1 release disassembly guided branch order and frame trimming. No new dependencies were required.

The 916-byte standalone fixture isolates the upstream expansion function and retains its Apache-2.0 notice. It was compiled with `compile:file(Source, [binary,deterministic])` and matches C# output byte for byte. The complete `cdv_term_cb` image remains unsupported at `format_term_fun/4`. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part796-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part796.json`. Across 1,366 files it reported 67 Equal, 195 SourceRejected and 1,104 Unsupported. The tool returned 1 because unmatched cases remain. The `cdv_term_cb` diagnostic advanced from `expand/2` to `format_term_fun/4`. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
