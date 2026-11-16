# Part782 Observer local function references and dispatch

The C# compiler rebuilds the complete 15,156-byte `cdv_dist_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. The release file serves as the expected output in the regression. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Local function references inside returned tuples and lists use `make_fun3` with an empty environment and a `FunT` entry for the declared function. The compiler folds constant list tails and writes local references into `Dbgi`. The tagged formatting flow reuses the existing list comprehension helper and checks the resolution of the conversion BIF. The atom alias dispatch separates the atom test from the integer `select_val`. The pinned `beam_asm.erl`, `v3_core.erl` and module disassembly served as references. The change adds no dependency.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part782-tool`. It ran with the pinned installation and `artifacts/beam-backend/otp-corpus-part782.json`. Across 1,366 release images, it reported 59 Equal, 195 SourceRejected and 1,112 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_dist_cb` changed status since Part781. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused checks passed, including the permanent release-image regression. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
