# Part 1630 — Emit receive markers after a checked setup call

A terminal receive matching the reference returned by a fresh process monitor now uses the OTP receive marker instructions. The setup result is compared in X0 and only the marker and reference require frame slots. The compiler retains the match location for badmatch, including when the setup call starts on a different source line. The pinned beam_ssa_recv pass and OTP instruction definitions provide the reference. The existing runtime supports these instructions. No dependency was added.

The permanent fixture covers auto-imported and explicitly qualified monitor calls. Its full BEAM matches the OTP compiler output. The OTP VM verified an immediate DOWN for a missing name, a delayed DOWN for a registered process, preservation of a stale DOWN for another reference, and the badmatch source line. Three focused checks passed. [Hashes](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

The Windows corpus accepted and generated all 1,366 sources. The final counts were 393 Equal and 973 Different. ELDAPv3 remained Equal. Observer was the only changed release image, with an 11-byte reduction in the Code size gap. Its start_and_wait/0 now matches after label normalization. Its guarded start_and_wait/1 and mixed atom/list start/1 still differ. The OTP loader accepted all 973 differing images. [Corpus](otp-corpus.json) · [Observer instructions](observer-disassembly.txt) · [Loader and size](loader-and-code.txt).

The baseline chunk inventory found differences in Code in all 973 remaining modules. Both builds used SDK 10.0.404 through MSBuild. The full test suite and readiness regeneration were outside this run. [Chunk inventory](part1630-chunk-ranking.txt).
