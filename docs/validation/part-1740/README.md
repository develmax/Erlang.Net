# Part 1740

Remove a frame-slot clear when an X-to-Y move overwrites the slot before the first frame read, branch or garbage collection point. Keep clears after the last assignment to their slot and retain the order of copies. Continue to coalesce initialization groups in the existing eligible function entry layouts. Pinned OTP beam_ssa_codegen:opt_allocate provides the reference for frame definitions.

All 1,366 Windows OTP 29.1.1 sources were admitted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and passed the OTP loader check. The change affects 778 images and decreases the total absolute Code size gap by 16806 bytes. Normalized instructions gain 0 function matches and lose 0. No status changes.

1680 fewer init_yregs instructions and 11724 fewer slot clears in 778 changed images.

The build and all six targeted tests passed. In the [fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_heap_call_result.erl), the argument copy immediately after allocating the frame initializes Y0, as in OTP. The standalone test compares six results through explicit garbage collection using atomic and heap-allocated values. Other fixture instructions still differ. The complete runtime suite and readiness regeneration were outside this run.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Tests](focused.txt) · [Frame initialization](frame-initialization.txt) · [Probe](frame-init-probe.txt) · [Remaining argument overwrites](remaining-argument-overwrites.txt) · [Remaining frame definitions](remaining-frame-definitions.txt)
