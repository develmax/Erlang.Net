# Part792 Observer captured table comprehension

The C# compiler emits the interleaved calls, delayed extraction of the rows and warning, and a two-argument comprehension helper. It verifies that the local loader returns a two-element tuple and that its row producer returns a proper list of three-element tuples before omitting shape tests. The compiler retains the loader's tagged reply check and badmatch location. The pinned OTP 29.1.1 disassembly guided frame trimming and helper argument order. No new dependencies were required.

The independent 1,228-byte fixture was compiled with `compile:file(Source, [binary,deterministic])` and matches C# output byte for byte. The complete `cdv_mem_cb` release image remains unsupported at `upper/1`. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part792-tool`. The final run used the compiler assembly from the focused test build and wrote `artifacts/beam-backend/otp-corpus-part792-final.json`. Across 1,366 files it reported 66 Equal, 195 SourceRejected and 1,105 Unsupported. The tool returned 1 because unmatched cases remain. The `cdv_mem_cb` diagnostic advanced from `get_info/0` to `upper/1`. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
