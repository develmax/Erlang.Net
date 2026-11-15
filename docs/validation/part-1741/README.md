# Part 1741

Tuple and list projections can initialize frame slots directly. The allocation prefix tracks reads and writes backwards to identify redundant clears. When an instruction reads and writes the same slot, its source must already be initialized. The existing eligible function entries still coalesce initialization groups.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed. The differing images passed the OTP loader check. The change affects 732 images and decreases the total absolute Code size gap by 16186 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

1694 fewer init_yregs instructions and 10966 fewer slot clears in 732 changed images.

The build and all six targeted tests passed. In the [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_projection_initialization.erl), two tuple projections initialize Y1 and Y2 before garbage collection. The standalone test compares three results with OTP, including heap-allocated field values. OTP and C# still use different frame layouts. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame initialization](frame-initialization.txt) · [Probe](frame-init-probe.txt) · [Remaining frame definitions](remaining-frame-definitions.txt)
