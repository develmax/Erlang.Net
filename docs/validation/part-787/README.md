# Part787 Observer process details release image

The C# compiler rebuilds the complete 19,124-byte `cdv_proc_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The tagged reply path preserves the original reply for errors and for the warning field, allocates the expansion table, and prepends it to the record properties. The success path trims its frame before formatting the title. The page helpers preserve parent and table state through nested rendering and forward indexed results to the remote tail call.

The pinned OTP `beam_ssa_pre_codegen.erl` and release disassembly served as references. The implementation reuses the tagged reply matcher, closure table and instruction writers. The independent 1,192-byte fixture was compiled with `compile:file(Source, [binary,deterministic])`. No new dependencies were required.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part787-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part787.json`. Across 1,366 files it reported 64 Equal, 195 SourceRejected and 1,107 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_proc_cb` changed status from Part786. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the standalone fixture and permanent release comparison. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
