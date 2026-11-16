# Part791 Observer internal tables release image

The C# compiler rebuilds the complete 14,356-byte `cdv_int_tab_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The compiler preserves the order of progress and data-loading calls, then builds the list from saved results. Table projection reuses the existing list comprehension helper for direct conversion. The tuple generator skips nonmatching elements and checks the returned list before dropping its first two fields. It retains distinct bad-generator and badmatch paths. The compiler follows the pinned release disassembly and reuses the existing call, list and tuple instruction writers. No new dependencies were required.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part791-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part791.json`. Across 1,366 files it reported 66 Equal, 195 SourceRejected and 1,105 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_int_tab_cb` changed status. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the permanent release comparison. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
