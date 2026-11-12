# Part798 Observer term detail release image

The C# compiler rebuilds the complete 5,564-byte `cdv_term_cb.beam` image from the Erlang source using the pinned Windows OTP 29.1.1 release profile. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The detail lookup preserves the table, key and type through the remote call and builds the tagged result. The page initializer preserves the order of the progress calls and the two expansions, schedules the six formatter calls and constructs the nested callback list. The pinned release disassembly guided the stack slots and frame trim. The compiler reuses its existing instruction writers. No new dependencies were required.

The pinned source and release image are covered by the permanent release comparison.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part798-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part798.json`. Across 1,366 files it reported 68 Equal, 195 SourceRejected and 1,103 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_term_cb` changed status. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
