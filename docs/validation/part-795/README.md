# Part795 Observer memory release image

The C# compiler rebuilds the complete 15,324-byte `cdv_mem_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Allocator normalization dispatches on tuple arity, preserves the recursive retry paths and generates the tuple-filtered comprehension. The compiler resolves the length BIF before emitting its typed instruction form and reuses the mapping scheduler and list failure handling. The pinned release disassembly guided register placement and helper ordering. No new dependencies were required.

The pinned source and release image are covered by the permanent release comparison.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part795-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part795.json`. Across 1,366 files it reported 67 Equal, 195 SourceRejected and 1,104 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_mem_cb` changed status. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
