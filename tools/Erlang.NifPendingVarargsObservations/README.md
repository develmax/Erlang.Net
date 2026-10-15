# Original pending-exception and bounded native SDK observations

The manual `nif-pending-native.yml` workflow builds the exact OTP-29.1.1 source pin, compiles against its real configured headers, then records two separate results: the bounded trusted SDK profile and thirteen fresh original VM observations. Original collection and independent typed review continue after an SDK failure; an original-only success never establishes SDK compatibility.

Files in `original/` are frozen observation inputs. Their source plan and expected policy must retain exact bytes. `verify-original13.py` reuses the existing hash-checked typed ETF decoder and preserves full raw stacks, taints and process records. Its metadata schema is the actual observer tag plus seven pairs.

`seal-source-plan.py` runs only after reviewing the final source tree. The reviewed plan declares exact binary versus LF text hash modes. `run-reviewed.py` rejects changed sources and any missing or additional input path before building or executing native code. The workflow pins the plan hash. Reseal and review before running on a changed source tree.

C fixtures, ABI exports and varargs traversal are the documented test-infrastructure/platform interoperability exception under work.md section 2.1. C# owns all Runtime terms and algorithms. No upstream headers, native binaries or new license files are distributed here. Full NIF ABI, public load_nif, native resources/threads, automatic owner lifetime and complete Erlang compatibility remain unfinished.
