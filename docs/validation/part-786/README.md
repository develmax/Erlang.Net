# Part786 Observer scheduler details release image

The C# compiler rebuilds the complete 16,072-byte `cdv_sched_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The page initializer emits the preliminary property lookup and passes its result to the local field builder. The computed field tuple and list prefix are built at runtime, while the constant suffix stays in the literal table. The queue title uses a constant return for the matching atom and an atom-to-list conversion followed by concatenation on the other branch.

The pinned OTP `beam_ssa_codegen.erl` and release disassembly served as references for heap reservations and operand placement. The compiler reuses existing guard and BIF resolution, literal folding, and instruction writers. No new dependencies were required.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part786-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part786.json`. Across 1,366 files it reported 63 Equal, 195 SourceRejected and 1,108 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_sched_cb` changed status from Part785. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the permanent release comparison. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
