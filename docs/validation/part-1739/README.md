# Part 1739

Remove repeated line positions across labels in functions without exception handlers. Begin at each function prologue and preserve instrumentation boundaries. Use the within-block rule in functions with try or catch. Pinned OTP beam_z supplies the reference.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 672 images and decreases the total absolute Code size gap by 23399 bytes. Normalized instructions gain 2 function matches and lose 0. No status changes.

7858 line instructions removed in 672 images. All changed counters match their emitted code and position tables retain their entries. 0 Line chunks gain exact equality with OTP and 7 lose it.

The build and all six targeted tests passed. The [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_match_failure_line.erl) now matches the entire OTP Line chunk, with one fewer line instruction. Both function entries retain line 6, and the OTP VM confirms the same normal result, error reason and badmatch at line 4. The full fixture images still differ. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Line tables](line-tables.txt) · [Changed line counts](line-distribution.txt) · [Probe](line-probe.txt) · [Across-block lines](across-block-lines.txt) · [Entry initialization](entry-initialization.txt)
