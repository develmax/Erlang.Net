# Part790 Observer binary detail release image

The C# compiler rebuilds the complete 6,120-byte `cdv_bin_cb.beam` image from the Erlang source using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The detail lookup shares the outer tuple test, preserves the key across the lookup, and retains distinct `badmatch` locations. The page initializer keeps the closure results in frame slots and builds the nested tuples and list after the last call. The compiler follows the heap reservations and operand placement from pinned OTP code generation and release disassembly, reusing the existing instruction writers. No new dependencies were required.

The pinned source and release image are covered by the permanent release comparison.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part790-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part790.json`. Across 1,366 files it reported 65 Equal, 195 SourceRejected and 1,106 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_bin_cb` changed status. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
