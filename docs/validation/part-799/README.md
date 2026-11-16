# Part799 Observer dynamic detail replies

The C# compiler emits the dynamic module callback, dispatch by reply shape, dialog selection and zero-argument closure invocation. It preserves the separate case failures and keeps the callback arguments alive across progress updates. It also emits the conditional progress pulse and its concatenated message. The pinned OTP 29.1.1 disassembly guided register scheduling and frame trims. No new dependencies were required.

The 1,412-byte standalone fixture isolates the upstream reply dispatch and progress pulse, keeps the Apache-2.0 notice and uses forwarding stubs for other callbacks. It was compiled with `compile:file(Source, [binary,deterministic])` and matches C# output byte for byte. The complete `cdv_detail_wx` image remains unsupported at `init/6`. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part799-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part799.json`. Across 1,366 files it reported 68 Equal, 195 SourceRejected and 1,103 Unsupported. The tool returned 1 because unmatched cases remain. The `cdv_detail_wx` diagnostic advanced from `init/1` to `init/6`. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
