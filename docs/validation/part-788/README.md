# Part788 Recursive binary digit formatting

The compiler emits two unsigned four-bit reads from a single binary match context. It distinguishes the empty input, final byte, line-break path and decrementing path. Both recursive paths pass the match context directly and rebuild the list after the recursive call. The invalid-input path restores the original bit position before reporting a function clause failure. Integer annotations require validated local call sites with an integer seed and the matched decrement.

The pinned OTP release disassembly guided register placement and the use of match commands. The compiler reuses binary matching, call resolution and the existing BEAM operand writers. No new dependencies were required.

The 940-byte standalone fixture was compiled using `compile:file(Source, [binary,deterministic])` and matches the C# image byte for byte. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part788-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part788.json`. Across 1,366 files it reported 64 Equal, 195 SourceRejected and 1,107 Unsupported. The tool returned 1 because unmatched cases remain. Module status counts are unchanged from Part787. The first unsupported function in `cdv_bin_cb` moved from `format_hex/2` to `format_bin_fun/3`. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the standalone fixture and permanent release comparisons. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
